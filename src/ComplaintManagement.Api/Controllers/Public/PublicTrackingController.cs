using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api.Controllers.Public;

/// <summary>
/// Customer complaint tracking: complaint number + registered mobile, then a one-time code sent to that mobile.
/// Anonymous and rate limited. Returns only customer-safe information.
/// </summary>
[ApiController]
[Route("api/v1/public/tracking")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Public)]
public sealed class PublicTrackingController(ITrackingService tracking) : ControllerBase
{
    [HttpPost("otp")]
    public Task<TrackingOtpResponse> RequestOtp(TrackingOtpRequest request, CancellationToken ct) =>
        tracking.RequestOtpAsync(request, ClientIp, ct);

    [HttpPost("verify")]
    public Task<TrackingView> Verify(TrackingVerifyRequest request, CancellationToken ct) =>
        tracking.VerifyAsync(request, ClientIp, ct);

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
