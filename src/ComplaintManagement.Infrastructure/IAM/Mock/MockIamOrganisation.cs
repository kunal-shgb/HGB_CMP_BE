using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>TEMPORARY stand-in for the Bank IAM's Regional Office list.</summary>
public sealed class MockIamRegion
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>TEMPORARY stand-in for the Bank IAM's branch list.</summary>
public sealed class MockIamBranch
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string RegionCode { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>TEMPORARY stand-in for the Bank IAM's Head Office department list.</summary>
public sealed class MockIamDepartment
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Organisation data backed by the mock IAM tables.</summary>
internal sealed class MockIamOrganisationService(MockIamDbContext db) : IIamOrganisationService
{
    public async Task<IReadOnlyList<IamRegion>> GetRegionsAsync(CancellationToken cancellationToken = default) =>
        await db.Regions.AsNoTracking().OrderBy(r => r.Name)
            .Select(r => new IamRegion(r.Code, r.Name, r.IsActive)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IamBranch>> GetBranchesAsync(CancellationToken cancellationToken = default) =>
        await (from b in db.Branches.AsNoTracking()
               join r in db.Regions.AsNoTracking() on b.RegionCode equals r.Code
               orderby b.Name
               select new IamBranch(b.Code, b.Name, r.Code, r.Name, b.IsActive && r.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IamDepartment>> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await db.Departments.AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new IamDepartment(d.Code, d.Name, d.IsActive)).ToListAsync(cancellationToken);
}
