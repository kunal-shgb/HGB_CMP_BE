using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
// ScopeLevel property name shadows the enum inside FakeUser.
using ScopeLevel = ComplaintManagement.Domain.Enums.ScopeLevel;

namespace ComplaintManagement.UnitTests.Support;

internal sealed class FakeUser(string employeeId, params string[] roles) : ICurrentUser
{
    private static readonly OfficeScopeOptions Scopes = new();

    public string EmployeeId { get; } = employeeId;
    public string Name => EmployeeId;
    public string? Designation => null;
    public IReadOnlySet<string> Roles { get; } = roles.ToHashSet();
    public ScopeLevel? ScopeLevel => Scopes.Resolve(OfficeType);
    public string? OfficeType { get; init; } = "Head Office";
    public string? OfficeCode { get; init; } = "0000";
    public string? OfficeName => OfficeCode;
    public string? DepartmentName { get; init; }
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "tests";
    public bool HasPermission(string permission) => Permissions.ForRoles(Roles).Contains(permission);

    public static FakeUser AtBranch(string id, string branch, params string[] roles) => new(id, roles) { OfficeType = "Branch", OfficeCode = branch };
    public static FakeUser AtRegion(string id, string region, params string[] roles) => new(id, roles) { OfficeType = "Regional Office", OfficeCode = region };
}

internal sealed class FakeAudit : IAuditLogger
{
    public List<(string Action, string Module, string? RecordId)> Entries { get; } = [];
    public void Log(string action, string module, string? recordId, string? details = null) => Entries.Add((action, module, recordId));
}

/// <summary>The seeded role mappings: OfficeHead at branches, Maker/Checker at RO and HO.</summary>
internal sealed class FakeRoleMappings : IRoleMappingService
{
    public static readonly RoleMapping[] Seeded =
    [
        new("OfficeHead", "Branch", AppRoles.OfficeHead),
        new("Maker", "Regional Office", AppRoles.Maker), new("Maker", "Head Office", AppRoles.Maker),
        new("Checker", "Regional Office", AppRoles.Checker), new("Checker", "Head Office", AppRoles.Checker),
    ];

    public Task<IReadOnlyList<RoleMapping>> GetActiveMappingsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<RoleMapping>>(Seeded);

    public static AssignmentPolicy Policy(IIamUserService iam) => new(iam, new FakeRoleMappings());
}

internal static class Staff
{
    public static IamUser Head(string code, string branch, bool active = true) =>
        new(code, $"Head {code}", null, "OfficeHead", "Branch", branch, branch, null, active);

    public static IamUser At(string code, string officeType, string officeCode, bool active = true) =>
        new(code, $"Officer {code}", null, "Maker", officeType, officeCode, officeCode, null, active);
}

