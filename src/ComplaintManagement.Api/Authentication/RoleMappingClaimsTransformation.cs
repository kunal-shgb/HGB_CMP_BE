using System.Security.Claims;
using ComplaintManagement.Application.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace ComplaintManagement.Api.Authentication;

/// <summary>Adds application-role claims by mapping the IAM access role (and office type) through application_role_mapping.</summary>
public sealed class RoleMappingClaimsTransformation(IRoleMappingService mappings, IMemoryCache cache) : IClaimsTransformation
{
    private const string CacheKey = "cmp:role-mappings";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity) return principal;
        if (identity.HasClaim(c => c.Type == CmpClaimTypes.AppRole)) return principal;

        var active = await cache.GetOrCreateAsync(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return mappings.GetActiveMappingsAsync(CancellationToken.None);
        }) ?? [];

        var roles = RoleMappingService.Resolve(
            active,
            identity.FindAll(CmpClaimTypes.IamRole).Select(c => c.Value),
            identity.FindFirst(CmpClaimTypes.OfficeType)?.Value);
        foreach (var role in roles) identity.AddClaim(new Claim(CmpClaimTypes.AppRole, role));

        return principal;
    }
}
