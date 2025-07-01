using System;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;

namespace LastMessenger;

public partial class CreateGroupChatButtonPop : Window
{
    private readonly int _currentUserId;
    public static List<int>RealUserIds = new List<int>();
    public static string LastGroupKey { get; private set; } = "";


    public CreateGroupChatButtonPop(int currentUserId)
    {
        InitializeComponent();
        _currentUserId = currentUserId;
    }

    private void CreateGroup_Click(object sender, RoutedEventArgs e)
    {
        string groupKey = GroupKeyInput.Text.Trim();
        string rawIds = UniqueIdsInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(groupKey) || string.IsNullOrWhiteSpace(rawIds))
        {
            MessageBox.Show("Please enter both group name and user IDs.");
            return;
        }

        RealUserIds.Clear();

        var memberIds = rawIds.Split(',')
            .Select(id => id.Trim())
            .Where(id => int.TryParse(id, out _))
            .Select(int.Parse)
            .ToList();

        
        if (!memberIds.Contains(_currentUserId))
        {
            memberIds.Add(_currentUserId);
        }

        string connStr = "server=localhost;user=root;password=AtesBebis_20;database=rider_dbb;";
        using var conn = new MySqlConnection(connStr);
        conn.Open();

        foreach (var id in memberIds)
        {
            string lookupQuery = "SELECT Id FROM Users WHERE uniqueid = @uid";
            using var lookupCmd = new MySqlCommand(lookupQuery, conn);
            lookupCmd.Parameters.AddWithValue("@uid", id.ToString());
            using var reader = lookupCmd.ExecuteReader();
            if (reader.Read())
            {
                RealUserIds.Add(reader.GetInt32("Id"));
            }
            reader.Close(); 
        }
        
        string insertChatQuery = "INSERT INTO Chats (SenderId, IsGroup, GroupKey) VALUES (@senderId, 1, @groupKey)";
        using var cmd = new MySqlCommand(insertChatQuery, conn);
        cmd.Parameters.AddWithValue("@senderId", _currentUserId);
        cmd.Parameters.AddWithValue("@groupKey", groupKey);
        cmd.ExecuteNonQuery();

        int groupChatId = (int)cmd.LastInsertedId;

        foreach (int userId in RealUserIds.Distinct())
        {
            string insertMemberQuery = "INSERT INTO chatmembers (GroupChatId, UserId) VALUES (@chatId, @userId)";
            using var memberCmd = new MySqlCommand(insertMemberQuery, conn);
            memberCmd.Parameters.AddWithValue("@chatId", groupChatId);
            memberCmd.Parameters.AddWithValue("@userId", userId);
            memberCmd.ExecuteNonQuery();
        }

        LastGroupKey = groupKey;
        
        MessageBox.Show("Group created!");
        Close();
    }
}
