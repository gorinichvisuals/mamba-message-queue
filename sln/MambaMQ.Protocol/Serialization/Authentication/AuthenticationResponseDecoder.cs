namespace MambaMQ.Protocol.Serialization.Authentication;

public static class AuthenticationResponseDecoder
{
    public static AuthenticationResponse Decode(ReadOnlySpan<byte> buffer)
    {
        const int successSize = sizeof(byte);
        const int errorMessageLengthSize = sizeof(int);
        const int headerSize = successSize + errorMessageLengthSize;

        if (buffer.Length < headerSize)
            throw new InvalidDataException("Authentication response does not contain complete header.");

        byte success = buffer[0];

        if (success is not 0 and not 1)
            throw new InvalidDataException("Authentication response contains invalid Success value.");

        int errorMessageLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(successSize, errorMessageLengthSize));

        if (errorMessageLength < 0)
            throw new InvalidDataException("Authentication error message length cannot be negative.");

        if (buffer.Length < headerSize + errorMessageLength)
            throw new InvalidDataException("Authentication response does not contain complete error message.");

        string? errorMessage = errorMessageLength is 0
            ? null
            : Encoding.UTF8.GetString(buffer.Slice(headerSize, errorMessageLength));

        return new AuthenticationResponse(success is 1, errorMessage);
    }
}