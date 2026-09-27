namespace ComplaintManagement.Contracts.Responses;

/// <summary>A signed, short-lived portal token. The caller keeps it server-side and sends it as a Bearer token.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
