using System.Windows;

namespace LastMessenger;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            _mainWindow = new MainWindow();
            Application.Current.MainWindow = _mainWindow;

            var popUp = new PopUp();
            bool? result = popUp.ShowDialog();

            if (result == true)
            {
                _mainWindow.Show(); 

            }
            else if (result == false)
            {
                Shutdown();
                return;
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show("Startup ex:"+exception);
            throw;
        }
        
    }
    

}