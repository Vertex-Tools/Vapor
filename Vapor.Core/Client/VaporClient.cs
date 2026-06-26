// ====================================
// <copyright file="VaporClient.cs" company="Spicco D'Aura">
// Copyright (c) Spicco D'Aura. All rights reserved.
// Licensed under the CC BY-SA 1.0 License.
// </copyright>
// ====================================

using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Vapor.Core.Common;
using Vapor.Core.Memory;

namespace Vapor.Core.Client;

public class VaporClient : IDisposable
{
    private readonly string _baseChannelName;
    private readonly int _bufferSize;
    
    private MemoryMappedFile? _txMmf;
    private MemoryMappedViewAccessor? _txAccessor;
    private EventWaitHandle? _txSyncEvent;
    private Mutex? _txMutex;

    private MemoryMappedFile? _rxMmf;
    private MemoryMappedViewAccessor? _rxAccessor;
    private EventWaitHandle? _rxSyncEvent;

    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private bool _isDisposed;

    public event Action<byte[]>? OnMessageReceived;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _isConnected;

    public VaporClient(string baseChannelName, int bufferSize = VaporConstants.DefaultBufferSize)
    {
        _baseChannelName = baseChannelName;
        _bufferSize = bufferSize;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(ConnectionMonitorLoop, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void ConnectionMonitorLoop()
    {
        while (_cts is { Token.IsCancellationRequested: false })
        {
            if (!_isConnected)
            {
                if (TryConnect())
                {
                    _isConnected = true;
                    OnConnected?.Invoke();
                    Task.Factory.StartNew(ListenLoop, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
            }
            else
            {
                try
                {
                    using var test = MemoryMappedFile.OpenExisting($"{_baseChannelName}_In");
                }
                catch
                {
                    HandleDisconnect();
                }
            }

            Thread.Sleep(1000); 
        }
    }

    private bool TryConnect()
    {
        try
        {
            _txMmf = MemoryMappedFile.OpenExisting($"{_baseChannelName}_In");
            _txAccessor = _txMmf.CreateViewAccessor();
            _txSyncEvent = EventWaitHandle.OpenExisting($"{_baseChannelName}_In_Sync");
            _txMutex = new Mutex(false, $"{_baseChannelName}_In_Mutex");

            _rxMmf = MemoryMappedFile.OpenExisting($"{_baseChannelName}_Out");
            _rxAccessor = _rxMmf.CreateViewAccessor();
            _rxSyncEvent = EventWaitHandle.OpenExisting($"{_baseChannelName}_Out_Sync");

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ListenLoop()
    {
        while (_isConnected && _cts != null && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                _rxSyncEvent?.WaitOne(1000); 

                if (!_isConnected || _rxAccessor == null) break;

                while (true)
                {
                    byte[]? message = RingBuffer.ReadNextMessage(_rxAccessor, _bufferSize);
                    if (message == null) break;

                    OnMessageReceived?.Invoke(message);
                }
            }
            catch
            {
                HandleDisconnect();
                break;
            }
        }
    }

    public bool Send(ReadOnlySpan<byte> message)
    {
        if (!_isConnected || _txAccessor == null || _txSyncEvent == null || _txMutex == null)
            return false;

        _txMutex.WaitOne();
        try
        {
            bool success = RingBuffer.WriteMessage(_txAccessor, _bufferSize, message);
            if (success)
            {
                _txSyncEvent.Set();
                return true;
            }
            return false;
        }
        catch
        {
            HandleDisconnect();
            return false;
        }
        finally
        {
            _txMutex.ReleaseMutex();
        }
    }

    private void HandleDisconnect()
    {
        if (!_isConnected) return;
        _isConnected = false;
        
        CleanupCurrentConnection();
        OnDisconnected?.Invoke();
    }

    private void CleanupCurrentConnection()
    {
        _txAccessor?.Dispose();
        _txMmf?.Dispose();
        _txSyncEvent?.Dispose();
        _txMutex?.Dispose();
        _rxAccessor?.Dispose();
        _rxMmf?.Dispose();
        _rxSyncEvent?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Cancel();
        CleanupCurrentConnection();
    }
}