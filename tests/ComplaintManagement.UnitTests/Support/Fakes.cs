using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.UnitTests.Support;

internal sealed class FakeUser(string employeeId, params string[] roles) : ICurrentUser
{
    public string EmployeeId { get; } = employeeId;
    public string Name => EmployeeId;
    public string? Designation => null;
    public IReadOnlySet<string> Roles { get; } = roles.ToHashSet();
    public ScopeLevel ScopeLevel => AppRoles.ResolveScope(Roles);
    public string? RegionCode { get; init; }
    public string? BranchCode { get; init; }
    public string? DepartmentCode { get; init; }
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "tests";
    public bool HasPermission(string permission) => Permissions.ForRoles(Roles).Contains(permission);
}

internal sealed class FakeAudit : IAuditLogger
{
    public List<(string Action, string Module, string? RecordId)> Entries { get; } = [];
    public void Log(string action, string module, string? recordId, string? details = null) => Entries.Add((action, module, recordId));
}

internal sealed class FakeIam(params IamUser[] users) : IIamUserService
{
    public Task<IamUser?> GetUserAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.FirstOrDefault(u => u.EmployeeId == employeeId));
    public Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IamUser>>(users);
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Two regions with two branches each, a department and a minimal workflow.</summary>
internal sealed class TestData
{
    public required Region RegionA { get; init; }
    public required Region RegionB { get; init; }
    public required Branch BranchA1 { get; init; }
    public required Branch BranchA2 { get; init; }
    public required Branch BranchB1 { get; init; }
    public required Department Dept { get; init; }
    public required ComplaintCategory Category { get; init; }
    public required ComplaintSubCategory SubCategory { get; init; }

    public static TestData Seed(TestDbContext db)
    {
        var ra = new Region { Code = "RA", Name = "Region A" };
        var rb = new Region { Code = "RB", Name = "Region B" };
        var a1 = new Branch { Code = "A1", Name = "A1", RegionId = ra.Id, Region = ra };
        var a2 = new Branch { Code = "A2", Name = "A2", RegionId = ra.Id, Region = ra };
        var b1 = new Branch { Code = "B1", Name = "B1", RegionId = rb.Id, Region = rb };
        var dept = new Department { Code = "DBD", Name = "Digital" };
        var cat = new ComplaintCategory { Code = "UPI", Name = "UPI", GroupName = "Digital Banking" };
        var sub = new ComplaintSubCategory { Code = "GENERAL", Name = "General", CategoryId = cat.Id, Category = cat, TatDays = 7 };

        db.AddRange(ra, rb, a1, a2, b1, dept, cat, sub);
        db.Statuses.AddRange(
            new ComplaintStatus { Code = "NEW", Name = "New", CustomerLabel = "Registered", IsInitial = true, SortOrder = 1 },
            new ComplaintStatus { Code = "ASSIGNED", Name = "Assigned", CustomerLabel = "Under review", IsAssignment = true, SortOrder = 2 },
            new ComplaintStatus { Code = "UNDER_PROCESS", Name = "Under process", CustomerLabel = "Under process", SortOrder = 3 },
            new ComplaintStatus { Code = "RESOLVED", Name = "Resolved", CustomerLabel = "Resolved", IsResolution = true, SortOrder = 4 },
            new ComplaintStatus { Code = "CLOSED", Name = "Closed", CustomerLabel = "Closed", IsTerminal = true, SortOrder = 5 },
            new ComplaintStatus { Code = "REOPENED", Name = "Reopened", CustomerLabel = "Reopened", SortOrder = 6 });
        db.StatusTransitions.AddRange(
            new ComplaintStatusTransition { Id = 1, FromStatusCode = "NEW", ToStatusCode = "ASSIGNED" },
            new ComplaintStatusTransition { Id = 2, FromStatusCode = "ASSIGNED", ToStatusCode = "UNDER_PROCESS" },
            new ComplaintStatusTransition { Id = 3, FromStatusCode = "UNDER_PROCESS", ToStatusCode = "RESOLVED", RequiresRemark = true },
            new ComplaintStatusTransition { Id = 4, FromStatusCode = "RESOLVED", ToStatusCode = "CLOSED" },
            new ComplaintStatusTransition { Id = 5, FromStatusCode = "CLOSED", ToStatusCode = "REOPENED", RequiresRemark = true },
            new ComplaintStatusTransition { Id = 6, FromStatusCode = "RESOLVED", ToStatusCode = "REOPENED", IsActive = false });
        db.Priorities.Add(new ComplaintPriority { Code = "MEDIUM", Name = "Medium", Rank = 2, IsDefault = true });
        db.SaveChanges();

        return new TestData { RegionA = ra, RegionB = rb, BranchA1 = a1, BranchA2 = a2, BranchB1 = b1, Dept = dept, Category = cat, SubCategory = sub };
    }

    public Complaint AddComplaint(TestDbContext db, Branch branch, string status = "NEW", Department? dept = null, string? assignedTo = null)
    {
        var c = new Complaint
        {
            ComplaintNumber = $"HGB-2026-{db.Complaints.Count() + 1:D8}",
            CustomerName = "Test", MobileNumber = "9876543210", AccountNumber = "123456789012",
            BranchId = branch.Id, CategoryId = Category.Id, SubCategoryId = SubCategory.Id,
            PriorityCode = "MEDIUM", StatusCode = status, Description = "Test complaint",
            AssignedDepartmentId = dept?.Id, AssignedEmployeeId = assignedTo,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        };
        db.Complaints.Add(c);
        db.SaveChanges();
        return c;
    }
}
