using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public static class TcpMessageHelper
{
    public static async Task SendMessageAsync(NetworkStream stream, string jsonMessage)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length); 

        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length); 
        await stream.WriteAsync(messageBytes, 0, messageBytes.Length); 
        await stream.FlushAsync();
    }

    public static async Task<string?> ReceiveMessageAsync(NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        int totalBytesReadForLength = 0;
        while (totalBytesReadForLength < lengthBuffer.Length)
        {
            int bytesRead = await stream.ReadAsync(lengthBuffer, totalBytesReadForLength, lengthBuffer.Length - totalBytesReadForLength);
            if (bytesRead == 0) return null; 
            totalBytesReadForLength += bytesRead;
        }

        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        byte[] messageBuffer = new byte[messageLength];
        int totalBytesReadForMessage = 0;
        while (totalBytesReadForMessage < messageLength)
        {
            int bytesRead = await stream.ReadAsync(messageBuffer, totalBytesReadForMessage, messageLength - totalBytesReadForMessage);
            if (bytesRead == 0) return null; 
            totalBytesReadForMessage += bytesRead;
        }

        return Encoding.UTF8.GetString(messageBuffer, 0, totalBytesReadForMessage);
    }
}