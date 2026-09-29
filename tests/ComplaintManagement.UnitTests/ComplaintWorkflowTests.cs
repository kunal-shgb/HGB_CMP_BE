using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class ComplaintWorkflowTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FakeAudit _audit = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 10, 6, 0, 0, TimeSpan.Zero));

    private static readonly IamUser BranchA1Officer = Staff.At("E-A1", "Branch", "A1");
    private static readonly IamUser BranchB1Officer = Staff.At("E-B1", "Branch", "B1");

    public ComplaintWorkflowTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(ICurrentUser user)
    {
        var iam = new FakeIam(BranchA1Officer, BranchB1Officer);
        return new(
        _db, user, _audit, iam, _data.Org, _clock, Options.Create(new SlaOptions()),
        new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
        new ComplaintManagement.Application.Notifications.CustomerNotifier(_db, _clock),
        FakeRoleMappings.Policy(iam), null!);
    }

    private static FakeUser HoAdmin => new("E1001", AppRoles.Maker);
    private static FakeUser BranchMaker => FakeUser.AtBranch("E-A1", "A1", AppRoles.Maker);
    private static FakeUser RoChecker(string ro = "RA", string id = "E-RC") => FakeUser.AtRegion(id, ro, AppRoles.Checker);
    private static FakeUser HoChecker => new("E-HC", AppRoles.Checker);

    [Fact]
    public async Task Allowed_transition_updates_status_and_writes_history_and_audit()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "ASSIGNED");
        await Service(HoAdmin).ChangeStatusAsync(c.Id, new ChangeStatusRequest("UNDER_PROCESS", null), default);

        Assert.Equal("UNDER_PROCESS", _db.Complaints.Single().StatusCode);
        var history = Assert.Single(_db.ComplaintStatusHistory);
        Assert.Equal(("ASSIGNED", "UNDER_PROCESS", "E1001"), (history.OldStatusCode, history.NewStatusCode, history.ChangedByEmployeeId));
        Assert.Contains(_audit.Entries, e => e.Action == "CHANGE_STATUS" && e.RecordId == c.Id.ToString());
    }

    [Fact]
    public async Task Transition_not_in_workflow_is_rejected()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Service(HoAdmin).ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default));
        Assert.Equal("status.transition_not_allowed", ex.Code);
    }

    [Fact]
    public async Task Inactive_transition_is_rejected()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "RESOLVED");
        await Assert.ThrowsAsync<DomainException>(() =>
            Service(HoAdmin).ChangeStatusAsync(c.Id, new ChangeStatusRequest("REOPENED", "Customer not satisfied"), default));
    }

    [Fact]
    public async Task Transition_requiring_remark_fails_without_one()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Service(HoAdmin).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "  "), default));
        Assert.Equal("status.remark_required", ex.Code);
    }

    [Fact]
    public async Task Resolution_and_closure_stamp_times_and_reopen_clears_them()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");

        await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Amount reversed"), default);
        await Service(RoChecker()).ApproveAsync(_db.ComplaintApprovals.Single().Id, new DecideApprovalRequest(null), default);
        Assert.Equal(_clock.Now, _db.Complaints.Single().ResolvedAt);

        var svc = Service(HoAdmin);
        await svc.ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default);
        Assert.Equal(_clock.Now, _db.Complaints.Single().ClosedAt);

        await svc.ChangeStatusAsync(c.Id, new ChangeStatusRequest("REOPENED", "Customer disputes"), default);
        var reopened = _db.Complaints.Single();
        Assert.Null(reopened.ClosedAt);
        Assert.Null(reopened.ResolvedAt);
    }

    [Fact]
    public async Task Branch_maker_resolution_waits_for_the_branch_RO_checker()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var result = await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);

        Assert.True(result.PendingApproval);
        Assert.Equal("PENDING_APPROVAL", _db.Complaints.Single().StatusCode);
        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.Region, "RA", "E-A1"), (approval.ApproverLevel, approval.ApproverOfficeCode, approval.RequestedByEmployeeId));
        Assert.Null(_db.Complaints.Single().ResolvedAt);
    }

    [Theory]
    [InlineData("Regional Office", "RA")]
    [InlineData("Head Office", "0000")]
    public async Task RO_and_HO_maker_resolution_goes_to_head_office(string officeType, string officeCode)
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var maker = new FakeUser("E-M", AppRoles.Maker) { OfficeType = officeType, OfficeCode = officeCode };
        await Service(maker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Done"), default);

        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.HeadOffice, (string?)null), (approval.ApproverLevel, approval.ApproverOfficeCode));
    }

    [Fact]
    public async Task HO_maker_request_goes_to_the_designated_HO_department()
    {
        _db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.HeadOfficeMakerCheckerDepartment, Value = "CSD" });
        _db.SaveChanges();
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var hoMaker = new FakeUser("E-HM", AppRoles.Maker) { DepartmentName = "DBD" };
        await Service(hoMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Done"), default);

        var approval = _db.ComplaintApprovals.Single();
        Assert.Equal((ScopeLevel.HeadOffice, "CSD"), (approval.ApproverLevel, approval.ApproverDepartment));

        var dbdChecker = new FakeUser("E-D", AppRoles.Checker) { DepartmentName = "DBD" };
        var csdChecker = new FakeUser("E-C", AppRoles.Checker) { DepartmentName = "csd" };
        Assert.Empty(await Service(dbdChecker).ListPendingApprovalsAsync(default));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(dbdChecker).ApproveAsync(approval.Id, new DecideApprovalRequest(null), default));
        Assert.Single(await Service(csdChecker).ListPendingApprovalsAsync(default));
        await Service(csdChecker).ApproveAsync(approval.Id, new DecideApprovalRequest(null), default);
        Assert.Equal("RESOLVED", _db.Complaints.Single().StatusCode);
    }

    [Fact]
    public async Task RO_maker_request_goes_to_any_HO_checker_even_when_a_department_is_set()
    {
        _db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.HeadOfficeMakerCheckerDepartment, Value = "CSD" });
        _db.SaveChanges();
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        await Service(FakeUser.AtRegion("E-RM", "RA", AppRoles.Maker)).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Done"), default);

        Assert.Null(_db.ComplaintApprovals.Single().ApproverDepartment);
        Assert.Single(await Service(new FakeUser("E-D", AppRoles.Checker) { DepartmentName = "DBD" }).ListPendingApprovalsAsync(default));
    }

    [Fact]
    public async Task Checker_queue_shows_only_their_office_and_not_their_own_requests()
    {
        var a1 = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var b1 = _data.AddComplaint(_db, _data.BranchB1, status: "UNDER_PROCESS");
        await Service(BranchMaker).ChangeStatusAsync(a1.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        await Service(FakeUser.AtBranch("E-B1", "B1", AppRoles.Maker)).ChangeStatusAsync(b1.Id, new ChangeStatusRequest("RESOLVED", "x"), default);

        Assert.Equal([a1.ComplaintNumber], (await Service(RoChecker("RA")).ListPendingApprovalsAsync(default)).Select(i => i.ComplaintNumber));
        Assert.Equal([b1.ComplaintNumber], (await Service(RoChecker("RB")).ListPendingApprovalsAsync(default)).Select(i => i.ComplaintNumber));
        Assert.Empty(await Service(HoChecker).ListPendingApprovalsAsync(default));
        Assert.Empty(await Service(BranchMaker).ListPendingApprovalsAsync(default));
    }

    [Fact]
    public async Task Wrong_checkers_and_the_maker_cannot_decide()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        var id = _db.ComplaintApprovals.Single().Id;

        // HO sees the complaint but a branch-level request belongs to the RO Checker.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(HoChecker).ApproveAsync(id, new DecideApprovalRequest(null), default));
        // Another RO cannot even see the complaint.
        await Assert.ThrowsAsync<NotFoundException>(() => Service(RoChecker("RB")).ApproveAsync(id, new DecideApprovalRequest(null), default));
        // A Maker has no approve permission, and never on their own request.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(BranchMaker).ApproveAsync(id, new DecideApprovalRequest(null), default));
    }

    [Fact]
    public async Task Checker_cannot_approve_own_request_even_with_both_roles()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        var both = new FakeUser("E-X", AppRoles.Maker, AppRoles.Checker);
        await Service(both).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Service(both).ApproveAsync(_db.ComplaintApprovals.Single().Id, new DecideApprovalRequest(null), default));
    }

    [Fact]
    public async Task Return_needs_a_remark_and_restores_the_previous_status()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        var id = _db.ComplaintApprovals.Single().Id;

        var ex = await Assert.ThrowsAsync<DomainException>(() => Service(RoChecker()).ReturnAsync(id, new DecideApprovalRequest(" "), default));
        Assert.Equal("approval.remark_required", ex.Code);

        await Service(RoChecker()).ReturnAsync(id, new DecideApprovalRequest("Attach the reversal proof"), default);
        Assert.Equal("UNDER_PROCESS", _db.Complaints.Single().StatusCode);
        Assert.Equal((ApprovalStatus.Returned, "E-RC"), (_db.ComplaintApprovals.Single().Status, _db.ComplaintApprovals.Single().DecidedByEmployeeId));
    }

    [Fact]
    public async Task No_status_change_while_waiting_for_a_checker()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "again"), default));
        Assert.Equal("approval.pending", ex.Code);

        var detail = await Service(RoChecker()).GetAsync(c.Id, default);
        Assert.NotNull(detail.PendingApproval);
        Assert.True(detail.PendingApproval.CanDecide);
        Assert.Empty(detail.AllowedTransitions);
    }

    [Fact]
    public async Task Decided_request_cannot_be_decided_again()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "UNDER_PROCESS");
        await Service(BranchMaker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "x"), default);
        var id = _db.ComplaintApprovals.Single().Id;
        await Service(RoChecker()).ApproveAsync(id, new DecideApprovalRequest(null), default);
        var ex = await Assert.ThrowsAsync<DomainException>(() => Service(RoChecker("RA", "E-RC2")).ApproveAsync(id, new DecideApprovalRequest(null), default));
        Assert.Equal("approval.already_decided", ex.Code);
    }

    [Fact]
    public async Task Out_of_scope_complaint_is_not_found()
    {
        var c = _data.AddComplaint(_db, _data.BranchB1, status: "ASSIGNED");
        var branchUser = FakeUser.AtBranch("E-A1", "A1", AppRoles.Maker);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service(branchUser).ChangeStatusAsync(c.Id, new ChangeStatusRequest("UNDER_PROCESS", null), default));
    }

    [Fact]
    public async Task Assign_records_assignment_and_moves_to_assigned_status()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        await Service(HoAdmin).AssignAsync(c.Id, new AssignComplaintRequest("E-A1", "DBD", "Please resolve"), default);

        var saved = _db.Complaints.Single();
        Assert.Equal(("E-A1", "ASSIGNED", "DBD", "Digital Banking Division"), (saved.AssignedEmployeeId, saved.StatusCode, saved.AssignedDepartmentCode, saved.AssignedDepartmentName));
        Assert.Single(_db.ComplaintAssignments);
    }

    [Fact]
    public async Task Regional_user_cannot_assign_outside_region()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        var ro = FakeUser.AtRegion("E-RO", "RA", AppRoles.Maker);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Service(ro).AssignAsync(c.Id, new AssignComplaintRequest("E-B1", null, null), default));
    }

    [Fact]
    public async Task Closed_complaint_cannot_be_assigned()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "CLOSED");
        await Assert.ThrowsAsync<DomainException>(() =>
            Service(HoAdmin).AssignAsync(c.Id, new AssignComplaintRequest("E-A1", null, null), default));
    }

    [Fact]
    public async Task Remark_records_visibility_and_author_from_identity()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        var item = await Service(HoAdmin).AddRemarkAsync(c.Id, new AddRemarkRequest("Checked with switch", "internal"), default);

        Assert.Equal("INTERNAL", item.Visibility);
        var remark = _db.ComplaintRemarks.Single();
        Assert.Equal((RemarkVisibility.Internal, "E1001"), (remark.Visibility, remark.CreatedByEmployeeId));
    }

    [Fact]
    public async Task Detail_masks_identifiers_without_unmasked_permission()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        // An employee whose IAM access role maps to no portal role.
        var detail = await Service(new FakeUser("E0")).GetAsync(c.Id, default);

        Assert.True(detail.Customer.IsMasked);
        Assert.Equal("XXXXXX3210", detail.Customer.Mobile);
        Assert.Equal("XXXX XXXX 9012", detail.Customer.AccountNumber);
        Assert.Empty(detail.AllowedTransitions);
    }

    [Fact]
    public async Task Detail_lists_only_active_transitions_from_current_status()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status: "RESOLVED");
        var detail = await Service(HoAdmin).GetAsync(c.Id, default);
        Assert.Equal(["CLOSED"], detail.AllowedTransitions.Select(t => t.Code));
    }
}
