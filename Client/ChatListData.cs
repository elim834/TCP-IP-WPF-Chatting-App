using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using MySql.Data.MySqlClient;
using LastMessenger;

namespace LastMessenger;

public class ChatListData : INotifyPropertyChanged
{
    public ObservableCollection<ChatModel> Chats { get; set; } = new();
    public  ChatModel SelectedChat { get; set; }
    private static readonly string ConnStr = "server=localhost;user=root;password=AtesBebis_20;database=rider_dbb;";
    private readonly int _currentUserId;

    public class ChatModel
    {
        public int ChatId { get; set; }
        public int ReceiverId { get; set; }
        public string DisplayUsername { get; set; } = "";
        public ObservableCollection<MessageModel> Message { get; set; } = new();
        public bool IsGroup { get; set; } = false;
        public string GroupKey { get; set; } = "";

        public string LastMessageText => Message.LastOrDefault()?.MessageText ?? "";
        
        public ChatModel()
        {
            Message = new ObservableCollection<MessageModel>();
        }
    }

    
    public ChatListData(int userId)
    {
        _currentUserId = userId; 
        LoadChatsFromDatabase();
    }

    public static int GetOrCreateChatId(int senderId, int receiverId)
    {
        int minId = Math.Min(senderId, receiverId);
        int maxId = Math.Max(senderId, receiverId);
        
        using var conn = new MySqlConnection(ConnStr);
        conn.Open();
        
        string checkQuery = "SELECT ChatId FROM Chats WHERE SenderId = @min AND ReceiverId = @max AND IsGroup = 0";
        using var checkCmd = new MySqlCommand(checkQuery, conn);
        checkCmd.Parameters.AddWithValue("@min", minId);
        checkCmd.Parameters.AddWithValue("@max", maxId);

        using var reader = checkCmd.ExecuteReader();
        if (reader.Read())
        {
            int existingChatId = reader.GetInt32("ChatId");
            reader.Close();
            return existingChatId;
        }
        reader.Close();

        
        
        string insertQuery = "INSERT INTO Chats (SenderId, ReceiverId, IsGroup) VALUES (@min, @max, 0)";
        using var insertCmd = new MySqlCommand(insertQuery, conn);
        insertCmd.Parameters.AddWithValue("@min", minId);
        insertCmd.Parameters.AddWithValue("@max", maxId);
        insertCmd.ExecuteNonQuery();
        
        return (int)insertCmd.LastInsertedId;
    }
    
    private void LoadChatsFromDatabase()
    {
        Chats.Clear();

        try
        {
            using var conn = new MySqlConnection(ConnStr);
            conn.Open();

            string query = @"
                SELECT 
                    c.ChatId,
                    CASE 
                        WHEN c.SenderId = @userId THEN c.ReceiverId
                        ELSE c.SenderId
                    END AS OtherUserId,
                    u.Username
                FROM Chats c
                JOIN Users u ON u.Id = 
                    CASE 
                        WHEN c.SenderId = @userId THEN c.ReceiverId
                        ELSE c.SenderId
                    END
                WHERE c.IsGroup = 0 AND (c.SenderId = @userId OR c.ReceiverId = @userId)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", MainWindow.LoggedInUserId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var chatId = reader.GetInt32("ChatId");
                var otherUserId = reader.GetInt32("OtherUserId");
                var username = reader.GetString("Username");

                Chats.Add(new ChatModel
                {
                    ChatId = chatId,
                    ReceiverId = otherUserId,
                    DisplayUsername = username,
                    IsGroup = false
                });
            }

            reader.Close();

            string groupQuery = "SELECT ChatId, GroupKey FROM Chats WHERE IsGroup = 1";
            using var groupCmd = new MySqlCommand(groupQuery, conn);
            using var groupReader = groupCmd.ExecuteReader();
            
            while (groupReader.Read())
            {
                Chats.Add(new ChatModel
                {
                    ChatId = groupReader.GetInt32("ChatId"),
                    IsGroup = true,
                    GroupKey = groupReader.IsDBNull(groupReader.GetOrdinal("GroupKey"))? 
                        "": groupReader.GetString("GroupKey")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadChatsFromDatabase error: " + ex.Message);
        }
    }

    public static int GroupOrNot(int chatId)
    {
        using var conn = new MySqlConnection(ConnStr);
        conn.Open();
        
        const string query = "SELECT IsGroup FROM Chats  WHERE chatId = @chatId";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@chatId", chatId);
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetInt32("IsGroup");
        }

        return -1;
    }
    
    public static int GetGroupChatIdByKey(string groupKey)
    {
        using var conn = new MySqlConnection(ConnStr);
        conn.Open();

        string query = "SELECT ChatId FROM Chats WHERE GroupKey = @key AND IsGroup = 1";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@key", groupKey);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetInt32("ChatId");
        }

        return -1;
    }


    public static string GenerateUnique8DigitId(MySqlConnection conn)
    {
        string Generate()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Create().GetBytes(bytes);
            uint number = BitConverter.ToUInt32(bytes, 0) % 100000000;
            return number.ToString("D8");
        }

        while (true)
        {
            string candidate = Generate();

            
            string checkQuery = "SELECT COUNT(*) FROM Users WHERE uniqueid = @uid";
            using var checkCmd = new MySqlCommand(checkQuery, conn);
            checkCmd.Parameters.AddWithValue("@uid", candidate);

            var count = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (count == 0)
                return candidate;
        }
    }

    public static int GetUserUniqueId(int userId)
    {
        using var conn = new MySqlConnection(ConnStr);
        conn.Open();

        
        string query = "SELECT UniqueId FROM Users  WHERE Id = @userId";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int uniqueIdOrdinal = reader.GetOrdinal("UniqueId");
            if (reader.IsDBNull(uniqueIdOrdinal))
            {
                reader.Close();
                return CreateAndStoreUniqueId(conn, userId);
            }
            return reader.GetInt32("UniqueId");
        }

        return -1;
    }

    private static int CreateAndStoreUniqueId(MySqlConnection conn, int userId)
    {
        string newId = GenerateUnique8DigitId(conn);
        
        string updateQuery = "UPDATE Users SET UniqueId = @uniqueId WHERE Id = @id";
        using var updateCmd = new MySqlCommand(updateQuery, conn);
        updateCmd.Parameters.AddWithValue("@uniqueId", newId);
        updateCmd.Parameters.AddWithValue("@id", userId);
        updateCmd.ExecuteNonQuery();
        
        return int.Parse(newId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
