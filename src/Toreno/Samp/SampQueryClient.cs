using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Toreno.Samp;

/// <summary>
/// Speaks the SA-MP UDP server query protocol -- the same public, unauthenticated
/// protocol server browsers use to list players. See https://sampwiki.blast.hk/wiki/Query_Mechanism.
/// </summary>
public static class SampQueryClient
{
    private const byte InfoOpcode = (byte)'i';
    private const byte PlayerListOpcode = (byte)'c';
    private const int HeaderLength = 11;
    private static readonly byte[] Magic = "SAMP"u8.ToArray();

    public static async Task<SampServerInfo> GetServerInfoAsync(
        string host, ushort port, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var buffer = await QueryAsync(host, port, InfoOpcode, timeout, cancellationToken).ConfigureAwait(false);
        return ParseServerInfoResponse(buffer);
    }

    public static async Task<IReadOnlyList<SampPlayer>> GetPlayersAsync(
        string host, ushort port, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var buffer = await QueryAsync(host, port, PlayerListOpcode, timeout, cancellationToken).ConfigureAwait(false);
        return ParsePlayerListResponse(buffer);
    }

    /// <summary>
    /// Servers with a large max-slot count silently drop the player-list opcode
    /// (an anti-amplification measure) -- this distinguishes "server unreachable"
    /// from "server reachable but won't answer that opcode" so the UI can tell
    /// the two apart instead of just timing out with no explanation.
    /// </summary>
    public static async Task<ServerQuerySupport> CheckPlayerListSupportAsync(
        string host, ushort port, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            await GetServerInfoAsync(host, port, timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (SampQueryException)
        {
            return ServerQuerySupport.Unreachable;
        }

        try
        {
            await GetPlayersAsync(host, port, timeout, cancellationToken).ConfigureAwait(false);
            return ServerQuerySupport.Supported;
        }
        catch (SampQueryException)
        {
            return ServerQuerySupport.PlayerListDisabled;
        }
    }

    public static SampServerInfo ParseServerInfoResponse(byte[] buffer)
    {
        ValidateHeader(buffer, InfoOpcode);

        var offset = HeaderLength;
        RequireBytes(buffer, offset, 5);
        var hasPassword = buffer[offset] != 0;
        offset += 1;
        var players = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, 2));
        offset += 2;
        var maxPlayers = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, 2));
        offset += 2;

        var hostname = ReadLengthPrefixedString(buffer, ref offset);
        var gamemode = ReadLengthPrefixedString(buffer, ref offset);
        var language = ReadLengthPrefixedString(buffer, ref offset);

        return new SampServerInfo(hasPassword, players, maxPlayers, hostname, gamemode, language);
    }

    public static IReadOnlyList<SampPlayer> ParsePlayerListResponse(byte[] buffer)
    {
        ValidateHeader(buffer, PlayerListOpcode);

        var offset = HeaderLength;
        RequireBytes(buffer, offset, 2);
        var playerCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset, 2));
        offset += 2;

        var players = new List<SampPlayer>(playerCount);
        for (var i = 0; i < playerCount; i++)
        {
            RequireBytes(buffer, offset, 1);
            var nameLength = buffer[offset];
            offset += 1;

            RequireBytes(buffer, offset, nameLength + 4);
            var name = Encoding.ASCII.GetString(buffer, offset, nameLength);
            offset += nameLength;

            var score = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset, 4));
            offset += 4;

            players.Add(new SampPlayer(name, score));
        }

        return players;
    }

    private static async Task<byte[]> QueryAsync(
        string host, ushort port, byte opcode, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var address = await ResolveAsync(host, cancellationToken).ConfigureAwait(false);
        var endpoint = new IPEndPoint(address, port);
        var request = BuildRequest(address, port, opcode);

        using var udp = new UdpClient();
        await udp.SendAsync(request, request.Length, endpoint).ConfigureAwait(false);

        var receiveTask = udp.ReceiveAsync();
        var completed = await Task.WhenAny(receiveTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

        if (completed != receiveTask)
        {
            // The socket is disposed on return, which will fault the still-pending
            // receive -- observe that fault so it doesn't surface as an unobserved
            // task exception later.
            _ = receiveTask.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

            cancellationToken.ThrowIfCancellationRequested();
            throw new SampQueryException($"Timed out waiting for a response from {host}:{port}.");
        }

        var result = await receiveTask.ConfigureAwait(false);
        return result.Buffer;
    }

    private static async Task<IPAddress> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var parsed))
        {
            return parsed;
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        var ipv4 = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork);
        return ipv4 ?? throw new SampQueryException($"Could not resolve host '{host}' to an IPv4 address.");
    }

    private static byte[] BuildRequest(IPAddress address, ushort port, byte opcode)
    {
        var ipBytes = address.GetAddressBytes();
        if (ipBytes.Length != 4)
        {
            throw new SampQueryException("Only IPv4 addresses are supported for SA-MP queries.");
        }

        var packet = new byte[HeaderLength];
        Encoding.ASCII.GetBytes("SAMP", 0, 4, packet, 0);
        ipBytes.CopyTo(packet, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(8, 2), port);
        packet[10] = opcode;
        return packet;
    }

    private static void ValidateHeader(byte[] buffer, byte expectedOpcode)
    {
        if (buffer.Length < HeaderLength || !buffer.AsSpan(0, 4).SequenceEqual(Magic))
        {
            throw new SampQueryException("Response was not a valid SA-MP query packet.");
        }

        if (buffer[10] != expectedOpcode)
        {
            throw new SampQueryException(
                $"Unexpected opcode 0x{buffer[10]:X2} in response (expected 0x{expectedOpcode:X2}).");
        }
    }

    private static string ReadLengthPrefixedString(byte[] buffer, ref int offset)
    {
        RequireBytes(buffer, offset, 4);
        var length = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset, 4));
        offset += 4;

        if (length < 0)
        {
            throw new SampQueryException("Response contained a negative string length.");
        }

        RequireBytes(buffer, offset, length);
        var value = Encoding.ASCII.GetString(buffer, offset, length);
        offset += length;
        return value;
    }

    private static void RequireBytes(byte[] buffer, int offset, int count)
    {
        if (offset + count > buffer.Length)
        {
            throw new SampQueryException("Response was truncated.");
        }
    }
}
