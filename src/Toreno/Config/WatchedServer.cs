namespace Toreno.Config;

public sealed class WatchedServer
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public List<string> WatchUsernames { get; set; } = new();
}
