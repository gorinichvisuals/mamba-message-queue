namespace MambaMQ.Core.Models;

public sealed record MessageDelivery(
    MambaMessage Message, 
    DeliveryId DeliveryId);