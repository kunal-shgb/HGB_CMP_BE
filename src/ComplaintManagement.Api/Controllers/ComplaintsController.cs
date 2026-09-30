using System.Text.Json;
using ComplaintManagement.Api.Infrastructure;
using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize(Policy = Permissions.ComplaintView)]
// Status, remarks, attachments and escalation are checked per complaint in the Application layer
// (role, or the complaint being assigned to the caller). Assigning stays a role-only action.
public sealed class ComplaintsController(IComplaintService complaints, IAttachmentService attachments, IComplaintIntakeService intake) : ControllerBase
{
    /// <summary>Categories and branches for lodging a complaint on a customer's behalf.</summary>
    [HttpGet("lodge-options")]
    [Authorize(Policy = Permissions.ComplaintCreate)]
    public Task<LodgeFormOptions> LodgeOptions(CancellationToken ct) => intake.GetFormOptionsAsync(ct);

    /// <summary>Lodges a complaint on a customer's behalf (JSON, no documents).</summary>
    [HttpPost]
    [Consumes("application/json")]
    [Authorize(Policy = Permissions.ComplaintCreate)]
    public async Task<ActionResult<LodgeComplaintResponse>> Lodge(StaffCreateComplaintRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await intake.LodgeAsync(request, [], ct));

    /// <summary>
    /// Lodges a complaint with supporting documents (multipart/form-data): a "complaint" field holding the
    /// same JSON as the plain endpoint, plus up to five "files".
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = Permissions.ComplaintCreate)]
    [RequestSizeLimit(FormFiles.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFiles.MaxRequestBytes)]
    public async Task<ActionResult<LodgeComplaintResponse>> LodgeWithDocuments(
        [FromForm(Name = "complaint")] string complaintJson,
        [FromForm(Name = "files")] IFormFileCollection? files,
        CancellationToken ct)
    {
        StaffCreateComplaintRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<StaffCreateComplaintRequest>(complaintJson, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            request = null;
        }
        if (request is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The complaint details could not be read.");
        return StatusCode(StatusCodes.Status201Created, await intake.LodgeAsync(request, files.ToUploads(), ct));
    }

    [HttpGet]
    public Task<PagedResponse<ComplaintListItem>> List([FromQuery] ComplaintFilterRequest filter, CancellationToken ct) =>
        complaints.ListAsync(filter, ct);

    [HttpGet("{id:guid}")]
    public Task<ComplaintDetail> Get(Guid id, CancellationToken ct) => complaints.GetAsync(id, ct);

    [HttpGet("{id:guid}/history")]
    public Task<IReadOnlyList<TimelineEvent>> History(Guid id, CancellationToken ct) => complaints.GetHistoryAsync(id, ct);

    [HttpPost("{id:guid}/status")]
    public Task<ChangeStatusResponse> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct) =>
        complaints.ChangeStatusAsync(id, request, ct);

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Permissions.ComplaintAssign)]
    public async Task<IActionResult> Assign(Guid id, AssignComplaintRequest request, CancellationToken ct)
    {
        await complaints.AssignAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/remarks")]
    [Consumes("application/json")]
    public Task<RemarkItem> AddRemark(Guid id, AddRemarkRequest request, CancellationToken ct) =>
        complaints.AddRemarkAsync(id, request, ct);

    /// <summary>
    /// Adds a remark with files (multipart/form-data): a "remark" field holding the same JSON as the plain
    /// endpoint, plus up to five "files" (PDF, JPG, PNG, XLS, XLSX; 5 MB each).
    /// </summary>
    [HttpPost("{id:guid}/remarks")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormFiles.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFiles.MaxRequestBytes)]
    public async Task<ActionResult<RemarkItem>> AddRemarkWithFiles(
        Guid id,
        [FromForm(Name = "remark")] string remarkJson,
        [FromForm(Name = "files")] IFormFileCollection? files,
        CancellationToken ct)
    {
        AddRemarkRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<AddRemarkRequest>(remarkJson, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            request = null;
        }
        if (request is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The remark could not be read.");
        return await complaints.AddRemarkAsync(id, request, files.ToUploads(), ct);
    }

    /// <summary>Messages sent or queued to the customer about this complaint (recipient masked).</summary>
    [HttpGet("{id:guid}/notifications")]
    public Task<IReadOnlyList<NotificationItem>> Notifications(Guid id, CancellationToken ct) => complaints.GetNotificationsAsync(id, ct);

    /// <summary>Marks the customer's "not resolved" feedback as reviewed, with an optional note.</summary>
    [HttpPost("{id:guid}/feedback/review")]
    public async Task<IActionResult> ReviewFeedback(Guid id, ReviewFeedbackRequest request, CancellationToken ct)
    {
        await complaints.ReviewFeedbackAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/escalate")]
    public async Task<IActionResult> Escalate(Guid id, EscalateRequest request, CancellationToken ct)
    {
        await complaints.EscalateAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Downloads an attachment. Always served as a download, never rendered inline.</summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var file = await attachments.OpenAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
