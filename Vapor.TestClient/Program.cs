using System.Text;
using Vapor.Core.Client;

Console.Title = "VAPOR CLIENT (LocalAdmin Console)";
Console.WriteLine("=== LocalAdmin Console Custom ===");

using var client = new VaporClient("Vapor_SL_Channel");

client.OnConnected += () => Console.WriteLine("\n✅ [MONITOR] Connesso al server di gioco!");
client.OnDisconnected += () => Console.WriteLine("\n⚠️ [MONITOR] Connessione perduta! Tentativo di riconnessione in corso...");

client.OnMessageReceived += (byte[] data) =>
{
    string log = Encoding.UTF8.GetString(data);
    Console.WriteLine($"\n{log}");
    Console.Write("> ");
};

client.Start(); 

Console.WriteLine("Scrivi un comando (es. 'stop', 'ban') e premi INVIO:\n");

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
        break;

    if (!client.IsConnected)
    {
        Console.WriteLine("❌ Impossibile inviare: Non connesso al server!");
        continue;
    }

    client.Send(Encoding.UTF8.GetBytes(input));
}