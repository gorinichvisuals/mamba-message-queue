namespace MambaMQ.Protocol.Responses;

public static class ProtocolCapabilities
{
    private const byte PayloadVersion = 1;
    private const byte LegacyMaximumCommandType = 27;

    public static IReadOnlySet<FrameType> LegacySupportedCommands { get; } =
        Enum.GetValues<FrameType>()
            .Where(type => IsCommand(type) && (byte)type <= LegacyMaximumCommandType)
            .ToHashSet();

    public static byte[] EncodeCurrentCommands()
    {
        FrameType[] commands = Enum.GetValues<FrameType>()
            .Where(IsCommand)
            .OrderBy(static type => (byte)type)
            .ToArray();

        if (commands.Length > byte.MaxValue)
            throw new InvalidOperationException("Protocol command capability list exceeds the supported size.");

        byte[] payload = new byte[2 + commands.Length];
        payload[0] = PayloadVersion;
        payload[1] = (byte)commands.Length;

        for (int i = 0; i < commands.Length; i++)
            payload[i + 2] = (byte)commands[i];

        return payload;
    }

    public static bool TryDecode(ReadOnlySpan<byte> payload, out IReadOnlySet<FrameType> commands)
    {
        commands = LegacySupportedCommands;

        if (payload.Length < 2 || payload[0] != PayloadVersion || payload[1] != payload.Length - 2)
            return false;

        HashSet<FrameType> decodedCommands = [];
        for (int i = 2; i < payload.Length; i++)
            decodedCommands.Add((FrameType)payload[i]);

        commands = decodedCommands;
        return true;
    }

    public static bool IsCommand(FrameType type)
        => Enum.GetName(type)?.EndsWith("Command", StringComparison.Ordinal) is true;
}