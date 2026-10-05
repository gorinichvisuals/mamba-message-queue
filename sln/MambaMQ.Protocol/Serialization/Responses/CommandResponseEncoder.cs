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
    
    public static byte[] Encode<T>(CommandResponse<T> response, Func<T, byte[]> dataEncoder)
    {
        byte[] errorMessage = response.ErrorMessage is null
            ? []
            : Encoding.UTF8.GetBytes(response.ErrorMessage);

        byte[] data = response.IsSucceed && response.Data is not null
            ? dataEncoder(response.Data)
            : [];

        const int successSize = sizeof(byte);
        const int errorCodeSize = sizeof(byte);
        const int errorMessageLengthSize = sizeof(int);
        const int dataLengthSize = sizeof(int);

        int offset = 0;

        byte[] buffer = new byte[
            successSize +
            errorCodeSize +
            errorMessageLengthSize +
            errorMessage.Length +
            dataLengthSize +
            data.Length];

        Span<byte> span = buffer;

        span[offset++] = response.IsSucceed
            ? (byte)1
            : (byte)0;

        span[offset++] = (byte)response.ErrorCode;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], errorMessage.Length);

        offset += errorMessageLengthSize;

        errorMessage.CopyTo(span[offset..]);

        offset += errorMessage.Length;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], data.Length);

        offset += dataLengthSize;

        data.CopyTo(span[offset..]);

        return buffer;
    }
}