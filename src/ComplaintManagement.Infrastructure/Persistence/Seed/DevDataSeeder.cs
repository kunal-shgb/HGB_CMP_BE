using ComplaintManagement.Application.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.Domain.ValueObjects;
using ComplaintManagement.Infrastructure.IAM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Sample organisation, role mappings and complaints for local development. Runs only in
/// Development and only against an empty database. None of this is real Bank data.
/// </summary>
public static class DevDataSeeder
{
    private static readonly (string Code, string Name, (string Code, string Name)[] Branches)[] Regions =
    [
        ("RO-ROH", "Regional Office Rohtak", [("BR-ROH-001", "Rohtak Main"), ("BR-ROH-002", "Sampla"), ("BR-ROH-003", "Meham")]),
        ("RO-HSR", "Regional Office Hisar", [("BR-HSR-001", "Hisar City"), ("BR-HSR-002", "Hansi"), ("BR-HSR-003", "Barwala")]),
        ("RO-KNL", "Regional Office Karnal", [("BR-KNL-001", "Karnal Sector 12"), ("BR-KNL-002", "Assandh"), ("BR-KNL-003", "Nilokheri")]),
        ("RO-RWR", "Regional Office Rewari", [("BR-RWR-001", "Rewari Main"), ("BR-RWR-002", "Bawal")]),
    ];

    private static readonly (string Code, string Name)[] Departments =
    [
        ("DBD", "Digital Banking Division"),
        ("CSD", "Customer Service Department"),
        ("CRD", "Credit Department"),
        ("OPS", "Operations Department"),
    ];

    private static readonly (string IamRole, string AppRole)[] RoleMappings =
    [
        ("CMP_SUPER_ADMIN", "SUPER_ADMIN"), ("CMP_HO_ADMIN", "HO_ADMIN"), ("CMP_HO_DEPT", "HO_DEPARTMENT_USER"),
        ("CMP_RO_USER", "REGIONAL_OFFICE_USER"), ("CMP_BRANCH_USER", "BRANCH_USER"), ("CMP_NODAL", "NODAL_OFFICER"),
        ("CMP_MANAGEMENT", "MANAGEMENT"), ("CMP_AUDITOR", "AUDITOR"),
    ];

    private static readonly string[] Names =
    [
        "Ramesh Kumar", "Sunita Devi", "Mahender Singh", "Kavita Rani", "Rajbir Malik", "Pooja Yadav", "Sandeep Hooda",
        "Anita Sharma", "Jagdish Chand", "Neelam Saini", "Vikram Dahiya", "Suman Lata", "Harish Goyal", "Meena Kumari",
    ];

