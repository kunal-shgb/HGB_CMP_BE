using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Reference;

public interface IReferenceService
{
    Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<StatusResponse>> StatusesAsync(CancellationToken ct);
    Task<IReadOnlyList<PriorityResponse>> PrioritiesAsync(CancellationToken ct);
    Task<IReadOnlyList<RegionResponse>> RegionsAsync(CancellationToken ct);
    Task<IReadOnlyList<BranchResponse>> BranchesAsync(string? regionCode, CancellationToken ct);
    Task<IReadOnlyList<DepartmentResponse>> DepartmentsAsync(CancellationToken ct);
}

public sealed class ReferenceService(IApplicationDbContext db, IIamOrganisationService org) : IReferenceService
{
    public async Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .UsableOnForms()
            .Select(c => new CategoryResponse(c.Code, c.Name, c.Group!.Name, c.TatDays))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StatusResponse>> StatusesAsync(CancellationToken ct) =>
        await db.Statuses.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
            .Select(s => new StatusResponse(s.Code, s.Name, s.IsInitial, s.IsTerminal, s.SortOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PriorityResponse>> PrioritiesAsync(CancellationToken ct) =>
        await db.Priorities.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Rank)
            .Select(p => new PriorityResponse(p.Code, p.Name, p.Rank))
            .ToListAsync(ct);

    // Organisation data comes from the Bank IAM (cached), not from portal tables.

    public async Task<IReadOnlyList<RegionResponse>> RegionsAsync(CancellationToken ct) =>
        (await org.GetRegionsAsync(ct)).Where(r => r.IsActive).OrderBy(r => r.Name)
            .Select(r => new RegionResponse(r.Code, r.Name)).ToList();

    public async Task<IReadOnlyList<BranchResponse>> BranchesAsync(string? regionCode, CancellationToken ct) =>
        (await org.GetBranchesAsync(ct))
            .Where(b => b.IsActive && (regionCode == null || string.Equals(b.RegionCode, regionCode, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse(b.Code, b.Name, b.RegionCode)).ToList();

    public async Task<IReadOnlyList<DepartmentResponse>> DepartmentsAsync(CancellationToken ct) =>
        (await org.GetDepartmentsAsync(ct)).Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Code, d.Name)).ToList();
}
