namespace MambaMQ.Protocol.Serialization.Responses;

public static class CommandResponseDecoder
{
    public static CommandResponse Decode(ReadOnlySpan<byte> buffer)
    {
        const int successSize = sizeof(byte);
        const int errorCodeSize = sizeof(byte);
        const int errorMessageLengthSize = sizeof(int);

        const int headerSize =
            successSize +
            errorCodeSize +
            errorMessageLengthSize;

        if (buffer.Length < headerSize)
            throw new InvalidDataException("Command response does not contain complete header.");

        int offset = 0;

        byte success = buffer[offset++];

        if (success is not 0 and not 1)
            throw new InvalidDataException("Command response contains invalid Success value.");

        ErrorCode errorCode = (ErrorCode)buffer[offset++];

        int errorMessageLength = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, errorMessageLengthSize));

        offset += errorMessageLengthSize;

        if (errorMessageLength < 0)
            throw new InvalidDataException("Command response error message length cannot be negative.");

        if (buffer.Length < offset + errorMessageLength)
            throw new InvalidDataException("Command response does not contain complete error message.");

        string? errorMessage = errorMessageLength is 0
            ? null
            : Encoding.UTF8.GetString(buffer.Slice(offset, errorMessageLength));

        return success is 1 ? CommandResponse.Success() : CommandResponse.Fail(errorCode, errorMessage ?? "Command failed.");
    }
}