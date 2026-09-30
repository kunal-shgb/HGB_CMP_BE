using ComplaintManagement.Application.Feedback;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api.Controllers.Public;

/// <summary>
/// Customer feedback on a closed complaint through the one-time link in the closure message (or from tracking).
/// Anonymous and rate limited; the token is the only credential and returns no personal data.
/// </summary>
[ApiController]
[Route("api/v1/public/feedback/{token}")]
[AllowAnonymous]
public sealed class PublicFeedbackController(IFeedbackService feedback) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    public Task<PublicFeedbackView> Get(string token, CancellationToken ct) => feedback.GetAsync(token, ct);

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    public Task<SubmitFeedbackResponse> Submit(string token, SubmitFeedbackRequest request, CancellationToken ct) =>
        feedback.SubmitAsync(token, request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
}
