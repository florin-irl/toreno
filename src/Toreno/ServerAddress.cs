namespace Toreno;

public static class ServerAddress
{
    public static bool TryParse(string input, out string host, out ushort port)
    {
        host = "";
        port = 0;

        var separatorIndex = input.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == input.Length - 1)
        {
            return false;
        }

        if (!ushort.TryParse(input[(separatorIndex + 1)..], out port))
        {
            return false;
        }

        host = input[..separatorIndex];
        return true;
    }
}
