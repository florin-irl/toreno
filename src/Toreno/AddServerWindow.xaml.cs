using System.Windows;
using Toreno.Config;

namespace Toreno;

/// <summary>
/// Interaction logic for AddServerWindow.xaml
/// </summary>
public partial class AddServerWindow : Window
{
    private readonly IReadOnlyList<WatchedServer> _existingServers;

    public string AddressInput { get; private set; } = "";
    public string DisplayNameInput { get; private set; } = "";

    public AddServerWindow(IReadOnlyList<WatchedServer> existingServers)
    {
        InitializeComponent();
        _existingServers = existingServers;
        AddressTextBox.Focus();
        AddressTextBox.SelectAll();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var addressInput = AddressTextBox.Text.Trim();
        if (!ServerAddress.TryParse(addressInput, out _, out _))
        {
            ShowError("Enter an address as host:port, e.g. 127.0.0.1:7777");
            return;
        }

        if (_existingServers.Any(s => string.Equals(s.Address, addressInput, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("A server with that address is already in your watchlist.");
            return;
        }

        var nameInput = NameTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(nameInput) &&
            _existingServers.Any(s => string.Equals(s.Name, nameInput, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("A server with that display name is already in your watchlist.");
            return;
        }

        AddressInput = addressInput;
        DisplayNameInput = nameInput;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}
