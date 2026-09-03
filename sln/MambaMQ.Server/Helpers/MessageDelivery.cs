namespace MambaMQ.Server.Helpers;

public sealed record MessageDelivery(MambaMessage Message, DeliveryId DeliveryId);