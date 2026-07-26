namespace Toreno.Config;

public sealed class AppConfig
{
    public int PollIntervalSeconds { get; set; } = 15;
    public List<WatchedServer> Servers { get; set; } = new();
}
