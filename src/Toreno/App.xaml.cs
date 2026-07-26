using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace Toreno;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Toreno",
            IconSource = BitmapFrame.Create(
                new System.Uri("pack://application:,,,/Resources/tray.ico")),
            ContextMenu = BuildContextMenu()
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private ContextMenu BuildContextMenu()
    {
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();

        var menu = new ContextMenu();
        menu.Items.Add(exitItem);
        return menu;
    }

    private void ShowMainWindow()
    {
        _mainWindow ??= new MainWindow();
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
