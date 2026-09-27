using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>Checker queue: Makers' decisions waiting for this Checker's office.</summary>
[ApiController]
[Route("api/v1/approvals")]
[Authorize(Policy = Permissions.ComplaintApprove)]
public sealed class ApprovalsController(IComplaintService complaints) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ApprovalListItem>> Pending(CancellationToken ct) => complaints.ListPendingApprovalsAsync(ct);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, DecideApprovalRequest request, CancellationToken ct)
    {
        await complaints.ApproveAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, DecideApprovalRequest request, CancellationToken ct)
    {
        await complaints.ReturnAsync(id, request, ct);
        return NoContent();
    }
}
