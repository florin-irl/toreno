using System.ComponentModel;
using System.Windows;

namespace Toreno;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        // Closing the window just hides it -- the app keeps running in the tray
        // until "Exit" is chosen from the tray icon's context menu.
        e.Cancel = true;
        Hide();
    }
}
