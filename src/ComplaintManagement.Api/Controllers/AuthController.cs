using ComplaintManagement.Api.Authentication;
using ComplaintManagement.Application.Auth;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ComplaintManagement.Api.Controllers;

/// <summary>
/// Staff sign-in. Credentials are relayed to the Bank IAM API and never stored or logged; the portal
/// keeps no password database. On success the API issues its own short-lived signed token.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth, PortalTokenIssuer tokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await auth.SignInAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            ct);

        // One message for every failure so the response does not reveal which employee codes exist.
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid employee code or password.");

        var (token, expiresAt) = tokens.Issue(user);
        return new LoginResponse(token, expiresAt);
    }
}
