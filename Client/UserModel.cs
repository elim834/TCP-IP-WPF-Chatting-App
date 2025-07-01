using System.Net.Sockets;

namespace LastMessenger;

public class UserModel
{
    public string ConnectedAt { get; set; }
    public string Username { get; set; }
    
    public UserModel(string username)
    {
        Username = username;
        this.ConnectedAt = DateTime.Now.ToString("h:mm:ss tt");
    }
}


