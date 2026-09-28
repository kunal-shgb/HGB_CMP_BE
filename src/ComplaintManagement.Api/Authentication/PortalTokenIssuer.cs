using System.Security.Claims;
using System.Text;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ComplaintManagement.Api.Authentication;

/// <summary>
/// Issues the portal's signed token from a Bank IAM profile. It carries identity and office only;
/// application roles are resolved from application_role_mapping on every request, so mapping changes
/// apply without re-login. No password or mobile number is ever included.
/// </summary>
public sealed class PortalTokenIssuer(IOptions<PortalTokenOptions> options, TimeProvider clock)
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(IamUser user)
    {
        var o = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(o.TokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.EmployeeCode),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(CmpClaimTypes.EmployeeId, user.EmployeeCode),
            new(CmpClaimTypes.Name, user.FullName),
        };
        void Add(string type, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) claims.Add(new Claim(type, value.Trim()));
        }
        Add(CmpClaimTypes.Designation, user.Designation);
        Add(CmpClaimTypes.IamRole, user.AccessRole);
        Add(CmpClaimTypes.OfficeType, user.OfficeType);
        Add(CmpClaimTypes.OfficeCode, user.OfficeCode);
        Add(CmpClaimTypes.OfficeName, user.OfficeName);
        Add(CmpClaimTypes.DepartmentName, user.DepartmentName);
        if (user.IsSystemAdmin) claims.Add(new Claim(CmpClaimTypes.SystemAdmin, "true"));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(o), SecurityAlgorithms.HmacSha256),
        });
        return (token, expires);
    }

    public static SymmetricSecurityKey SigningKey(PortalTokenOptions o) => new(Encoding.UTF8.GetBytes(o.SigningKey));
}
