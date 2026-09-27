namespace MambaMQ.Protocol.Responses;

public sealed record AuthenticationResponse(bool Success, string? ErrorMessage = null);