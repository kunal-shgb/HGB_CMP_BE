namespace ComplaintManagement.Domain.Common;

/// <summary>Raised when a request breaks a business rule. Mapped to HTTP 422 by the API.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
