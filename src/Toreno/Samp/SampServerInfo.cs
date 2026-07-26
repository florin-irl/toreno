namespace Toreno.Samp;

public sealed record SampServerInfo(
    bool HasPassword,
    int Players,
    int MaxPlayers,
    string Hostname,
    string Gamemode,
    string Language);
