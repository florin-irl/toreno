using Toreno.Polling;
using Toreno.Samp;

namespace Toreno.Tests;

public class JoinDetectorTests
{
    [Fact]
    public void DetectJoins_FirstPoll_WatchedPlayerAlreadyOnline_IsDetected()
    {
        var current = new List<SampPlayer> { new("CJ", 0), new("Sweet", 0) };

        var joined = JoinDetector.DetectJoins(previousNames: null, current, watchUsernames: ["CJ"]);

        Assert.Single(joined);
        Assert.Equal("CJ", joined[0].Name);
    }

    [Fact]
    public void DetectJoins_FirstPoll_NoWatchedPlayersOnline_ReturnsNothing()
    {
        var current = new List<SampPlayer> { new("Sweet", 0) };

        var joined = JoinDetector.DetectJoins(previousNames: null, current, watchUsernames: ["CJ"]);

        Assert.Empty(joined);
    }

    [Fact]
    public void DetectJoins_NewWatchedPlayer_IsDetected()
    {
        var previous = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sweet" };
        var current = new List<SampPlayer> { new("Sweet", 0), new("CJ", 0) };

        var joined = JoinDetector.DetectJoins(previous, current, watchUsernames: ["CJ"]);

        Assert.Single(joined);
        Assert.Equal("CJ", joined[0].Name);
    }

    [Fact]
    public void DetectJoins_NewUnwatchedPlayer_IsIgnored()
    {
        var previous = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sweet" };
        var current = new List<SampPlayer> { new("Sweet", 0), new("SomeoneElse", 0) };

        var joined = JoinDetector.DetectJoins(previous, current, watchUsernames: ["CJ"]);

        Assert.Empty(joined);
    }

    [Fact]
    public void DetectJoins_PlayerAlreadyOnline_IsNotReDetected()
    {
        var previous = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CJ" };
        var current = new List<SampPlayer> { new("CJ", 0) };

        var joined = JoinDetector.DetectJoins(previous, current, watchUsernames: ["CJ"]);

        Assert.Empty(joined);
    }

    [Fact]
    public void DetectJoins_UsernameMatchIsCaseInsensitive()
    {
        var previous = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sweet" };
        var current = new List<SampPlayer> { new("Sweet", 0), new("cj", 0) };

        var joined = JoinDetector.DetectJoins(previous, current, watchUsernames: ["CJ"]);

        Assert.Single(joined);
        Assert.Equal("cj", joined[0].Name);
    }
}
