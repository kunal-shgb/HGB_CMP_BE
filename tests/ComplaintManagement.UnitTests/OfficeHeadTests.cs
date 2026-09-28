using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class RoleResolutionTests
{
    // Mirrors the seeded mappings: OfficeHead only at branches; Maker/Checker at RO and HO.
    private static readonly RoleMapping[] Seeded =
    [
        new("OfficeHead", "Branch", AppRoles.OfficeHead),
        new("Maker", "Regional Office", AppRoles.Maker), new("Maker", "Head Office", AppRoles.Maker),
        new("Checker", "Regional Office", AppRoles.Checker), new("Checker", "Head Office", AppRoles.Checker),
    ];

    [Theory]
    [InlineData("OfficeHead", "Branch", false, new[] { AppRoles.OfficeHead, AppRoles.Viewer })]
    [InlineData("NoRole", "Branch", false, new[] { AppRoles.Viewer })]
    [InlineData("Maker", "Branch", false, new[] { AppRoles.Viewer })]          // Makers do not act at branches
    [InlineData("OfficeHead", "Regional Office", false, new[] { AppRoles.Viewer })] // branch-only for now
    [InlineData("Maker", "Regional Office", false, new[] { AppRoles.Maker, AppRoles.Viewer })]
    [InlineData("Checker", "Head Office", false, new[] { AppRoles.Checker, AppRoles.Viewer })]
    [InlineData("NoRole", "Head Office", true, new[] { AppRoles.Viewer, AppRoles.Admin })]
    public void Roles_come_from_access_role_office_and_system_admin_flag(string accessRole, string officeType, bool systemAdmin, string[] expected) =>
        Assert.Equal(expected.Order(), RoleMappingService.ResolveAll(Seeded, [accessRole], officeType, systemAdmin).Order());

    [Fact]
    public void Viewer_can_only_look()
    {
        var p = Permissions.ForRoles([AppRoles.Viewer]);
        Assert.Equal([Permissions.ComplaintView, Permissions.DashboardView], p.Order());
    }
}

public class OfficeHeadDelegationTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(DateTimeOffset.UtcNow);

    private static readonly FakeUser Head = FakeUser.AtBranch("300001", "A1", AppRoles.OfficeHead, AppRoles.Viewer);
    private static readonly FakeUser Clerk = FakeUser.AtBranch("300005", "A1", AppRoles.Viewer);
    private static readonly FakeUser OtherClerk = FakeUser.AtBranch("300006", "A1", AppRoles.Viewer);

    public OfficeHeadDelegationTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(FakeUser user)
    {
        var iam = new FakeIam(Staff.At("300005", "Branch", "A1"), Staff.At("300006", "Branch", "A1"));
        return new(
        _db, user, new FakeAudit(),
        iam,
        _data.Org, _clock, Options.Create(new SlaOptions()),
        new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
        new CustomerNotifier(_db, _clock),
        FakeRoleMappings.Policy(iam));
    }

    [Fact]
    public async Task Branch_staff_can_see_but_not_act_until_assigned()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");

        var view = await Service(Clerk).GetAsync(c.Id, default);
        Assert.True(view.Customer.IsMasked);
        Assert.Empty(view.AllowedTransitions);
        Assert.Equal(new ComplaintManagement.Contracts.Responses.ComplaintAbilities(false, false, false, false, false, false, false), view.Abilities);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).AddRemarkAsync(c.Id, new AddRemarkRequest("x", "INTERNAL"), default));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).EscalateAsync(c.Id, new EscalateRequest("x"), default));
    }

    [Fact]
    public async Task Office_head_assigns_and_the_assignee_can_work_that_complaint()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(Head).AssignAsync(c.Id, new AssignComplaintRequest("300005", null, "Please handle"), default);

        var view = await Service(Clerk).GetAsync(c.Id, default);
        Assert.False(view.Customer.IsMasked);
        Assert.True(view.Abilities is { ChangeStatus: true, AddRemark: true, AddAttachment: true, Escalate: true, IsAssignedToMe: true, Assign: false });

        await Service(Clerk).AddRemarkAsync(c.Id, new AddRemarkRequest("Called the customer", "INTERNAL"), default);
        var result = await Service(Clerk).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);

        // The assignee is at a branch, so the decision still goes to the branch's RO Checker.
        Assert.True(result.PendingApproval);
        Assert.Equal((ScopeLevel.Region, "RA"), (_db.ComplaintApprovals.Single().ApproverLevel, _db.ComplaintApprovals.Single().ApproverOfficeCode));
    }

    [Fact]
    public async Task Assignee_cannot_reassign_and_loses_rights_when_reassigned()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(Head).AssignAsync(c.Id, new AssignComplaintRequest("300005", null, null), default);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).AssignAsync(c.Id, new AssignComplaintRequest("300006", null, null), default));

        await Service(Head).AssignAsync(c.Id, new AssignComplaintRequest("300006", null, null), default);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).AddRemarkAsync(c.Id, new AddRemarkRequest("x", "INTERNAL"), default));
        await Service(OtherClerk).AddRemarkAsync(c.Id, new AddRemarkRequest("Taking over", "INTERNAL"), default);
    }

    [Fact]
    public async Task Assignment_does_not_reach_other_complaints()
    {
        var mine = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        var other = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        await Service(Head).AssignAsync(mine.Id, new AssignComplaintRequest("300005", null, null), default);

        await Service(Clerk).AddRemarkAsync(mine.Id, new AddRemarkRequest("ok", "INTERNAL"), default);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(Clerk).AddRemarkAsync(other.Id, new AddRemarkRequest("no", "INTERNAL"), default));
    }
}

public class RegionalCheckerAssignmentTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(DateTimeOffset.UtcNow);
    private static readonly FakeUser RoChecker = FakeUser.AtRegion("200001", "RA", AppRoles.Checker, AppRoles.Viewer);
    private static readonly FakeUser RoMaker = FakeUser.AtRegion("200003", "RA", AppRoles.Maker, AppRoles.Viewer);

    public RegionalCheckerAssignmentTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(FakeUser user, params IamUser[] directory)
    {
        var iam = new FakeIam([.. directory, Staff.At("300005", "Branch", "A1"), Staff.At("300007", "Branch", "A2")]);
        return new(_db, user, new FakeAudit(), iam, _data.Org, _clock, Options.Create(new SlaOptions()),
            new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
            new CustomerNotifier(_db, _clock), FakeRoleMappings.Policy(iam));
    }

    [Fact]
    public async Task RO_checker_cannot_assign_when_the_branch_has_an_office_head()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        var svc = Service(RoChecker, Staff.Head("300001", "A1"));

        Assert.False((await svc.GetAsync(c.Id, default)).Abilities.Assign);
        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => svc.AssignAsync(c.Id, new AssignComplaintRequest("300005", null, null), default));
        Assert.Contains("office head", ex.Message);
    }

    [Fact]
    public async Task RO_checker_assigns_when_the_branch_has_no_active_office_head()
    {
        var noHead = _data.AddComplaint(_db, _data.BranchA2, "UNDER_PROCESS");
        var svc = Service(RoChecker, Staff.Head("300001", "A1"), Staff.Head("300008", "A2", active: false));

        Assert.True((await svc.GetAsync(noHead.Id, default)).Abilities.Assign);
        await svc.AssignAsync(noHead.Id, new AssignComplaintRequest("300007", null, "Branch head on leave"), default);
        Assert.Equal("300007", _db.Complaints.Single(x => x.Id == noHead.Id).AssignedEmployeeId);
    }

    [Fact]
    public async Task RO_makers_keep_their_assignment_rights()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        var svc = Service(RoMaker, Staff.Head("300001", "A1"));
        await svc.AssignAsync(c.Id, new AssignComplaintRequest("300005", null, null), default);
    }
}
