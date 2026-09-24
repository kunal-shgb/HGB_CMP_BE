using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Infrastructure.IAM;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace ComplaintManagement.Api.Authentication;

public static class AuthenticationSetup
{
    public static IServiceCollection AddCmpAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var devAuthEnabled = configuration.GetValue<bool>($"{DevIdentityOptions.SectionName}:Enabled");
        if (devAuthEnabled && !environment.IsDevelopment())
            throw new InvalidOperationException("DevAuth is enabled outside Development. Refusing to start.");

        if (devAuthEnabled)
        {
            services.AddAuthentication(DevAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, _ => { });
        }
        else
        {
            // Bank IAM token validation is registered here once the IAM protocol is confirmed
            // (OIDC/JWT bearer, SAML or a Bank-specific API). Until then every staff call is rejected.
            services.AddAuthentication("Unconfigured")
                .AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>("Unconfigured", _ => { });
        }

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IClaimsTransformation, RoleMappingClaimsTransformation>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

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

/// <summary>Rejects every request until real IAM validation is wired in.</summary>
internal sealed class UnconfiguredAuthenticationHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
