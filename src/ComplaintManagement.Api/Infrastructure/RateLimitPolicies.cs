using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    /// <summary>Complaint registration and tracking calls per client IP per minute.</summary>
    public int PublicPerMinute { get; set; } = 10;
    /// <summary>Public read calls (form options) per client IP per minute.</summary>
    public int PublicReadPerMinute { get; set; } = 60;
    /// <summary>Sign-in attempts per client IP per minute. Account lockout itself belongs to the Bank IAM.</summary>
    public int LoginPerMinute { get; set; } = 10;
}

public static class RateLimitPolicies
{
    public const string Public = "public";
    public const string PublicRead = "public-read";
    public const string Login = "login";

    public static IServiceCollection AddCmpRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Per client IP. Behind Nginx, forwarded headers must be configured so this is the real client.
            options.AddPolicy(Public, context => PerIp(context, limits.PublicPerMinute));
            options.AddPolicy(PublicRead, context => PerIp(context, limits.PublicReadPerMinute));
            options.AddPolicy(Login, context => PerIp(context, limits.LoginPerMinute));
        });
        return services;
    }

    private static RateLimitPartition<string> PerIp(HttpContext context, int perMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
