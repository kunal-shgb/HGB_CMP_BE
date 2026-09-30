using ComplaintManagement.Application.Admin;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>Categories and workflow settings. Admin access role only.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = Permissions.AdminManage)]
public sealed class AdminController(IAdminService admin) : ControllerBase
{
    [HttpGet("categories")]
    public Task<AdminCategoryCatalogue> Categories(CancellationToken ct) => admin.GetCategoriesAsync(ct);

    [HttpPost("category-groups")]
    public async Task<IActionResult> CreateGroup(CreateCategoryGroupRequest request, CancellationToken ct)
    {
        await admin.CreateGroupAsync(request, ct);
        return NoContent();
    }

    [HttpPut("category-groups/{code}")]
    public async Task<IActionResult> UpdateGroup(string code, UpdateCategoryGroupRequest request, CancellationToken ct)
    {
        await admin.UpdateGroupAsync(code, request, ct);
        return NoContent();
    }

    /// <summary>Order of the groups: every group code, first to last.</summary>
    [HttpPut("group-order")]
    public async Task<IActionResult> ReorderGroups(ReorderRequest request, CancellationToken ct)
    {
        await admin.ReorderGroupsAsync(request, ct);
        return NoContent();
    }

    /// <summary>Order of one group's categories: every category code in the group, first to last.</summary>
    [HttpPut("category-groups/{code}/category-order")]
    public async Task<IActionResult> ReorderCategories(string code, ReorderRequest request, CancellationToken ct)
    {
        await admin.ReorderCategoriesAsync(code, request, ct);
        return NoContent();
    }

    [HttpDelete("category-groups/{code}")]
    public async Task<IActionResult> DeleteGroup(string code, CancellationToken ct)
    {
        await admin.DeleteGroupAsync(code, ct);
        return NoContent();
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request, CancellationToken ct)
    {
        await admin.CreateCategoryAsync(request, ct);
        return NoContent();
    }

    [HttpPut("categories/{code}")]
    public async Task<IActionResult> UpdateCategory(string code, UpdateCategoryRequest request, CancellationToken ct)
    {
        await admin.UpdateCategoryAsync(code, request, ct);
        return NoContent();
    }

    [HttpDelete("categories/{code}")]
    public async Task<IActionResult> DeleteCategory(string code, CancellationToken ct)
    {
        await admin.DeleteCategoryAsync(code, ct);
        return NoContent();
    }

    [HttpGet("workflow")]
    public Task<AdminWorkflow> Workflow(CancellationToken ct) => admin.GetWorkflowAsync(ct);

    [HttpPut("workflow/statuses/{code}")]
    public async Task<IActionResult> UpdateStatus(string code, UpdateStatusRequest request, CancellationToken ct)
    {
        await admin.UpdateStatusAsync(code, request, ct);
        return NoContent();
    }

    [HttpPost("workflow/transitions")]
    public async Task<IActionResult> CreateTransition(CreateTransitionRequest request, CancellationToken ct)
    {
        await admin.CreateTransitionAsync(request, ct);
        return NoContent();
    }

    [HttpPut("workflow/transitions/{id:int}")]
    public async Task<IActionResult> UpdateTransition(int id, UpdateTransitionRequest request, CancellationToken ct)
    {
        await admin.UpdateTransitionAsync(id, request, ct);
        return NoContent();
    }

    [HttpPut("workflow/approvals")]
    public async Task<IActionResult> UpdateApprovalSettings(UpdateApprovalSettingsRequest request, CancellationToken ct)
    {
        await admin.UpdateApprovalSettingsAsync(request, ct);
        return NoContent();
    }

    [HttpPut("workflow/feedback")]
    public async Task<IActionResult> UpdateFeedbackSettings(UpdateFeedbackSettingsRequest request, CancellationToken ct)
    {
        await admin.UpdateFeedbackSettingsAsync(request, ct);
        return NoContent();
    }

    [HttpPut("workflow/escalation")]
    public async Task<IActionResult> UpdateEscalationSettings(UpdateEscalationSettingsRequest request, CancellationToken ct)
    {
        await admin.UpdateEscalationSettingsAsync(request, ct);
        return NoContent();
    }
}
