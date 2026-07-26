using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Toreno.Config;
using Toreno.Notifications;
using Toreno.Samp;

namespace Toreno;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(3);

    private readonly AppConfig _config;
    private readonly ObservableCollection<ServerListItem> _servers = new();

    public MainWindow(AppConfig config)
    {
        InitializeComponent();

        _config = config;
        ServersListBox.ItemsSource = _servers;

        foreach (var server in _config.Servers)
        {
            var item = new ServerListItem(server);
            _servers.Add(item);
            _ = RefreshServerStatusAsync(item);
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        // Closing the window just hides it -- the app keeps running in the tray
        // until "Exit" is chosen from the tray icon's context menu.
        e.Cancel = true;
        Hide();
    }

    private async void AddServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var addressInput = AddressTextBox.Text.Trim();
        if (!ServerAddress.TryParse(addressInput, out _, out _))
        {
            MessageBox.Show(this, "Enter an address as host:port, e.g. 127.0.0.1:7777", "Toreno");
            return;
        }

        var server = new WatchedServer
        {
            Address = addressInput,
            Name = NameTextBox.Text.Trim()
        };

        _config.Servers.Add(server);
        ConfigStore.Save(_config);

        var item = new ServerListItem(server);
        _servers.Add(item);

        AddressTextBox.Text = "";
        NameTextBox.Text = "";

        await RefreshServerStatusAsync(item);
    }

    private void TestNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        NotificationService.ShowPlayerJoined("Test Server", "TestPlayer");
    }

    private async void RecheckServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is ServerListItem selected)
        {
            await RefreshServerStatusAsync(selected);
        }
    }

    private void RemoveServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is not ServerListItem selected)
        {
            return;
        }

        _config.Servers.Remove(selected.Server);
        ConfigStore.Save(_config);
        _servers.Remove(selected);
    }

    private void ServersListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ServersListBox.SelectedItem as ServerListItem;

        UsernamesListBox.Items.Clear();
        var hasSelection = selected != null;
        UsernameTextBox.IsEnabled = hasSelection;
        AddUsernameButton.IsEnabled = hasSelection;
        RemoveUsernameButton.IsEnabled = hasSelection;
        RecheckServerButton.IsEnabled = hasSelection;
        RemoveServerButton.IsEnabled = hasSelection;

        if (selected == null)
        {
            return;
        }

        foreach (var name in selected.Server.WatchUsernames)
        {
            UsernamesListBox.Items.Add(name);
        }
    }

    private void AddUsernameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is not ServerListItem selected)
        {
            return;
        }

        var name = UsernameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name) ||
            selected.Server.WatchUsernames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        selected.Server.WatchUsernames.Add(name);
        ConfigStore.Save(_config);
        UsernamesListBox.Items.Add(name);
        UsernameTextBox.Text = "";
    }

    private void RemoveUsernameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is not ServerListItem selected)
        {
            return;
        }

        if (UsernamesListBox.SelectedItem is not string name)
        {
            return;
        }

        selected.Server.WatchUsernames.Remove(name);
        ConfigStore.Save(_config);
        UsernamesListBox.Items.Remove(name);
    }

    private async Task RefreshServerStatusAsync(ServerListItem item)
    {
        item.StatusText = "Checking...";
        item.StatusColor = ThemeBrush("TextSecondaryBrush");

        if (!ServerAddress.TryParse(item.Server.Address, out var host, out var port))
        {
            item.StatusText = "Invalid address";
            item.StatusColor = ThemeBrush("DangerBrush");
            return;
        }

        var support = await SampQueryClient.CheckPlayerListSupportAsync(host, port, QueryTimeout);

        (item.StatusText, item.StatusColor) = support switch
        {
            ServerQuerySupport.Supported =>
                ("Player-list queries supported", ThemeBrush("SuccessBrush")),
            ServerQuerySupport.PlayerListDisabled =>
                ("⚠ This server has disabled player-list queries -- Toreno can't detect specific players here.",
                    ThemeBrush("WarningBrush")),
            ServerQuerySupport.Unreachable =>
                ("✕ Could not reach this server", ThemeBrush("DangerBrush")),
            _ => ("Unknown", ThemeBrush("TextSecondaryBrush"))
        };
    }

    private static Brush ThemeBrush(string resourceKey) => (Brush)Application.Current.Resources[resourceKey];
}
