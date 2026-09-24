using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api.Controllers.Public;

/// <summary>
/// Anonymous endpoints for the Bank website. Kept apart from staff routes and rate limited.
/// The /api/v1/public prefix is provisional until the Bank confirms how the website reaches the API.
/// </summary>
[ApiController]
[Route("api/v1/public/complaints")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Public)]
public sealed class PublicComplaintsController(IPublicComplaintService complaints) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateComplaintResponse>> Register(PublicCreateComplaintRequest request, CancellationToken ct)
    {
        var result = await complaints.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
