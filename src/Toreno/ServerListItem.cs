using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Toreno.Config;

namespace Toreno;

public sealed class ServerListItem : INotifyPropertyChanged
{
    public WatchedServer Server { get; }

    public ServerListItem(WatchedServer server)
    {
        Server = server;
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Server.Name) ? Server.Address : Server.Name;
    public string Address => Server.Address;

    private string _statusText = "Checking...";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    private Brush _statusColor = Brushes.Gray;
    public Brush StatusColor
    {
        get => _statusColor;
        set => SetField(ref _statusColor, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
