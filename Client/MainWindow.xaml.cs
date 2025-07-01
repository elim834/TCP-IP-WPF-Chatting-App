using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient; 

namespace LastMessenger;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ClientClass? ClientClass { get; set; }
    public ChatListData? ChatListData { get; set; }
    public MessageListViewModel ViewModel { get; set; } = new();

    public static int LoggedInUserId { get; set; }
    public static int LoggedInUserUniqueId { get; set; }
    public static string LoggedInUsername { get; set; } = string.Empty; 

    
    private string _displayedUsername = "non-user"; 
    public string DisplayedUsername
    {
        get { return _displayedUsername; }
        set
        {
            if (_displayedUsername != value)
            {
                _displayedUsername = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        
        DataContext = this;
        
        SendButton.Click += SendButton_Click;
        SentView.ItemsSource = ViewModel.Messages;
        
        try
        {
            using var conn = new MySqlConnection("server=localhost;user=root;password=AtesBebis_20;database=rider_dbb;");
            conn.Open();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Database connection error- mainwindow: " + ex.Message);
        }
    }
    
   
    public void HandleLoginSuccess(int userId, string username)
    {
        LoggedInUserId = userId;
        LoggedInUsername = username; 
        DisplayedUsername = username; 
        
        ChatListData = new ChatListData(LoggedInUserId); 
        ChatList.ItemsSource = ChatListData.Chats; 

        ClientClass = new ClientClass(this);
        _ = ClientClass.StartClient("127.0.0.1", 7891); 
    }
        
    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        string messageToSend = MessagesTextBox.Text;
        
        if (string.IsNullOrWhiteSpace(messageToSend)) return;
        
        var selectedChat = ChatListData.SelectedChat;
        if (selectedChat == null) return;

        int chatId = selectedChat.ChatId;
        
        var message = new MessageModel
        {
            ChatId = chatId,
            UserId = LoggedInUserId,
            MessageText = messageToSend,
            SentAt = DateTime.Now,
            IsGroup = selectedChat.IsGroup ? "1": "0"
            
        };
        
        ViewModel.Messages.Add(message);
        selectedChat.Message.Add(message);
        MessagesTextBox.Clear();

        ClientClass?.SendMessage(
            senderId: LoggedInUserId,
            receiverId: selectedChat.ReceiverId,
            messageText: messageToSend
        );

        MessageToLocal.SaveMessagesToLocal(
            selectedChat.Message.ToList(),
            LoggedInUserId,
            selectedChat.ChatId.ToString());
    }
    
    public class MessageListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MessageModel> Messages { get; set; } = new();
        
        public void LoadMessages(int userId, int key)
        {
            Messages.Clear();

            var loadedMessages = MessageToLocal.LoadMessagesFromLocal(userId, key.ToString());
            foreach (var msg in loadedMessages)
            {
                Messages.Add(msg);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private void ChatList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChatList.SelectedItem is ChatListData.ChatModel selectedChat)
        {
            ChatListData.SelectedChat = selectedChat;
            ViewModel.LoadMessages(LoggedInUserId, selectedChat.ChatId);
        }
    }

    private void MessagesTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendButton_Click(null, null); 
            e.Handled = true; 
        }
    }

    private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    private void AddChatButton_Click(object sender, RoutedEventArgs e)
    {
        var popup = new AddChatPopUp(this, ChatListData.GetUserUniqueId(LoggedInUserId));
        popup.ShowDialog();
    }

    public void StartChatWithUser(int otherUserId, string otherUsername)
    {
        int chatId = ChatListData.GetOrCreateChatId(LoggedInUserId, otherUserId);
        int IsGroup = ChatListData.GroupOrNot(chatId);

        if (IsGroup == 0)
        {
            if (ChatListData.Chats.All(c => c.ChatId != chatId))
            {
                var newChat = new ChatListData.ChatModel
                {
                    ChatId = chatId,
                    ReceiverId = otherUserId,
                    DisplayUsername = otherUsername,
                    IsGroup = false
                };

                ChatListData.Chats.Add(newChat);
                ChatListData.SelectedChat = newChat;
                ViewModel.LoadMessages(LoggedInUserId, chatId);
                ChatList.SelectedItem = newChat;
            }
        }
    }

    public void StartChatWithUser(List<int> userIds, string groupKey)
    {
        int chatId = ChatListData.GetGroupChatIdByKey(groupKey);
    
        if (ChatListData.Chats.All(c => c.ChatId != chatId))
        {
            var newChat = new ChatListData.ChatModel
            {
                ChatId = chatId,
                IsGroup = true,
                GroupKey = groupKey,
                DisplayUsername = "Group: " + groupKey
            };

            ChatListData.Chats.Add(newChat);
            ChatListData.SelectedChat = newChat;
            ViewModel.LoadMessages(LoggedInUserId, chatId);
            ChatList.SelectedItem = newChat;
        }
    }


    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void CreateGroupChatButton_Click(object sender, RoutedEventArgs e)
    {
        var groupPopup = new CreateGroupChatButtonPop(LoggedInUserId);
        groupPopup.ShowDialog();
        
        if (!string.IsNullOrWhiteSpace(CreateGroupChatButtonPop.LastGroupKey))
        {
            StartChatWithUser(CreateGroupChatButtonPop.RealUserIds, CreateGroupChatButtonPop.LastGroupKey);
        }
        
    }

}