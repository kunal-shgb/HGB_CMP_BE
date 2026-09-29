using ComplaintManagement.Application.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.Domain.ValueObjects;
using ComplaintManagement.Infrastructure.IAM.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Sample complaints for local development. Runs only in
/// Development and only against an empty database. None of this is real Bank data.
/// </summary>
public static class DevDataSeeder
{
    private static readonly string[] Names =
    [
        "Ramesh Kumar", "Sunita Devi", "Mahender Singh", "Kavita Rani", "Rajbir Malik", "Pooja Yadav", "Sandeep Hooda",
        "Anita Sharma", "Jagdish Chand", "Neelam Saini", "Vikram Dahiya", "Suman Lata", "Harish Goyal", "Meena Kumari",
    ];

    private static readonly (string Title, string Description)[] Samples =
    [
        ("UPI payment failed but amount debited", "Amount debited from my account but the UPI transaction failed. Beneficiary has not received the money."),
        ("ATM did not dispense cash", "ATM did not dispense cash but my account was debited."),
        ("Passbook not updated", "Passbook has not been updated for the last three months at the branch."),
        ("Unexplained charges on loan statement", "Loan statement shows charges that were not explained to me."),
        ("Cannot add beneficiary in mobile app", "Mobile banking app shows an error while adding a beneficiary."),
        ("NEFT transfer not credited", "NEFT transfer made last week has not been credited to the beneficiary account."),
        ("Pension not credited", "Pension for this month has not been credited to my account."),
        ("Cash deposit refused at counter", "Staff at the branch counter did not accept my cash deposit form."),
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevDataSeeder));
        // Sample assignments use the dummy employees from the mock IAM store.
        var devUsers = MockIamSeedData.Users;

        if (await db.Complaints.AnyAsync(ct)) return;
        logger.LogInformation("Seeding development sample data");

        // Offices come from the (mock) IAM organisation data; complaints keep a snapshot of them.
        var regionNames = MockIamSeedData.Regions.ToDictionary(r => r.Code, r => r.Name);
        var branches = MockIamSeedData.Branches
            .Select(b => (b.Code, b.Name, RegionCode: b.RegionCode, RegionName: regionNames[b.RegionCode]))
            .ToList();
        var departmentNames = MockIamSeedData.Departments.ToDictionary(d => d.Code, d => d.Name);

        // Sample: Customer Service Department's Checkers decide Head Office Makers' requests.
        var hoDept = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == AppSettingKeys.HeadOfficeMakerCheckerDepartment, ct);
        if (hoDept is not null) { hoDept.Value = "CSD"; hoDept.UpdatedAt = DateTimeOffset.UtcNow; hoDept.UpdatedBy = "SYSTEM"; }

        // Sample TAT so SLA states are visible locally. Real TAT values are pending from the Bank.
        var categories = await db.Categories.Include(c => c.Group).ToListAsync(ct);
        foreach (var category in categories)
        {
            var digitalGroup = category.Group!.Code == "DIGITAL_BANKING";
            category.TatDays = digitalGroup ? 7 : 15;
            if (digitalGroup) category.DefaultDepartmentCode = "DBD";
        }

        var statuses = await db.Statuses.ToDictionaryAsync(s => s.Code, ct);
        var assignees = devUsers.Where(u => u.IsActive && u.AccessRole is "Maker" or "OfficeHead").ToList();
        var random = new Random(20260924);
        var now = DateTimeOffset.UtcNow;
        var year = IstDate.ToIstDate(now).Year;
        var flow = new[] { "NEW", "RECEIVED", "ASSIGNED", "UNDER_PROCESS", "RESOLVED", "CLOSED" };
        var priorities = new[] { "LOW", "MEDIUM", "MEDIUM", "MEDIUM", "HIGH", "CRITICAL" };
        const int count = 120;

        for (var i = 1; i <= count; i++)
        {
            var created = now.AddDays(-random.Next(0, 45)).AddHours(-random.Next(0, 23)).AddMinutes(-random.Next(0, 59));
            var category = categories[random.Next(categories.Count)];
            var branch = branches[random.Next(branches.Count)];
            var steps = created > now.AddDays(-3) ? random.Next(0, 3) : random.Next(0, flow.Length);
            var digital = category.Group!.Code == "DIGITAL_BANKING";
            var sample = Samples[random.Next(Samples.Length)];

            var complaint = new Complaint
            {
                ComplaintNumber = ComplaintNumber.Format(year, i),
                CustomerName = Names[random.Next(Names.Length)],
                MobileNumber = $"9{random.Next(100_000_000, 999_999_999)}",
                Email = random.Next(3) == 0 ? $"customer{i}@example.com" : null,
                CustomerId = $"CIF{random.Next(10_000_000, 99_999_999)}",
                AccountNumber = $"{random.Next(1000, 9999)}{random.Next(10_000_000, 99_999_999)}",
                PreferredChannel = random.Next(2) == 0 ? "SMS" : "EMAIL",
                BranchCode = branch.Code,
                BranchName = branch.Name,
                RegionCode = branch.RegionCode,
                RegionName = branch.RegionName,
                CategoryId = category.Id,
                PriorityCode = priorities[random.Next(priorities.Length)],
                StatusCode = "NEW",
                Title = sample.Title,
                Description = sample.Description,
                TransactionId = digital ? $"{random.Next(100_000, 999_999)}{random.Next(100_000, 999_999)}" : null,
                TransactionDate = digital ? DateOnly.FromDateTime(created.AddDays(-1).UtcDateTime) : null,
                TransactionAmount = digital ? random.Next(100, 50_000) : null,
                AssignedDepartmentCode = category.DefaultDepartmentCode,
                AssignedDepartmentName = category.DefaultDepartmentCode is { } dept ? departmentNames[dept] : null,
                SlaDueDate = SlaCalculator.DueDate(created, category.TatDays),
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
                // Prefer staff of the complaint's own branch, then its RO, then Head Office.
                var local = assignees.Where(u => u.OfficeCode == branch.Code).ToList();
                if (local.Count == 0) local = assignees.Where(u => u.OfficeCode == branch.RegionCode).ToList();
                if (local.Count == 0) local = assignees.Where(u => u.OfficeType == "Head Office").ToList();
                var actor = local.Count > 0 ? local[random.Next(local.Count)] : null;
                if (code == "ASSIGNED" && actor is not null)
                {
                    complaint.AssignedEmployeeId = actor.EmployeeCode;
                    complaint.AssignedEmployeeName = actor.FullName;
                    complaint.Assignments.Add(new ComplaintAssignment
                    {
                        ComplaintId = complaint.Id, AssignedToEmployeeId = actor.EmployeeCode, AssignedToName = actor.FullName,
                        AssignedByEmployeeId = "200001", AssignedAt = at, Remarks = "Please examine and resolve.",
                    });
                }
                if (code == "UNDER_PROCESS" && random.Next(2) == 0)
                {
                    complaint.Remarks.Add(new ComplaintRemark
                    {
                        ComplaintId = complaint.Id, Remark = "Transaction log requested from the switch team.",
                        Visibility = RemarkVisibility.Internal, CreatedByEmployeeId = actor?.EmployeeCode ?? "100001",
                        CreatedByName = actor?.FullName, CreatedAt = at,
                    });
                }
                if (code == "RESOLVED")
                {
                    complaint.ResolvedAt = at;
                    complaint.Remarks.Add(new ComplaintRemark
                    {
                        ComplaintId = complaint.Id,
                        Remark = "The amount has been reversed to your account. Please check your statement.",
                        Visibility = RemarkVisibility.Customer, CreatedByEmployeeId = actor?.EmployeeCode ?? "100001",
                        CreatedByName = actor?.FullName, CreatedAt = at,
                    });
                }
                if (statuses[code].IsTerminal) complaint.ClosedAt = at;

                complaint.StatusHistory.Add(new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id, OldStatusCode = complaint.StatusCode, NewStatusCode = code,
                    ChangedByEmployeeId = actor?.EmployeeCode ?? "100001", ChangedByName = actor?.FullName, ChangedAt = at,
                });
                complaint.StatusCode = code;
                complaint.UpdatedAt = at;
            }

            // Some complaints under process have a Maker's resolution waiting for a Checker.
            if (complaint.StatusCode is "UNDER_PROCESS" or "ASSIGNED" && random.Next(4) > 0)
            {
                var maker = assignees.FirstOrDefault(u => u.OfficeCode == branch.Code)
                    ?? assignees.FirstOrDefault(u => u.OfficeCode == branch.RegionCode)
                    ?? assignees.FirstOrDefault(u => u.OfficeType == "Head Office");
                if (maker is not null)
                {
                    var requestedAt = at.AddHours(random.Next(1, 20));
                    if (requestedAt > now) requestedAt = now.AddMinutes(-5);
                    var byBranch = maker.OfficeType == "Branch";
                    var byHeadOffice = maker.OfficeType == "Head Office";
                    db.ComplaintApprovals.Add(new ComplaintApproval
                    {
                        ComplaintId = complaint.Id,
                        RequestedStatusCode = "RESOLVED",
                        PreviousStatusCode = complaint.StatusCode,
                        MakerRemarks = "Amount reversed to the customer's account; reversal confirmed with the switch team.",
                        RequestedByEmployeeId = maker.EmployeeCode,
                        RequestedByName = maker.FullName,
                        RequestedByOfficeName = maker.OfficeName,
                        RequestedAt = requestedAt,
                        ApproverLevel = byBranch ? ScopeLevel.Region : ScopeLevel.HeadOffice,
                        ApproverOfficeCode = byBranch ? branch.RegionCode : null,
                        ApproverDepartment = byHeadOffice ? "CSD" : null,
                    });
                    complaint.StatusHistory.Add(new ComplaintStatusHistory
                    {
                        ComplaintId = complaint.Id, OldStatusCode = complaint.StatusCode, NewStatusCode = "PENDING_APPROVAL",
                        ChangedByEmployeeId = maker.EmployeeCode, ChangedByName = maker.FullName, ChangedAt = requestedAt,
                        Remarks = "Requested: Resolved. Amount reversed to the customer's account.",
                    });
                    complaint.StatusCode = "PENDING_APPROVAL";
                    complaint.UpdatedAt = requestedAt;
                }
            }
            db.Complaints.Add(complaint);
        }

        db.ComplaintNumberSequences.Add(new ComplaintNumberSequence { Year = year, LastValue = count });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} sample complaints", count);
    }
}
