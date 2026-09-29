using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Application.Reference;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>Configuration and master data the portal needs for filters and forms.</summary>
[ApiController]
[Route("api/v1")]
public sealed class ReferenceController(IReferenceService reference) : ControllerBase
{
    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryResponse>> Categories(CancellationToken ct) => reference.CategoriesAsync(ct);

    [HttpGet("statuses")]
    public Task<IReadOnlyList<StatusResponse>> Statuses(CancellationToken ct) => reference.StatusesAsync(ct);

    [HttpGet("priorities")]
    public Task<IReadOnlyList<PriorityResponse>> Priorities(CancellationToken ct) => reference.PrioritiesAsync(ct);

    [HttpGet("regions")]
    public Task<IReadOnlyList<RegionResponse>> Regions(CancellationToken ct) => reference.RegionsAsync(ct);

    [HttpGet("branches")]
    public Task<IReadOnlyList<BranchResponse>> Branches([FromQuery] string? regionCode, CancellationToken ct) =>
        reference.BranchesAsync(regionCode, ct);

    /// <summary>Where complaints can come from: the website, or staff at a branch, RO or HO.</summary>
    [HttpGet("complaint-sources")]
    public IReadOnlyList<OrgRef> Sources() => ComplaintSources.All.Select(s => new OrgRef(s, ComplaintSources.Name(s))).ToList();

    [HttpGet("departments")]
    public Task<IReadOnlyList<DepartmentResponse>> Departments(CancellationToken ct) => reference.DepartmentsAsync(ct);
}
