using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Server;

public class Server
{
    private readonly object _lock = new();
    private readonly Dictionary<int, TcpClient> _userClients = new();
    

    public static async Task Main(string[] args)
    {
        var server = new Server();
        await server.StartServer();
    }

    private async Task StartServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 7891);
        listener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        int userId = -1;

        try
        {
            var stream = client.GetStream();
            
            while (client.Connected)
            {
                string? messageJson = await TcpMessageHelper.ReceiveMessageAsync(stream);
                if (messageJson == null) break;

                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(messageJson);

                if (data == null || !data.ContainsKey("Type"))
                {
                    Console.WriteLine("Received invalid or untyped message.");
                    continue;
                }

                string type = data["Type"];

                if (type == "identity" && data.ContainsKey("UserId"))
                {
                    userId = int.Parse(data["UserId"]);
                    lock (_lock)
                    {
                        if (_userClients.ContainsKey(userId))
                        {
                            _userClients[userId].Close();
                            _userClients.Remove(userId);
                            Console.WriteLine($"Old connection for User {userId} closed.");
                        }
                        _userClients.Add(userId, client);
                    }
                    Console.WriteLine($"User {userId} identified and added to active clients.");
                }
                else if (type == "message")
                {
                    if (data.ContainsKey("SenderId") && data.ContainsKey("ReceiverId") && data.ContainsKey("MessageText"))
                    {
                        int senderId = int.Parse(data["SenderId"]);
                        int receiverId = int.Parse(data["ReceiverId"]);
                        string messageText = data["MessageText"];

                        Console.WriteLine($"Server received message from {senderId} to {receiverId}: {messageText}");

                        var messageForRecipient = new
                        {
                            Type = "message",
                            SenderId = senderId.ToString(),
                            MessageText = messageText,
                        };

                        string jsonForRecipient = JsonSerializer.Serialize(messageForRecipient);

                        await RelayMessageToClient(receiverId, jsonForRecipient);
                    }
                    else
                    {
                        Console.WriteLine("Received incomplete message data.");
                    }
                }
            }
        }
        catch (IOException)
        {
            Console.WriteLine($"Client {userId} connection lost.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client User {userId}: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                if (userId != -1 && _userClients.ContainsKey(userId) && _userClients[userId] == client)
                {
                    _userClients.Remove(userId);
                    Console.WriteLine($"User {userId} removed from active clients.");
                }
            }
            client.Close();
            Console.WriteLine($"Client connection closed for User {userId}.");
        }
    }

    private async Task RelayMessageToClient(int targetUserId, string messageJson)
    {
        NetworkStream? targetStream = null;
        TcpClient? targetClient = null; 
        bool clientRemoved = false;

        lock (_lock)
        {
            if (_userClients.TryGetValue(targetUserId, out targetClient))
            {
               
                targetStream = targetClient.GetStream();
            }
            else
            {
                Console.WriteLine($"Target client {targetUserId} not found in active connections.");
                return; 
            }
        }

        if (targetClient.Connected && targetStream != null) 
        {
            try
            {
                await TcpMessageHelper.SendMessageAsync(targetStream, messageJson);
                Console.WriteLine($"Relayed message to {targetUserId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error relaying message to {targetUserId}: {ex.Message}");
                lock (_lock) 
                {
                    if (_userClients.ContainsKey(targetUserId) && _userClients[targetUserId] == targetClient)
                    {
                        _userClients.Remove(targetUserId);
                        clientRemoved = true;
                        Console.WriteLine($"Client {targetUserId} removed due to send error.");
                    }
                }
            }
        }
        else 
        {
            Console.WriteLine($"Target client {targetUserId} found but not connected. Removing.");
            lock (_lock) 
            {
                if (_userClients.ContainsKey(targetUserId) && _userClients[targetUserId] == targetClient)
                {
                    _userClients.Remove(targetUserId);
                    clientRemoved = true;
                }
            }
        }

        if (clientRemoved)
        {
            targetClient?.Close();
        }
    }
}