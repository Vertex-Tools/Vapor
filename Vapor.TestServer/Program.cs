using System.Text;
using Vapor.Core.Server;

Console.Title = "VAPOR SERVER (Gioco Dedicated Server)";
Console.WriteLine("=== SCP:SL Server Emulator ===");

using var server = new VaporServer("Vapor_SL_Channel");

server.OnMessageReceived += (byte[] data) =>
{
    string command = Encoding.UTF8.GetString(data);
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📥 Comando ricevuto da LocalAdmin: '{command}'");

    // Rispondi al client per simulare l'output/log del comando eseguito
    string responseLog = $"[SERVER-LOG] Comando '{command}' eseguito con successo.";
    server.Send(Encoding.UTF8.GetBytes(responseLog));
};

server.Start();
Console.WriteLine("📡 Server online. In attesa di comandi da LocalAdmin... Premi INVIO per spegnere.");
Console.ReadLine();