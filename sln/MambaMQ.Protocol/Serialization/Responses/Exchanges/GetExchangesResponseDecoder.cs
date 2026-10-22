namespace MambaMQ.Protocol.Serialization.Responses.Exchanges;

public static class GetExchangesResponseDecoder
{
    public static GetExchangesResponse Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < sizeof(int))
            throw new InvalidDataException("Get exchanges response does not contain an exchange count.");

        int count = BinaryPrimitives.ReadInt32BigEndian(buffer);
        if (count < 0)
            throw new InvalidDataException("Get exchanges response contains an invalid exchange count.");

        int offset = sizeof(int);
        List<ExchangeModel> exchanges = new(count);

        for (int i = 0; i < count; i++)
            exchanges.Add(ExchangeModelDecoder.Decode(buffer, ref offset));

        if (offset != buffer.Length)
            throw new InvalidDataException("Get exchanges response contains trailing data.");

        return new GetExchangesResponse { Exchanges = exchanges };
    }
}