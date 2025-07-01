using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using Server;

namespace LastMessenger;

public partial class PopUp : Window
{
    public bool UserClosedWithoutTrying { get; private set; } = false;
    
    public PopUp()
        {
            InitializeComponent();
            this.Closed += PopUp_Closed;
            Topmost = true;
        }

    private void PopUp_Closed(object? sender, EventArgs e)
    {
        if (DialogResult != true)
        {
            UserClosedWithoutTrying = true;
        }
    }

    public void SignInResponse(string username, string password)
    {
        try
        {
            PasswordHash.Program.Result result = Server.PasswordHash.Program.SignInRequest(username, password);
            
            if (result.Success)
            {
                MainWindow.LoggedInUserId = result.UserId ?? -1; 
                MainWindow.LoggedInUsername = result.Username ?? "";
                
                DialogResult = true; 

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.HandleLoginSuccess(MainWindow.LoggedInUserId, MainWindow.LoggedInUsername);
                }
                
                this.Close(); 
            }

            else
            {
                MessageBox.Show(result.Message ?? "Login failed.");
                PasswordTextBox.Clear();
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("Unexpected error: " + e.Message);
        }
    }

   

    private void SignIn_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter both username and password.");
            return;
        }

        SignInResponse(username, password);
    }

    

    private void MessagesTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SignIn_Click(this, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Enter));
            e.Handled = true; 
        }
    }

    private void SignUp_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordTextBox.Text;
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Please enter both username and password for sign-up.");
            return;
        }
        
        PasswordHash.Program.Result result = Server.PasswordHash.Program.SignUpRequest(UsernameTextBox.Text, PasswordTextBox.Text); 
        
        if (result.Success)
        {
            MessageBox.Show("Sign-up successful! You can now sign in.");
            if (result.UserId.HasValue)
            {
                ChatListData.GetUserUniqueId(result.UserId.Value);
            }
            
            UsernameTextBox.Clear();
            PasswordTextBox.Clear();
        }
        else
        {
            MessageTextBlock.Text = result.Message ?? "Sign-up failed.";
        }
    }
}