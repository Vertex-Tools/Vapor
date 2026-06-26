// ====================================
// <copyright file="Program.cs" company="Spicco D'Aura">
// Copyright (c) Spicco D'Aura. All rights reserved.
// Licensed under the CC BY-SA 1.0 License.
// </copyright>
// ====================================

using System;
using System.Text;
using Vapor.Core.Server;

namespace Vapor.TestServer;

internal static class Program
{
    private static VaporServer? _server;
    private static bool _keepRunning = true;

    public static void Main(string[] args)
    {
        Console.Title = "Vapor IPC - Dedicated Server Emulator";
        
        PrintHeader();

        // Initialize the Server on the designated shared memory channel
        _server = new VaporServer("Vapor_SL_Channel");

        // Subscribe to the incoming payload event
        _server.OnMessageReceived += OnCommandReceived;

        // Formal interception of termination signals (Ctrl+C / Window Close)
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination to allow graceful cleanup
            LogInfo("Termination request captured. Cleaning up shared memory allocation...");
            _keepRunning = false;
        };

        try
        {
            _server.Start();
            LogSuccess("IPC Infrastructure successfully allocated in system RAM.");
            LogInfo("Listening on channel 'Vapor_SL_Channel'. Press Ctrl+C to exit.\n");

            // Main execution thread sleep loop to keep the process alive
            while (_keepRunning)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        catch (Exception ex)
        {
            LogError($"Critical error during Server initialization: {ex.Message}");
        }
        finally
        {
            LogInfo("Releasing Shared Memory MMF mappings and system hooks...");
            _server.Dispose();
            LogInfo("Server terminated gracefully.");
        }
    }

    private static void OnCommandReceived(byte[] data)
    {
        try
        {
            string command = Encoding.UTF8.GetString(data).Trim();
            LogIncoming($"Command received: \"{command}\"");

            // Command processing simulation (Simulating game engine logic / LocalAdmin hooks)
            string responseMessage = command.ToLower() switch
            {
                "status"  => "[SERVER-INFO] Status: Running | Players: 18/40 | TPS: 59.9",
                "restart" => "[SERVER-WARN] Initiating controlled server hot-reload sequence...",
                "stop"    => "[SERVER-CRIT] Shutting down instance execution loop...",
                _         => $"[SERVER-LOG] Command '{command}' successfully processed on the main thread."
            };

            // Transmit the binarized response back to the client via the Output RingBuffer
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
            _server?.Send(responseBytes);
        }
        catch (Exception ex)
        {
            LogError($"Error processing incoming memory packet: {ex.Message}");
        }
    }

    #region Logging Helpers
    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=======================================================");
        Console.WriteLine("                VAPOR IPC TEST SERVER                  ");
        Console.WriteLine("=======================================================");
        Console.ResetColor();
    }

    private static void LogInfo(string message) => WriteLog("INFO", ConsoleColor.Gray, message);
    private static void LogSuccess(string message) => WriteLog(" OK ", ConsoleColor.Green, message);
    private static void LogIncoming(string message) => WriteLog(" IN ", ConsoleColor.Magenta, message);
    private static void LogError(string message) => WriteLog("ERR ", ConsoleColor.Red, message);

    private static void WriteLog(string prefix, ConsoleColor color, string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] ");
        Console.ForegroundColor = color;
        Console.Write($"[{prefix}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }
    #endregion
}