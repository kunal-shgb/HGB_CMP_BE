using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ComplaintManagement.Api.Authentication;

public static class AuthenticationSetup
{
    public static IServiceCollection AddCmpAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PortalTokenOptions>()
            .Bind(configuration.GetSection(PortalTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<PortalTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<PortalTokenOptions>>((jwt, tokenOptions) =>
            {
                var o = tokenOptions.Value;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = o.Issuer,
                    ValidAudience = o.Audience,
                    IssuerSigningKey = PortalTokenIssuer.SigningKey(o),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = CmpClaimTypes.Name,
                    RoleClaimType = CmpClaimTypes.AppRole,
                };
            });

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IClaimsTransformation, RoleMappingClaimsTransformation>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddOptions<OfficeScopeOptions>().Bind(configuration.GetSection(OfficeScopeOptions.SectionName));

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(ctx => Permissions
                        .ForRoles(ctx.User.FindAll(CmpClaimTypes.AppRole).Select(c => c.Value))
                        .Contains(permission)));
            }
        });

        return services;
    }
}
