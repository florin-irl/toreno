using System.Buffers.Binary;
using System.Net;
using System.Text;
using Toreno.Samp;

namespace Toreno.Tests;

public class SampQueryClientTests
{
    [Fact]
    public void ParsePlayerListResponse_ParsesNamesAndScores()
    {
        var buffer = BuildPlayerListResponse(
            IPAddress.Parse("127.0.0.1"), 7777,
            ("CJ", 100), ("Sweet", 50));

        var players = SampQueryClient.ParsePlayerListResponse(buffer);

        Assert.Equal(2, players.Count);
        Assert.Equal("CJ", players[0].Name);
        Assert.Equal(100, players[0].Score);
        Assert.Equal("Sweet", players[1].Name);
        Assert.Equal(50, players[1].Score);
    }

    [Fact]
    public void ParsePlayerListResponse_EmptyServer_ReturnsEmptyList()
    {
        var buffer = BuildPlayerListResponse(IPAddress.Parse("127.0.0.1"), 7777);

        var players = SampQueryClient.ParsePlayerListResponse(buffer);

        Assert.Empty(players);
    }

    [Fact]
    public void ParsePlayerListResponse_RejectsBadMagic()
    {
        var buffer = BuildPlayerListResponse(IPAddress.Parse("127.0.0.1"), 7777);
        buffer[0] = (byte)'X';

        Assert.Throws<SampQueryException>(() => SampQueryClient.ParsePlayerListResponse(buffer));
    }

    [Fact]
    public void ParseServerInfoResponse_ParsesRealCapturedResponse()
    {
        // Captured live from blue.bugged.ro:7777 (opcode 'i') during manual protocol
        // verification -- pins the parser against real bytes, not just synthetic ones.
        const string hex =
            "53-41-4D-50-95-CA-58-C8-61-1E-69-00-EC-02-E8-03-1A-00-00-00-62-6C-75-65-2E-62-75-67-67-65-64-2E-72-" +
            "6F-20-7C-20-35-30-30-30-20-5A-49-4C-45-0C-00-00-00-42-75-67-67-65-64-20-31-33-2E-31-37-05-00-00-00-" +
            "52-4F-2F-45-4E";
        var buffer = Convert.FromHexString(hex.Replace("-", ""));

        var info = SampQueryClient.ParseServerInfoResponse(buffer);

        Assert.False(info.HasPassword);
        Assert.Equal(748, info.Players);
        Assert.Equal(1000, info.MaxPlayers);
        Assert.Equal("blue.bugged.ro | 5000 ZILE", info.Hostname);
        Assert.Equal("Bugged 13.17", info.Gamemode);
        Assert.Equal("RO/EN", info.Language);
    }

    [Fact]
    public void ParsePlayerListResponse_RejectsTruncatedPayload()
    {
        var buffer = BuildPlayerListResponse(
            IPAddress.Parse("127.0.0.1"), 7777,
            ("CJ", 100));
        var truncated = buffer[..^2];

        Assert.Throws<SampQueryException>(() => SampQueryClient.ParsePlayerListResponse(truncated));
    }

    private static byte[] BuildPlayerListResponse(IPAddress address, ushort port, params (string Name, int Score)[] players)
    {
        var bytes = new List<byte>();
        bytes.AddRange("SAMP"u8.ToArray());
        bytes.AddRange(address.GetAddressBytes());

        var portBytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(portBytes, port);
        bytes.AddRange(portBytes);

        bytes.Add((byte)'c');

        var countBytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(countBytes, (ushort)players.Length);
        bytes.AddRange(countBytes);

        foreach (var (name, score) in players)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            bytes.Add((byte)nameBytes.Length);
            bytes.AddRange(nameBytes);

            var scoreBytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(scoreBytes, score);
            bytes.AddRange(scoreBytes);
        }

        return bytes.ToArray();
    }
}
