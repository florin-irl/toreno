using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Toreno;

/// <summary>
/// A player currently online on a queried server, with whether they're already
/// in that server's watch list -- so the "server players" list can show/toggle
/// watched state without a round trip through the underlying WatchedServer.
/// </summary>
public sealed class ServerPlayerItem : INotifyPropertyChanged
{
    public string Name { get; }

    public ServerPlayerItem(string name, bool isWatched)
    {
        Name = name;
        _isWatched = isWatched;
    }

    private bool _isWatched;
    public bool IsWatched
    {
        get => _isWatched;
        set
        {
            if (_isWatched == value)
            {
                return;
            }

            _isWatched = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWatched)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
