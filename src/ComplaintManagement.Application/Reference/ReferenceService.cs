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

public sealed class ReferenceService(IApplicationDbContext db) : IReferenceService
{
    public async Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryResponse(c.Code, c.Name, c.GroupName,
                c.SubCategories.Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                    .Select(s => new SubCategoryResponse(s.Code, s.Name, s.TatDays)).ToList()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StatusResponse>> StatusesAsync(CancellationToken ct) =>
        await db.Statuses.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
            .Select(s => new StatusResponse(s.Code, s.Name, s.IsInitial, s.IsTerminal, s.SortOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PriorityResponse>> PrioritiesAsync(CancellationToken ct) =>
        await db.Priorities.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Rank)
            .Select(p => new PriorityResponse(p.Code, p.Name, p.Rank))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RegionResponse>> RegionsAsync(CancellationToken ct) =>
        await db.Regions.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name)
            .Select(r => new RegionResponse(r.Code, r.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BranchResponse>> BranchesAsync(string? regionCode, CancellationToken ct) =>
        await db.Branches.AsNoTracking()
            .Where(b => b.IsActive && (regionCode == null || b.Region!.Code == regionCode))
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse(b.Code, b.Name, b.Region!.Code))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DepartmentResponse>> DepartmentsAsync(CancellationToken ct) =>
        await db.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Code, d.Name))
            .ToListAsync(ct);
}
