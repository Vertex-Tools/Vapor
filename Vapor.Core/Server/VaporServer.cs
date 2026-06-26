// ====================================
// <copyright file="VaporServer.cs" company="Spicco D'Aura">
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

namespace Vapor.Core.Server;

public class VaporServer : IDisposable
{
    private readonly string _baseChannelName;
    private readonly int _bufferSize;
    
    private MemoryMappedFile? _rxMmf;
    private MemoryMappedViewAccessor? _rxAccessor;
    private EventWaitHandle? _rxSyncEvent;
    
    private MemoryMappedFile? _txMmf;
    private MemoryMappedViewAccessor? _txAccessor;
    private EventWaitHandle? _txSyncEvent;
    private Mutex? _txMutex;

    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public event Action<byte[]>? OnMessageReceived;

    public VaporServer(string baseChannelName, int bufferSize = VaporConstants.DefaultBufferSize)
    {
        _baseChannelName = baseChannelName;
        _bufferSize = bufferSize;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        
        _rxMmf = MemoryMappedFile.CreateOrOpen($"{_baseChannelName}_In", _bufferSize + VaporConstants.HeaderSize);
        _rxAccessor = _rxMmf.CreateViewAccessor();
        MemoryHeader.Reset(_rxAccessor);
        _rxSyncEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{_baseChannelName}_In_Sync");
        
        _txMmf = MemoryMappedFile.CreateOrOpen($"{_baseChannelName}_Out", _bufferSize + VaporConstants.HeaderSize);
        _txAccessor = _txMmf.CreateViewAccessor();
        MemoryHeader.Reset(_txAccessor);
        _txSyncEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"{_baseChannelName}_Out_Sync");
        _txMutex = new Mutex(false, $"{_baseChannelName}_Out_Mutex");

        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(ListenLoop, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void ListenLoop()
    {
        if (_rxAccessor == null || _rxSyncEvent == null) return;

        while (_cts is { Token.IsCancellationRequested: false })
        {
            _rxSyncEvent.WaitOne();

            while (true)
            {
                byte[]? message = RingBuffer.ReadNextMessage(_rxAccessor, _bufferSize);
                if (message == null) break;

                OnMessageReceived?.Invoke(message);
            }
        }
    }

    public bool Send(ReadOnlySpan<byte> message)
    {
        if (!_isRunning || _txAccessor == null || _txSyncEvent == null || _txMutex == null)
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
        finally
        {
            _txMutex.ReleaseMutex();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _rxAccessor?.Dispose();
        _rxMmf?.Dispose();
        _rxSyncEvent?.Dispose();
        
        _txAccessor?.Dispose();
        _txMmf?.Dispose();
        _txSyncEvent?.Dispose();
        _txMutex?.Dispose();
    }
}