using System.ComponentModel;
using System.Runtime.CompilerServices;

public class MessageModel : INotifyPropertyChanged
{
    public int ChatId { get; set; }

    public int UserId { get; set; }

    public string MessageText { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.Now;
    public string IsGroup { get; set; }
    
    public MessageModel() { }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}