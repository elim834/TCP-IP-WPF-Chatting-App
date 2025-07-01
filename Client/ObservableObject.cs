using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LastMessenger;

public class ObservableObject : INotifyPropertyChanged
{
   
    public event PropertyChangedEventHandler? PropertyChanged;
        
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}