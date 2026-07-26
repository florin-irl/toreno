using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Toreno.Config;
using Toreno.Notifications;
using Toreno.Polling;
using Toreno.Samp;

namespace Toreno;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private AppConfig _config = null!;
    private PollingService? _pollingService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _config = ConfigStore.Load();

        _pollingService = new PollingService(_config);
        _pollingService.PlayerJoined += OnPlayerJoined;
        _pollingService.Start();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Toreno",
            IconSource = BitmapFrame.Create(
                new System.Uri("pack://application:,,,/Resources/tray.ico")),
            ContextMenu = BuildContextMenu()
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private void OnPlayerJoined(WatchedServer server, SampPlayer player)
    {
        var serverLabel = string.IsNullOrWhiteSpace(server.Name) ? server.Address : server.Name;
        Dispatcher.Invoke(() => NotificationService.ShowPlayerJoined(serverLabel, player.Name));
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
        _mainWindow ??= new MainWindow(_config);
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pollingService?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
