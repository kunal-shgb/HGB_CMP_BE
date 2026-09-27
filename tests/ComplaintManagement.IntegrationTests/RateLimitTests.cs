using System.Net;
using System.Net.Http.Json;
using ComplaintManagement.Contracts.Requests;

namespace ComplaintManagement.IntegrationTests;

/// <summary>A host with a tiny public limit, to prove the limiter actually rejects.</summary>
public sealed class StrictLimitFactory : ApiFactory
{
    public override int PublicLimit => 2;
}

public class RateLimitTests(StrictLimitFactory factory) : IClassFixture<StrictLimitFactory>
{
    [Fact]
    public async Task Public_calls_beyond_the_limit_get_429()
    {
        var client = factory.CreateClient();
        var request = new TrackingOtpRequest("HGB-2026-99999999", "9876543210");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/public/tracking/otp", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/public/tracking/otp", request)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/public/tracking/otp", request)).StatusCode);
    }
}
