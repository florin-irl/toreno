namespace Toreno.Samp;

/// <summary>
/// Whether a server answers the player-list query opcode. Servers with a large
/// max-slot count disable it (an anti-UDP-amplification measure), in which case
/// Toreno cannot see individual player names on that server at all.
/// </summary>
public enum ServerQuerySupport
{
    Unreachable,
    PlayerListDisabled,
    Supported
}
