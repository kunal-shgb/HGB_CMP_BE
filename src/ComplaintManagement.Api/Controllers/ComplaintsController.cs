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
public sealed class ComplaintsController(IComplaintService complaints) : ControllerBase
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
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        await complaints.ChangeStatusAsync(id, request, ct);
        return NoContent();
    }

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
}
