namespace MambaMQ.Protocol.Serialization.Authentication;

public static class AuthenticationResponseEncoder
{
    public static byte[] Encode(AuthenticationResponse response)
    {
        byte[] errorMessage = response.ErrorMessage is null
            ? []
            : Encoding.UTF8.GetBytes(response.ErrorMessage);

        const int successSize = sizeof(byte);
        const int errorMessageLengthSize = sizeof(int);

        int offset = successSize;

        byte[] buffer = new byte[successSize + errorMessageLengthSize + errorMessage.Length];

        Span<byte> span = buffer;

        span[0] = response.Success
            ? (byte)1
            : (byte)0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], errorMessage.Length);

        offset += errorMessageLengthSize;

        errorMessage.CopyTo(span[offset..]);

        return buffer;
    }
}