namespace MambaMQ.Protocol.Serialization.Responses;

public static class CommandResponseEncoder
{
    public static byte[] Encode(CommandResponse response)
    {
        byte[] errorMessage = response.ErrorMessage is null
            ? []
            : Encoding.UTF8.GetBytes(response.ErrorMessage);

        const int successSize = sizeof(byte);
        const int errorCodeSize = sizeof(byte);
        const int errorMessageLengthSize = sizeof(int);

        int offset = 0;

        byte[] buffer = new byte[
            successSize +
            errorCodeSize +
            errorMessageLengthSize +
            errorMessage.Length];

        Span<byte> span = buffer;

        span[offset++] = response.IsSucceed
            ? (byte)1
            : (byte)0;

        span[offset++] = (byte)response.ErrorCode;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], errorMessage.Length);

        offset += errorMessageLengthSize;

        errorMessage.CopyTo(span[offset..]);

        return buffer;
    }
}