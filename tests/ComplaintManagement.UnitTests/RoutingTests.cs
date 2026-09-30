using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Escalation;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

/// <summary>The Admin-defined flow of a category: RO and HO divisions, and categories that skip the RO.</summary>
public class RoutingTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 27, 6, 0, 0, TimeSpan.Zero));

    private static readonly FakeUser BranchHead = FakeUser.AtBranch("300001", "A1", AppRoles.OfficeHead, AppRoles.Viewer);
    private static FakeUser Ro(string id, string role, string division) =>
        new(id, role, AppRoles.Viewer) { OfficeType = "Regional Office", OfficeCode = "RA", DepartmentName = division };
    private static FakeUser Ho(string id, string role, string division) =>
        new FakeUser(id, role, AppRoles.Viewer) { DepartmentName = division };

    public RoutingTests()
    {
        _data = TestData.Seed(_db);
        var upi = _db.Categories.Single();
        upi.RoDivisionCode = "DBD";
        upi.HoDivisionCode = "CSD";
        _db.SaveChanges();
    }

    private void SkipRo()
    {
        var upi = _db.Categories.Single();
        upi.DirectToHeadOffice = true;
        upi.RoDivisionCode = null;
        _db.SaveChanges();
    }

    private ComplaintService Service(FakeUser user)
    {
        var iam = new FakeIam();
        var org = new FakeOrg([_data.BranchA1], [_data.Dept, new IamDepartment("CSD", "Customer Service Department", true)]);
        return new(_db, user, new FakeAudit(), iam, org, _clock, Options.Create(new SlaOptions()),
            new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
            new CustomerNotifier(_db, _clock), FakeRoleMappings.Policy(iam), null!);
    }

    [Fact]
    public async Task Branch_escalation_goes_to_the_categorys_RO_division_then_its_HO_division()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).EscalateAsync(c.Id, new EscalateRequest("Customer waiting"), default);
        Assert.Equal((EscalationLevels.RegionalOffice, "DBD", "Digital Banking Division"), (c.EscalationLevel, c.EscalatedDivisionCode, c.EscalatedDivisionName));

        await Service(Ro("200001", AppRoles.Checker, "DBD")).EscalateAsync(c.Id, new EscalateRequest("Needs HO"), default);
        Assert.Equal((EscalationLevels.HeadOffice, "CSD"), (c.EscalationLevel, c.EscalatedDivisionCode));
        Assert.Equal(["DBD", "CSD"], _db.ComplaintEscalations.OrderBy(e => e.ToLevel).Select(e => e.ToDivisionCode));
    }

    [Fact]
    public async Task Only_the_RO_division_acts_on_a_complaint_escalated_to_it()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).EscalateAsync(c.Id, new EscalateRequest("Customer waiting"), default);

        var otherDivision = Ro("200006", AppRoles.Maker, "CSD");
        var view = await Service(otherDivision).GetAsync(c.Id, default); // can still see it
        Assert.False(view.Abilities.AddRemark);
        Assert.Equal("DBD", view.EscalatedDivision?.Code);
        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Service(otherDivision).AddRemarkAsync(c.Id, new AddRemarkRequest("x", "INTERNAL"), default));
        Assert.Contains("Digital Banking Division", ex.Message);

        await Service(Ro("200003", AppRoles.Maker, "DBD")).AddRemarkAsync(c.Id, new AddRemarkRequest("Looking into it", "INTERNAL"), default);
    }

    [Fact]
    public async Task Branch_approvals_go_to_the_RO_division_Checkers_only()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);

        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.Region, "RA", "DBD"), (approval.ApproverLevel, approval.ApproverOfficeCode, approval.ApproverDepartment));
        Assert.True(ApprovalRules.CanDecide(Ro("200001", AppRoles.Checker, "DBD"), approval));
        Assert.False(ApprovalRules.CanDecide(Ro("200005", AppRoles.Checker, "CSD"), approval));
        Assert.Single(_db.ComplaintApprovals.DecidableBy(Ro("200001", AppRoles.Checker, "DBD")));
        Assert.Empty(_db.ComplaintApprovals.DecidableBy(Ro("200005", AppRoles.Checker, "CSD")));
    }

    [Fact]
    public async Task RO_approvals_go_to_the_HO_division()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(Ro("200003", AppRoles.Maker, "DBD")).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);
        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.HeadOffice, "CSD"), (approval.ApproverLevel, approval.ApproverDepartment));
        Assert.False(ApprovalRules.CanDecide(Ho("100004", AppRoles.Checker, "DBD"), approval));
        Assert.True(ApprovalRules.CanDecide(Ho("100001", AppRoles.Checker, "CSD"), approval));
    }

    [Fact]
    public async Task A_category_can_skip_the_RO_for_escalation_and_approval()
    {
        SkipRo();
        var escalated = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).EscalateAsync(escalated.Id, new EscalateRequest("Fraud suspected"), default);
        Assert.Equal((EscalationLevels.HeadOffice, "CSD"), (escalated.EscalationLevel, escalated.EscalatedDivisionCode));

        var resolved = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).ChangeStatusAsync(resolved.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);
        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.HeadOffice, null, "CSD"), (approval.ApproverLevel, approval.ApproverOfficeCode, approval.ApproverDepartment));
    }

    [Fact]
    public async Task Automatic_escalation_follows_the_route()
    {
        var routed = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        routed.SlaDueDate = _clock.Now.AddDays(-1);
        _db.SaveChanges();
        var auto = new EscalationService(_db, new FakeOrg([_data.BranchA1], [_data.Dept]), _clock, NullLogger<EscalationService>.Instance);
        await auto.RunAsync(default);
        Assert.Equal((EscalationLevels.RegionalOffice, "DBD"), (routed.EscalationLevel, routed.EscalatedDivisionCode));

        SkipRo();
        var direct = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        direct.SlaDueDate = _clock.Now.AddDays(-1);
        _db.SaveChanges();
        await auto.RunAsync(default);
        Assert.Equal((EscalationLevels.HeadOffice, "CSD"), (direct.EscalationLevel, direct.EscalatedDivisionCode));
    }

    [Fact]
    public async Task Categories_without_a_route_keep_the_whole_office()
    {
        var upi = _db.Categories.Single();
        upi.RoDivisionCode = null;
        upi.HoDivisionCode = null;
        _db.SaveChanges();

        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(BranchHead).EscalateAsync(c.Id, new EscalateRequest("Customer waiting"), default);
        Assert.Null(c.EscalatedDivisionCode);
        Assert.True((await Service(Ro("200006", AppRoles.Maker, "CSD")).GetAsync(c.Id, default)).Abilities.AddRemark);
    }
}
