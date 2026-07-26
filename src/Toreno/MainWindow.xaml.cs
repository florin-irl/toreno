using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Toreno.Config;
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
    private readonly ObservableCollection<ServerPlayerItem> _serverPlayers = new();

    public MainWindow(AppConfig config)
    {
        InitializeComponent();

        _config = config;
        ServersListBox.ItemsSource = _servers;
        ServerPlayersListBox.ItemsSource = _serverPlayers;

        foreach (var server in _config.Servers)
        {
            var item = new ServerListItem(server);
            _servers.Add(item);
            _ = RefreshServerStatusAsync(item);
        }
    }

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
    }

    private void AboutButton_OnClick(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        // Closing the window just hides it -- the app keeps running in the tray
        // until "Exit" is chosen from the tray icon's context menu.
        e.Cancel = true;
        Hide();
    }

    private async void AddServerIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddServerWindow(_config.Servers) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var server = new WatchedServer
        {
            Address = dialog.AddressInput,
            Name = dialog.DisplayNameInput
        };

        _config.Servers.Add(server);
        ConfigStore.Save(_config);

        var item = new ServerListItem(server);
        _servers.Add(item);

        await RefreshServerStatusAsync(item);
    }

    private async void ItemRecheckButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ServerListItem item)
        {
            await RefreshServerStatusAsync(item);
        }
    }

    private void ItemRemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not ServerListItem item)
        {
            return;
        }

        _config.Servers.Remove(item.Server);
        ConfigStore.Save(_config);
        _servers.Remove(item);
    }

    private void ServersListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ServersListBox.SelectedItem as ServerListItem;

        UsernamesListBox.Items.Clear();
        _serverPlayers.Clear();

        AddUsernameIconButton.IsEnabled = selected != null;

        if (selected == null)
        {
            return;
        }

        foreach (var name in selected.Server.WatchUsernames)
        {
            UsernamesListBox.Items.Add(name);
        }

        _ = RefreshServerPlayersAsync(selected);
    }

    private async void RefreshServerPlayersButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is ServerListItem selected)
        {
            await RefreshServerPlayersAsync(selected);
        }
    }

    private void ServerPlayersListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServerPlayersListBox.SelectedItem is ServerPlayerItem item &&
            ServersListBox.SelectedItem is ServerListItem selected)
        {
            if (item.IsWatched)
            {
                RemoveWatchedUsername(selected, item.Name);
            }
            else
            {
                AddWatchedUsername(selected, item.Name);
            }
        }

        // Selection is used as a momentary "click" signal, not a persistent highlight --
        // IsWatched already carries the visual state, so clear it to allow re-clicking.
        ServerPlayersListBox.SelectedItem = null;
    }

    private void AddUsernameIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is not ServerListItem selected)
        {
            return;
        }

        var dialog = new AddUsernameWindow(selected.Server.WatchUsernames) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        AddWatchedUsername(selected, dialog.UsernameInput);
    }

    private void RemoveUsernameIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not string name)
        {
            return;
        }

        if (ServersListBox.SelectedItem is not ServerListItem selected)
        {
            return;
        }

        RemoveWatchedUsername(selected, name);
    }

    private void AddWatchedUsername(ServerListItem server, string name)
    {
        if (server.Server.WatchUsernames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        server.Server.WatchUsernames.Add(name);
        ConfigStore.Save(_config);
        UsernamesListBox.Items.Add(name);

        var match = _serverPlayers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.IsWatched = true;
        }
    }

    private void RemoveWatchedUsername(ServerListItem server, string name)
    {
        server.Server.WatchUsernames.Remove(name);
        ConfigStore.Save(_config);
        UsernamesListBox.Items.Remove(name);

        var match = _serverPlayers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.IsWatched = false;
        }
    }

    private async Task RefreshServerPlayersAsync(ServerListItem server)
    {
        _serverPlayers.Clear();

        if (!ServerAddress.TryParse(server.Server.Address, out var host, out var port))
        {
            return;
        }

        try
        {
            var players = await SampQueryClient.GetPlayersAsync(host, port, QueryTimeout);
            foreach (var player in players)
            {
                var isWatched = server.Server.WatchUsernames.Contains(player.Name, StringComparer.OrdinalIgnoreCase);
                _serverPlayers.Add(new ServerPlayerItem(player.Name, isWatched));
            }
        }
        catch (SampQueryException)
        {
            // Server unreachable or has disabled player-list queries -- its status
            // in the Servers list already explains why; this panel just stays empty.
        }
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
