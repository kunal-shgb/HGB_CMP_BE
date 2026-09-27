using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Application.Auth;

public interface IAuthService
{
    /// <summary>
    /// Verifies credentials with the Bank IAM. Returns the active employee's profile, or null for a
    /// wrong employee code or password or an inactive employee (callers must not tell these apart).
    /// </summary>
    Task<IamUser?> SignInAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken ct);
}

public sealed class AuthService(
    IIamAuthenticator iam,
    IApplicationDbContext db,
    TimeProvider clock,
    IValidator<LoginRequest> validator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<IamUser?> SignInAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        if (!(await validator.ValidateAsync(request, ct)).IsValid) return null;
        var code = request.EmployeeCode.Trim();

        var profile = await iam.AuthenticateAsync(code, request.Password, ct);
        var outcome = profile is null ? "LOGIN_FAILED" : profile.IsActive ? "LOGIN_SUCCESS" : "LOGIN_INACTIVE";

        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = code,
            Action = outcome,
            Module = "Auth",
            IpAddress = ipAddress,
            UserAgent = userAgent is { Length: > 512 } ? userAgent[..512] : userAgent,
            CreatedAt = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);

        if (outcome != "LOGIN_SUCCESS")
        {
            logger.LogWarning("Sign-in rejected ({Outcome}) for employee {EmployeeCode}", outcome, code);
            return null;
        }
        return profile;
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9]+$");
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
