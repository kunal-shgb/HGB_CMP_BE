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
    public Task<IReadOnlyList<AdminCategory>> Categories(CancellationToken ct) => admin.GetCategoriesAsync(ct);

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

    [HttpPost("categories/{code}/subcategories")]
    public async Task<IActionResult> CreateSubCategory(string code, CreateSubCategoryRequest request, CancellationToken ct)
    {
        await admin.CreateSubCategoryAsync(code, request, ct);
        return NoContent();
    }

    [HttpPut("categories/{code}/subcategories/{subCode}")]
    public async Task<IActionResult> UpdateSubCategory(string code, string subCode, UpdateSubCategoryRequest request, CancellationToken ct)
    {
        await admin.UpdateSubCategoryAsync(code, subCode, request, ct);
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

    [HttpPut("workflow/escalation")]
    public async Task<IActionResult> UpdateEscalationSettings(UpdateEscalationSettingsRequest request, CancellationToken ct)
    {
        await admin.UpdateEscalationSettingsAsync(request, ct);
        return NoContent();
    }
}
