using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace MarkPos.UI;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        Close();
    }
}