    private static readonly string[] Descriptions =
    [
        "Amount debited from my account but the UPI transaction failed. Beneficiary has not received the money.",
        "ATM did not dispense cash but my account was debited.",
        "Passbook has not been updated for the last three months at the branch.",
        "Loan statement shows charges that were not explained to me.",
        "Mobile banking app shows an error while adding a beneficiary.",
        "NEFT transfer made last week has not been credited to the beneficiary account.",
        "Pension for this month has not been credited to my account.",
        "Staff at the branch counter did not accept my cash deposit form.",
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevDataSeeder));
        var devUsers = scope.ServiceProvider.GetRequiredService<IOptions<DevIdentityOptions>>().Value.Users;

        if (await db.Regions.AnyAsync(ct)) return;
        logger.LogInformation("Seeding development sample data");

        var branches = new List<Branch>();
        foreach (var (code, name, branchRows) in Regions)
        {
            var region = new Region { Code = code, Name = name };
            db.Regions.Add(region);
            foreach (var (bCode, bName) in branchRows)
            {
                var branch = new Branch { Code = bCode, Name = bName, RegionId = region.Id, Region = region };
                branches.Add(branch);
                db.Branches.Add(branch);
            }
        }

        var departments = Departments.Select(d => new Department { Code = d.Code, Name = d.Name }).ToList();
        db.Departments.AddRange(departments);
        db.ApplicationRoleMappings.AddRange(RoleMappings.Select(m => new ApplicationRoleMapping { IamRole = m.IamRole, ApplicationRole = m.AppRole }));

        // Sample TAT so SLA states are visible locally. Real TAT values are pending from the Bank.
        var subCategories = await db.SubCategories.Include(s => s.Category).ToListAsync(ct);
        foreach (var sub in subCategories)
        {
            sub.TatDays = sub.Category!.GroupName == "Digital Banking" ? 7 : 15;
            if (sub.Category.GroupName == "Digital Banking") sub.DefaultDepartmentId = departments[0].Id;
        }

        var statuses = await db.Statuses.ToDictionaryAsync(s => s.Code, ct);
        var assignees = devUsers.Where(u => u.BranchCode is not null || u.RegionCode is not null || u.DepartmentCode is not null).ToList();
        var random = new Random(20260924);
        var now = DateTimeOffset.UtcNow;
        var year = IstDate.ToIstDate(now).Year;
        var flow = new[] { "NEW", "RECEIVED", "ASSIGNED", "UNDER_PROCESS", "RESOLVED", "CLOSED" };
        var priorities = new[] { "LOW", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "CRITICAL" };
        const int count = 120;

        for (var i = 1; i <= count; i++)
        {
            var created = now.AddDays(-random.Next(0, 45)).AddHours(-random.Next(0, 23)).AddMinutes(-random.Next(0, 59));
            var sub = subCategories[random.Next(subCategories.Count)];
            var branch = branches[random.Next(branches.Count)];
            var steps = created > now.AddDays(-3) ? random.Next(0, 3) : random.Next(0, flow.Length);
            var digital = sub.Category!.GroupName == "Digital Banking";

            var complaint = new Complaint
            {
                ComplaintNumber = ComplaintNumber.Format(year, i),
                CustomerName = Names[random.Next(Names.Length)],
                MobileNumber = $"9{random.Next(100_000_000, 999_999_999)}",
                Email = random.Next(3) == 0 ? $"customer{i}@example.com" : null,
                CustomerId = $"CIF{random.Next(10_000_000, 99_999_999)}",
                AccountNumber = $"{random.Next(1000, 9999)}{random.Next(10_000_000, 99_999_999)}",
                PreferredChannel = random.Next(2) == 0 ? "SMS" : "EMAIL",
                BranchId = branch.Id,
                CategoryId = sub.CategoryId,
                SubCategoryId = sub.Id,
                PriorityCode = priorities[random.Next(priorities.Length)],
                StatusCode = "NEW",
                Description = Descriptions[random.Next(Descriptions.Length)],
                TransactionId = digital ? $"{random.Next(100_000, 999_999)}{random.Next(100_000, 999_999)}" : null,
                TransactionDate = digital ? DateOnly.FromDateTime(created.AddDays(-1).UtcDateTime) : null,
                TransactionAmount = digital ? random.Next(100, 50_000) : null,
                AssignedDepartmentId = sub.DefaultDepartmentId,
                SlaDueDate = SlaCalculator.DueDate(created, sub.TatDays),
                CreatedAt = created,
                UpdatedAt = created,
            };
            complaint.StatusHistory.Add(new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id, NewStatusCode = "NEW", ChangedByEmployeeId = "CUSTOMER",
                ChangedByName = "Customer", Remarks = "Lodged through the Bank website", ChangedAt = created,
            });

            var at = created;
            for (var s = 1; s <= steps; s++)
            {
                at = at.AddHours(random.Next(2, 60));
                if (at > now) break;
                var code = flow[s];
                var actor = assignees.Count > 0 ? assignees[random.Next(assignees.Count)] : null;
                if (code == "ASSIGNED" && actor is not null)
                {
                    complaint.AssignedEmployeeId = actor.EmployeeId;
                    complaint.AssignedEmployeeName = actor.Name;
                    complaint.Assignments.Add(new ComplaintAssignment
                    {
                        ComplaintId = complaint.Id, AssignedToEmployeeId = actor.EmployeeId, AssignedToName = actor.Name,
                        AssignedByEmployeeId = "E1001", AssignedAt = at, Remarks = "Please examine and resolve.",
                    });
                }
                if (code == "UNDER_PROCESS" && random.Next(2) == 0)
                {
                    complaint.Remarks.Add(new ComplaintRemark
                    {
                        ComplaintId = complaint.Id, Remark = "Transaction log requested from the switch team.",
                        Visibility = RemarkVisibility.Internal, CreatedByEmployeeId = actor?.EmployeeId ?? "E1001",
                        CreatedByName = actor?.Name, CreatedAt = at,
                    });
                }
                if (code == "RESOLVED")
                {
                    complaint.ResolvedAt = at;
                    complaint.Remarks.Add(new ComplaintRemark
                    {
                        ComplaintId = complaint.Id,
                        Remark = "The amount has been reversed to your account. Please check your statement.",
                        Visibility = RemarkVisibility.Customer, CreatedByEmployeeId = actor?.EmployeeId ?? "E1001",
                        CreatedByName = actor?.Name, CreatedAt = at,
                    });
                }
                if (statuses[code].IsTerminal) complaint.ClosedAt = at;

                complaint.StatusHistory.Add(new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id, OldStatusCode = complaint.StatusCode, NewStatusCode = code,
                    ChangedByEmployeeId = actor?.EmployeeId ?? "E1001", ChangedByName = actor?.Name, ChangedAt = at,
                });
                complaint.StatusCode = code;
                complaint.UpdatedAt = at;
            }
            db.Complaints.Add(complaint);
        }

        db.ComplaintNumberSequences.Add(new ComplaintNumberSequence { Year = year, LastValue = count });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} sample complaints", count);
    }
}
