namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>
/// Verifies an employee's credentials against the Bank IAM API and returns their profile.
/// Implemented in Infrastructure/IAM. The password is passed straight through and never stored or logged.
/// </summary>
public interface IIamAuthenticator
{
    /// <summary>The profile on success; null when the employee code or password is wrong.</summary>
    /// <exception cref="IamUnavailableException">The IAM service could not be reached or answered unexpectedly.</exception>
    Task<IamUser?> AuthenticateAsync(string employeeCode, string password, CancellationToken cancellationToken = default);
}

public sealed class IamUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
