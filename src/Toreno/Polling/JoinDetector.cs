using Toreno.Samp;

namespace Toreno.Polling;

/// <summary>
/// Pure diffing logic, kept separate from the network/scheduling loop so it can be
/// tested without a live server: given who was online last time and who's online now,
/// which watched usernames just joined.
/// </summary>
public static class JoinDetector
{
    public static IReadOnlyList<SampPlayer> DetectJoins(
        HashSet<string>? previousNames,
        IReadOnlyList<SampPlayer> currentPlayers,
        IReadOnlyList<string> watchUsernames)
    {
        // No previous snapshot means this is the first poll (or the first poll
        // after regaining reachability) -- report anyone already online who's
        // being watched for, rather than waiting for a subsequent join. Otherwise
        // watching a friend who's already connected would never fire at all.
        if (previousNames == null)
        {
            return currentPlayers
                .Where(p => watchUsernames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var joined = new List<SampPlayer>();
        foreach (var player in currentPlayers)
        {
            if (!previousNames.Contains(player.Name) &&
                watchUsernames.Contains(player.Name, StringComparer.OrdinalIgnoreCase))
            {
                joined.Add(player);
            }
        }

        return joined;
    }
}
