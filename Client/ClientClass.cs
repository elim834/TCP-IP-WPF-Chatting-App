using System.Net.Sockets;
using System.Text.Json;
using System.Windows.Threading;


namespace LastMessenger
{
    public class ClientClass
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private readonly MainWindow _mainWindow;
        
        public ClientClass(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }
        public async Task StartClient(string ip, int port)
        {
            try
            {
                    _tcpClient = new TcpClient();
                    Console.WriteLine("Connecting...");
                    await _tcpClient.ConnectAsync(ip, port);
                    _stream = _tcpClient.GetStream();
                    Console.WriteLine("Connected!");

                    var identityMessage = new
                    {
                        Type = "identity",
                        UserId = MainWindow.LoggedInUserId.ToString()
                    };                
                    
                    string json = JsonSerializer.Serialize(identityMessage);
                    await TcpMessageHelper.SendMessageAsync(_stream, json);

                    _ = ReceiveMessageAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}"); 
            }
        }

        public async Task ReceiveMessageAsync()
        {
            if (_tcpClient ==null || !_tcpClient.Connected || _stream == null)
            {
                Console.WriteLine("Client not connected.");
                return;
            }

            try
            {
                while (true)
                {
                    string? messageJson = await TcpMessageHelper.ReceiveMessageAsync(_stream);
                    if (messageJson == null) break;

                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(messageJson);

                    if (dict != null && dict.ContainsKey("MessageText") && dict.ContainsKey("SenderId"))
                    {
                        int senderId = int.Parse(dict["SenderId"]);
                        int chatId = await Task.Run(() =>
                            ChatListData.GetOrCreateChatId(MainWindow.LoggedInUserId, int.Parse(dict["SenderId"])));

                        var message = new MessageModel
                        {
                            ChatId = chatId,
                            UserId = senderId,
                            MessageText = dict["MessageText"],
                            SentAt = DateTime.Now
                        };

                        _mainWindow.Dispatcher.Invoke(() =>
                        {
                            var chat = _mainWindow.ChatListData.Chats.FirstOrDefault(x => x.ChatId == chatId);
                            if (chat == null)
                            {
                                Console.WriteLine($"ChatId for {chatId} not found in local list.");
                                return;
                            }

                            chat.Message.Add(message);
                            if (_mainWindow.ChatListData.SelectedChat != null && _mainWindow.ChatListData.SelectedChat.ChatId == chatId)
                            {
                                _mainWindow.ViewModel.Messages.Add(message);
                            }

                            MessageToLocal.SaveMessagesToLocal(
                                chat.Message.ToList(),
                                MainWindow.LoggedInUserId,
                                chat.ChatId.ToString());
                        }, DispatcherPriority.Background);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
            finally
            {
                _tcpClient.Close();
            }
        }

        public async void SendMessage(int senderId, int receiverId, string messageText)
        {
            if (_tcpClient == null|| !_tcpClient.Connected || _stream == null) return;
                
            var message = new
            {
                Type = "message",
                SenderId = senderId.ToString(),
                ReceiverId = receiverId.ToString(),
                MessageText = messageText,
                SentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonSerializer.Serialize(message);
            
            try
            {
                await TcpMessageHelper.SendMessageAsync(_stream, json);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client message sending error"+e);
            }
            
        }

    }
}
