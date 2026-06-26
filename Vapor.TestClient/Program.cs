// ====================================
// <copyright file="Program.cs" company="Spicco D'Aura">
// Copyright (c) Spicco D'Aura. All rights reserved.
// Licensed under the CC BY-SA 1.0 License.
// </copyright>
// ====================================

using System;
using System.Text;
using Vapor.Core.Client;

namespace Vapor.TestClient;

internal static class Program
{
    private static VaporClient? _client;

    public static void Main(string[] args)
    {
        Console.Title = "Vapor IPC - LocalAdmin Console";
        
        PrintHeader();

        // Initialize the client bound to the server's base communication channel
        _client = new VaporClient("Vapor_SL_Channel");

        // Bind cross-process channel state and data event listeners
        _client.OnConnected += () => LogSystem("Channel Status: CONNECTED to shared memory pipeline.", ConsoleColor.Green);
        _client.OnDisconnected += () => LogSystem("Channel Status: DISCONNECTED. Polling for server handle active...", ConsoleColor.Yellow);
        _client.OnMessageReceived += OnLogReceived;

        try
        {
            LogSystem("Spawning background asynchronous connection monitor thread...", ConsoleColor.Gray);
            _client.Start();

            Console.WriteLine("Type a command and press ENTER ('exit' to close the console):\n");

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                if (!_client.IsConnected)
                {
                    LogSystem("Transmission blocked: Memory pipeline connection not established.", ConsoleColor.Red);
                    continue;
                }

                // Convert user payload string to byte array and push via atomic Mutex write
                byte[] commandBytes = Encoding.UTF8.GetBytes(input);
                bool success = _client.Send(commandBytes);

                if (!success)
                {
                    LogSystem("Transmission failure: Shared RingBuffer might be saturated.", ConsoleColor.Red);
                }
            }
        }
        catch (Exception ex)
        {
            LogSystem($"Exception caught in main input thread: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            LogSystem("Releasing global system Mutex hooks and unmapping MMF regions...", ConsoleColor.Gray);
            _client.Dispose();
        }
    }

    private static void OnLogReceived(byte[] data)
    {
        // Clears the current input prompt '> ' before writing the asynchronous incoming packet
        int currentLineCursor = Console.CursorLeft;
        Console.Write(new string('\b', currentLineCursor));

        string logMessage = Encoding.UTF8.GetString(data);
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[SERVER-OUT] ");
        Console.ResetColor();
        Console.WriteLine(logMessage);

        // Re-render the user input prompt on the clean line
        Console.Write("> ");
    }

    private static void LogSystem(string message, ConsoleColor color)
    {
        // Temporarily clear the cursor to output a clean system notice
        int currentLineCursor = Console.CursorLeft;
        Console.Write(new string('\b', currentLineCursor));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ForegroundColor = color;
        Console.Write("[SYSTEM] ");
        Console.ResetColor();
        Console.WriteLine(message);

        Console.Write("> ");
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=======================================================");
        Console.WriteLine("               VAPOR IPC CUSTOM CONSOLE                ");
        Console.WriteLine("=======================================================");
        Console.ResetColor();
    }
}