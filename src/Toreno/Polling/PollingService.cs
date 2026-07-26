using Toreno.Config;
using Toreno.Samp;

namespace Toreno.Polling;

/// <summary>
/// Runs continuously in the background for as long as the app is alive (owned by App,
/// not the window), polling every watched server and firing <see cref="PlayerJoined"/>
/// when a watched username newly appears.
/// </summary>
public sealed class PollingService : IDisposable
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    public event Action<WatchedServer, SampPlayer>? PlayerJoined;

    private readonly AppConfig _config;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, ServerPollState> _state = new();
    private Task? _loopTask;

    public PollingService(AppConfig config)
    {
        _config = config;
    }

    public void Start()
    {
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var server in _config.Servers.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await PollServerAsync(server, cancellationToken).ConfigureAwait(false);
                }

                var intervalSeconds = Math.Max(1, _config.PollIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task PollServerAsync(WatchedServer server, CancellationToken cancellationToken)
    {
        if (server.WatchUsernames.Count == 0 || !ServerAddress.TryParse(server.Address, out var host, out var port))
        {
            return;
        }

        var state = GetState(server.Address);
        if (DateTimeOffset.UtcNow < state.NextAttemptAt)
        {
            return;
        }

        IReadOnlyList<SampPlayer> players;
        try
        {
            players = await SampQueryClient.GetPlayersAsync(host, port, QueryTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SampQueryException)
        {
            RecordFailure(state);
            return;
        }

        state.ConsecutiveFailures = 0;
        state.NextAttemptAt = DateTimeOffset.MinValue;

        var joined = JoinDetector.DetectJoins(state.LastKnownPlayers, players, server.WatchUsernames);
        foreach (var player in joined)
        {
            PlayerJoined?.Invoke(server, player);
        }

        state.LastKnownPlayers = new HashSet<string>(
            players.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
    }

    private ServerPollState GetState(string address)
    {
        if (!_state.TryGetValue(address, out var state))
        {
            state = new ServerPollState();
            _state[address] = state;
        }

        return state;
    }

    private void RecordFailure(ServerPollState state)
    {
        state.ConsecutiveFailures++;
        var intervalSeconds = Math.Max(1, _config.PollIntervalSeconds);
        var backoffSeconds = Math.Min(
            MaxBackoff.TotalSeconds,
            intervalSeconds * Math.Pow(2, state.ConsecutiveFailures - 1));
        state.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
    }

    private sealed class ServerPollState
    {
        public int ConsecutiveFailures;
        public DateTimeOffset NextAttemptAt = DateTimeOffset.MinValue;
        public HashSet<string>? LastKnownPlayers;
    }
}
