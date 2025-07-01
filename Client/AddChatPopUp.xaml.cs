using System.Windows;
using LastMessenger;
using MySql.Data.MySqlClient;

namespace LastMessenger;
public partial class AddChatPopUp : Window
{
    private int _currentUserUniqueId;
    private MainWindow _mainWindow;

    public AddChatPopUp(MainWindow mainWindow, int currentUserUniqueId)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _currentUserUniqueId = currentUserUniqueId;
        CurrentUserUniqueIdText.Text = currentUserUniqueId.ToString();
    }

    private void StartChat_Click(object sender, RoutedEventArgs e)
    {
        string uniqueId = UniqueIdInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(uniqueId)) return;

        using var conn = new MySqlConnection("server=localhost;user=root;password=AtesBebis_20;database=rider_dbb;");
        conn.Open();

        string query = "SELECT Id, Username FROM Users WHERE uniqueid = @uid";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@uid", uniqueId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int otherUserId = reader.GetInt32("Id");
            string otherUsername = reader.GetString("Username");

            _mainWindow.StartChatWithUser(otherUserId, otherUsername);
            Close();
        }
        else
        {
            MessageBox.Show("Couldn't find user");
        }
    }
    
    private void CopyUniqueId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(CurrentUserUniqueIdText.Text);
    }
}