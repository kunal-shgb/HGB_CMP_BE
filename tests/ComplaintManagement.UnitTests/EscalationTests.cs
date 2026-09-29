using ComplaintManagement.Application.Admin;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Escalation;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class AutomaticEscalationTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 27, 6, 0, 0, TimeSpan.Zero));

    public AutomaticEscalationTests() => _data = TestData.Seed(_db);

    private EscalationService Service => new(_db, _clock, NullLogger<EscalationService>.Instance);

    private Complaint Overdue(double days, string status = "UNDER_PROCESS")
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status);
        c.SlaDueDate = _clock.Now.AddDays(-days);
        if (status == "CLOSED") c.ClosedAt = _clock.Now.AddDays(-1);
        _db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task Overdue_complaint_goes_to_the_Regional_Office()
    {
        var c = Overdue(1.5);
        Assert.Equal(1, await Service.RunAsync(default));

        Assert.Equal(EscalationLevels.RegionalOffice, _db.Complaints.Single().EscalationLevel);
        var e = _db.ComplaintEscalations.Single();
        Assert.Equal((1, 2, "SYSTEM", "TAT exceeded by 1 day"), (e.FromLevel, e.ToLevel, e.EscalatedBy, e.Reason));
        Assert.Contains(_db.AuditLogs, a => a.Action == "ESCALATE" && a.RecordId == c.Id.ToString());
    }

    [Fact]
    public async Task Long_overdue_complaint_goes_straight_to_Head_Office()
    {
        Overdue(8);
        await Service.RunAsync(default);
        var e = _db.ComplaintEscalations.Single();
        Assert.Equal((1, 3), (e.FromLevel, e.ToLevel));
    }

    [Fact]
    public async Task Complaints_on_time_closed_or_without_TAT_are_left_alone()
    {
        Overdue(-2);            // due in two days
        Overdue(5, "CLOSED");   // closed
        _data.AddComplaint(_db, _data.BranchA1); // no TAT
        Assert.Equal(0, await Service.RunAsync(default));
        Assert.All(_db.Complaints, c => Assert.Equal(1, c.EscalationLevel));
    }

    [Fact]
    public async Task Resolved_complaints_awaiting_closure_are_not_escalated()
    {
        var c = Overdue(3, "RESOLVED");
        c.ResolvedAt = _clock.Now.AddDays(-1);
        _db.SaveChanges();
        Assert.Equal(0, await Service.RunAsync(default));
    }

    [Fact]
    public async Task Running_again_does_not_escalate_twice()
    {
        Overdue(2);
        await Service.RunAsync(default);
        Assert.Equal(0, await Service.RunAsync(default));
        Assert.Single(_db.ComplaintEscalations);
    }

    [Fact]
    public async Task Admin_can_switch_it_off_and_change_thresholds()
    {
        Overdue(2);
        _db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.EscalationEnabled, Value = "false" });
        _db.SaveChanges();
        Assert.Equal(0, await Service.RunAsync(default));

        _db.AppSettings.Single().Value = "true";
        _db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.EscalateToRegionalOfficeAfterDays, Value = "3" });
        _db.SaveChanges();
        Assert.Equal(0, await Service.RunAsync(default)); // only 2 days overdue
    }
}

public class ManualEscalationTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(DateTimeOffset.UtcNow);

    public ManualEscalationTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(FakeUser user)
    {
        var iam = new FakeIam();
        return new(
        _db, user, new FakeAudit(), iam, _data.Org, _clock, Options.Create(new SlaOptions()),
        new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
        new ComplaintManagement.Application.Notifications.CustomerNotifier(_db, _clock),
        FakeRoleMappings.Policy(iam), null!);
    }

    private static FakeUser BranchMaker => FakeUser.AtBranch("E-B", "A1", AppRoles.Maker);
    private static FakeUser RoChecker => FakeUser.AtRegion("E-R", "RA", AppRoles.Checker);

    [Fact]
    public async Task Branch_escalates_to_its_RO_and_RO_escalates_to_HO()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        Assert.True((await Service(BranchMaker).GetAsync(c.Id, default)).CanEscalate);

        await Service(BranchMaker).EscalateAsync(c.Id, new EscalateRequest("Customer called three times"), default);
        Assert.Equal(2, _db.Complaints.Single().EscalationLevel);
        Assert.False((await Service(BranchMaker).GetAsync(c.Id, default)).CanEscalate);
        await Assert.ThrowsAsync<DomainException>(() => Service(BranchMaker).EscalateAsync(c.Id, new EscalateRequest("again"), default));

        await Service(RoChecker).EscalateAsync(c.Id, new EscalateRequest("Needs HO decision"), default);
        Assert.Equal(3, _db.Complaints.Single().EscalationLevel);
        Assert.Equal([(1, 2), (2, 3)], _db.ComplaintEscalations.OrderBy(e => e.ToLevel).Select(e => new ValueTuple<int, int>(e.FromLevel, e.ToLevel)));
    }

    [Fact]
    public async Task RO_escalates_a_branch_level_complaint_directly_to_HO()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(RoChecker).EscalateAsync(c.Id, new EscalateRequest("Serious"), default);
        Assert.Equal(3, _db.Complaints.Single().EscalationLevel);
    }

    [Fact]
    public async Task Head_office_closed_complaints_and_missing_reasons_are_refused()
    {
        var open = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        Assert.False((await Service(new FakeUser("E-H", AppRoles.Maker)).GetAsync(open.Id, default)).CanEscalate);
        await Assert.ThrowsAsync<DomainException>(() => Service(BranchMaker).EscalateAsync(open.Id, new EscalateRequest(" "), default));

        var closed = _data.AddComplaint(_db, _data.BranchA1, "CLOSED");
        closed.ClosedAt = _clock.Now;
        _db.SaveChanges();
        await Assert.ThrowsAsync<DomainException>(() => Service(BranchMaker).EscalateAsync(closed.Id, new EscalateRequest("x"), default));
    }

    [Fact]
    public async Task Escalation_appears_in_the_history()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchMaker).EscalateAsync(c.Id, new EscalateRequest("Customer called three times"), default);
        var e = Assert.Single(await Service(BranchMaker).GetHistoryAsync(c.Id, default), x => x.Type == "ESCALATION");
        Assert.Equal(("Escalated to Regional Office (level 2)", "Customer called three times"), (e.Title, e.Detail));
    }
}

public class EscalationSettingsTests
{
    [Fact]
    public async Task Settings_are_validated_and_saved()
    {
        var db = TestDbContext.Create();
        var data = TestData.Seed(db);
        var admin = new AdminService(db, data.Org, new FakeUser("900001", AppRoles.Admin), new FakeAudit(), new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DomainException>(() => admin.UpdateEscalationSettingsAsync(new UpdateEscalationSettingsRequest(true, 5, 2), default));
        await Assert.ThrowsAsync<DomainException>(() => admin.UpdateEscalationSettingsAsync(new UpdateEscalationSettingsRequest(true, -1, 2), default));

        await admin.UpdateEscalationSettingsAsync(new UpdateEscalationSettingsRequest(false, 1, 10), default);
        var saved = (await admin.GetWorkflowAsync(default)).Escalation;
        Assert.Equal((false, 1, 10), (saved.Enabled, saved.ToRegionalOfficeAfterDays, saved.ToHeadOfficeAfterDays));
    }
}
