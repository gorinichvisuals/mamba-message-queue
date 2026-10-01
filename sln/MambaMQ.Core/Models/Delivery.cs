namespace MambaMQ.Core.Models;

public class Delivery(DeliveryId deliveryId, Guid messageId, Guid consumerId)
{
    public DeliveryId DeliveryId { get; } = deliveryId;
    public Guid MessageId { get; } = messageId;
    public Guid ConsumerId { get; } = consumerId;
}