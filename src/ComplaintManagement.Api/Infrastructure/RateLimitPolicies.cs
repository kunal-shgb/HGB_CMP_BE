using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api;

public static class RateLimitPolicies
{
    public const string Public = "public";

    public static IServiceCollection AddCmpRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Per client IP. Behind Nginx, forwarded headers must be configured so this is the real client.
            options.AddPolicy(Public, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }
}