internal sealed class FakeIam(params IamUser[] users) : IIamUserService
{
    public Task<IamUser?> GetUserAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.FirstOrDefault(u => u.EmployeeCode == employeeId));
    public Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IamUser>>(users);
    public Task<IReadOnlyList<IamUser>> GetUsersInOfficeAsync(string officeCode, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IamUser>>(users.Where(u => u.OfficeCode == officeCode).ToList());
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Two regions with two branches each, a department and a minimal workflow.</summary>
/// <summary>IAM organisation data for tests.</summary>
internal sealed class FakeOrg(IReadOnlyList<IamBranch> branches, IReadOnlyList<IamDepartment> departments) : IIamOrganisationService
{
    public Task<IReadOnlyList<IamRegion>> GetRegionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IamRegion>>(branches.Select(b => new IamRegion(b.RegionCode, b.RegionName, true)).DistinctBy(r => r.Code).ToList());
    public Task<IReadOnlyList<IamBranch>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult(branches);
    public Task<IReadOnlyList<IamDepartment>> GetDepartmentsAsync(CancellationToken cancellationToken = default) => Task.FromResult(departments);
}

internal sealed class TestData
{
    // Organisation data comes from the IAM: two Regional Offices (RA, RB) and one HO department.
    public IamBranch BranchA1 { get; } = new("A1", "Branch A1", "RA", "Region A", true);
    public IamBranch BranchA2 { get; } = new("A2", "Branch A2", "RA", "Region A", true);
    public IamBranch BranchB1 { get; } = new("B1", "Branch B1", "RB", "Region B", true);
    public IamDepartment Dept { get; } = new("DBD", "Digital Banking Division", true);
    public FakeOrg Org { get; }
    public required ComplaintCategory Category { get; init; }

    private TestData() => Org = new FakeOrg([BranchA1, BranchA2, BranchB1], [Dept]);

    public static TestData Seed(TestDbContext db)
    {
        var group = new ComplaintCategoryGroup { Code = "DIGITAL_BANKING", Name = "Digital Banking", SortOrder = 10 };
        var cat = new ComplaintCategory { Code = "UPI", Name = "UPI", GroupId = group.Id, Group = group, TatDays = 7 };

        db.AddRange(group, cat);
        db.Statuses.AddRange(
            new ComplaintStatus { Code = "NEW", Name = "New", CustomerLabel = "Registered", IsInitial = true, SortOrder = 1 },
            new ComplaintStatus { Code = "ASSIGNED", Name = "Assigned", CustomerLabel = "Under review", IsAssignment = true, SortOrder = 2 },
            new ComplaintStatus { Code = "UNDER_PROCESS", Name = "Under process", CustomerLabel = "Under process", SortOrder = 3 },
            new ComplaintStatus { Code = "RESOLVED", Name = "Resolved", CustomerLabel = "Resolved", IsResolution = true, SortOrder = 4 },
            new ComplaintStatus { Code = "CLOSED", Name = "Closed", CustomerLabel = "Closed", IsTerminal = true, SortOrder = 5 },
            new ComplaintStatus { Code = "REOPENED", Name = "Reopened", CustomerLabel = "Reopened", SortOrder = 6 },
            new ComplaintStatus { Code = "PENDING_APPROVAL", Name = "Pending checker approval", CustomerLabel = "Under process", IsApprovalPending = true, SortOrder = 7 });
        db.StatusTransitions.AddRange(
            new ComplaintStatusTransition { Id = 1, FromStatusCode = "NEW", ToStatusCode = "ASSIGNED" },
            new ComplaintStatusTransition { Id = 2, FromStatusCode = "ASSIGNED", ToStatusCode = "UNDER_PROCESS" },
            new ComplaintStatusTransition { Id = 3, FromStatusCode = "UNDER_PROCESS", ToStatusCode = "RESOLVED", RequiresRemark = true, RequiresApproval = true },
            new ComplaintStatusTransition { Id = 4, FromStatusCode = "RESOLVED", ToStatusCode = "CLOSED" },
            new ComplaintStatusTransition { Id = 5, FromStatusCode = "CLOSED", ToStatusCode = "REOPENED", RequiresRemark = true },
            new ComplaintStatusTransition { Id = 6, FromStatusCode = "RESOLVED", ToStatusCode = "REOPENED", IsActive = false });
        db.Priorities.Add(new ComplaintPriority { Code = "MEDIUM", Name = "Medium", Rank = 2, IsDefault = true });
        db.SaveChanges();

        return new TestData { Category = cat };
    }

    public Complaint AddComplaint(TestDbContext db, IamBranch branch, string status = "NEW", IamDepartment? dept = null, string? assignedTo = null)
    {
        var c = new Complaint
        {
            ComplaintNumber = $"HGB-2026-{db.Complaints.Count() + 1:D8}",
            CustomerName = "Test", MobileNumber = "9876543210", AccountNumber = "123456789012",
            BranchCode = branch.Code, BranchName = branch.Name, RegionCode = branch.RegionCode, RegionName = branch.RegionName,
            CategoryId = Category.Id,
            PriorityCode = "MEDIUM", StatusCode = status, Title = "Test complaint", Description = "Test complaint",
            AssignedDepartmentCode = dept?.Code, AssignedDepartmentName = dept?.Name, AssignedEmployeeId = assignedTo,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        };
        db.Complaints.Add(c);
        db.SaveChanges();
        return c;
    }
}

internal sealed class SequentialNumbers : IComplaintNumberGenerator
{
    private int _next;
    public Task<string> NextAsync(int year, CancellationToken cancellationToken = default) =>
        Task.FromResult($"HGB-{year}-{++_next:D8}");

    /// <summary>A registrar for tests that lodge complaints without documents.</summary>
    public static ComplaintManagement.Application.Complaints.ComplaintRegistrar Registrar(TestDbContext db, IIamOrganisationService org)
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        return new(db, new SequentialNumbers(), org, clock, null!, new ComplaintManagement.Application.Notifications.CustomerNotifier(db, clock));
    }
}
