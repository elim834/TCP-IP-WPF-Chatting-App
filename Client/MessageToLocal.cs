using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace LastMessenger;

public class MessageToLocal
{
    public static string GetChatMessagePath(int userId, string ChatKey)
    {
        
        string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LastMessenger", $"user_{userId}");
        Directory.CreateDirectory(baseFolder);
        return Path.Combine(baseFolder, $"chat_{ChatKey}.json");
    }

    public static void SaveMessagesToLocal(List<MessageModel> messages, int userId, string ChatKey)
    {
        string path = GetChatMessagePath(userId, ChatKey);
        string json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static List<MessageModel> LoadMessagesFromLocal(int userId, string ChatKey)
    {
        
        string path = GetChatMessagePath(userId, ChatKey);
        return File.Exists(path) ? JsonSerializer.Deserialize<List<MessageModel>>(File.ReadAllText(path)) ?? new() : new();

    }
    
}