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
public sealed class ComplaintsController(IComplaintService complaints, IAttachmentService attachments) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<ComplaintListItem>> List([FromQuery] ComplaintFilterRequest filter, CancellationToken ct) =>
        complaints.ListAsync(filter, ct);

    [HttpGet("{id:guid}")]
    public Task<ComplaintDetail> Get(Guid id, CancellationToken ct) => complaints.GetAsync(id, ct);

    [HttpGet("{id:guid}/history")]
    public Task<IReadOnlyList<TimelineEvent>> History(Guid id, CancellationToken ct) => complaints.GetHistoryAsync(id, ct);

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Permissions.ComplaintChangeStatus)]
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
    [Authorize(Policy = Permissions.ComplaintAddRemark)]
    public Task<RemarkItem> AddRemark(Guid id, AddRemarkRequest request, CancellationToken ct) =>
        complaints.AddRemarkAsync(id, request, ct);

    /// <summary>Messages sent or queued to the customer about this complaint (recipient masked).</summary>
    [HttpGet("{id:guid}/notifications")]
    public Task<IReadOnlyList<NotificationItem>> Notifications(Guid id, CancellationToken ct) => complaints.GetNotificationsAsync(id, ct);

    [HttpPost("{id:guid}/escalate")]
    [Authorize(Policy = Permissions.ComplaintEscalate)]
    public async Task<IActionResult> Escalate(Guid id, EscalateRequest request, CancellationToken ct)
    {
        await complaints.EscalateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Policy = Permissions.ComplaintAddAttachment)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormFiles.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFiles.MaxRequestBytes)]
    public Task<IReadOnlyList<AttachmentItem>> UploadAttachments(Guid id, [FromForm(Name = "files")] IFormFileCollection files, CancellationToken ct) =>
        attachments.AddAsync(id, files.ToUploads(), ct);

    /// <summary>Downloads an attachment. Always served as a download, never rendered inline.</summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        var file = await attachments.OpenAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
