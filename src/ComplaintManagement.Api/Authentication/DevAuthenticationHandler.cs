using System.Security.Claims;
using System.Text.Encodings.Web;
using ComplaintManagement.Infrastructure.IAM;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Api.Authentication;

/// <summary>
/// DEVELOPMENT ONLY. Accepts "Authorization: Bearer dev.{EmployeeId}" for an identity listed under
/// DevAuth:Users and issues its claims. Stands in for Bank IAM token validation until the IAM spec is
/// available. Registration refuses to happen outside the Development environment.
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<DevIdentityOptions> identities) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevToken";
    private const string TokenPrefix = "Bearer dev.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(TokenPrefix, StringComparison.Ordinal)) return Task.FromResult(AuthenticateResult.NoResult());

        var employeeId = header[TokenPrefix.Length..].Trim();
        var user = identities.Value.Users.FirstOrDefault(u => string.Equals(u.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
        if (user is null) return Task.FromResult(AuthenticateResult.Fail("Unknown development identity."));

        var claims = new List<Claim>
        {
            new(CmpClaimTypes.EmployeeId, user.EmployeeId),
            new(CmpClaimTypes.Name, user.Name),
        };
        if (user.Designation is not null) claims.Add(new(CmpClaimTypes.Designation, user.Designation));
        if (user.RegionCode is not null) claims.Add(new(CmpClaimTypes.Region, user.RegionCode));
        if (user.BranchCode is not null) claims.Add(new(CmpClaimTypes.Branch, user.BranchCode));
        if (user.DepartmentCode is not null) claims.Add(new(CmpClaimTypes.Department, user.DepartmentCode));
        claims.AddRange(user.IamRoles.Select(r => new Claim(CmpClaimTypes.IamRole, r)));

        var identity = new ClaimsIdentity(claims, SchemeName, CmpClaimTypes.Name, CmpClaimTypes.AppRole);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
