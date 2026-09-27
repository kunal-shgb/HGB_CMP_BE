using System.Text.Json;
using ComplaintManagement.Api.Infrastructure;
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
public sealed class PublicComplaintsController(IPublicComplaintService complaints) : ControllerBase
{
    /// <summary>Branches and categories for the complaint form.</summary>
    [HttpGet("form-options")]
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    public Task<PublicFormOptions> FormOptions(CancellationToken ct) => complaints.GetFormOptionsAsync(ct);

    /// <summary>Registers a complaint without documents (JSON).</summary>
    [HttpPost]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    public async Task<ActionResult<CreateComplaintResponse>> Register(PublicCreateComplaintRequest request, CancellationToken ct)
    {
        var result = await complaints.RegisterAsync(request, [], ClientIp, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Registers a complaint with supporting documents (multipart/form-data): a "complaint" field holding the
    /// same JSON as the plain endpoint, plus up to five "files" (PDF, JPG, PNG, XLS, XLSX; 5 MB each).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(RateLimitPolicies.Public)]
    [RequestSizeLimit(FormFiles.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFiles.MaxRequestBytes)]
    public async Task<ActionResult<CreateComplaintResponse>> RegisterWithDocuments(
        [FromForm(Name = "complaint")] string complaintJson,
        [FromForm(Name = "files")] IFormFileCollection? files,
        CancellationToken ct)
    {
        PublicCreateComplaintRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<PublicCreateComplaintRequest>(complaintJson, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            request = null;
        }
        if (request is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The complaint details could not be read.");

        var result = await complaints.RegisterAsync(request, files.ToUploads(), ClientIp, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
