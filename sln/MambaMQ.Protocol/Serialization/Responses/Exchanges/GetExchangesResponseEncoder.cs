namespace MambaMQ.Protocol.Serialization.Responses.Exchanges;

public static class GetExchangesResponseEncoder
{
    public static byte[] Encode(GetExchangesResponse response)
    {
        List<byte[]> encodedExchanges = response.Exchanges
            .Select(ExchangeModelEncoder.Encode)
            .ToList();

        int size = sizeof(int) + encodedExchanges.Sum(exchange => exchange.Length);
        byte[] buffer = new byte[size];
        Span<byte> span = buffer;

        BinaryPrimitives.WriteInt32BigEndian(span, encodedExchanges.Count);
        int offset = sizeof(int);

        foreach (byte[] exchange in encodedExchanges)
        {
            exchange.CopyTo(span[offset..]);
            offset += exchange.Length;
        }

        return buffer;
    }
}