using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
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

    private static readonly IamUser BranchA1Officer = new("E-A1", "Officer A1", null, null, [], "RA", "A1", null);
    private static readonly IamUser BranchB1Officer = new("E-B1", "Officer B1", null, null, [], "RB", "B1", null);

    public ComplaintWorkflowTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(ICurrentUser user) => new(
        _db, user, _audit, new FakeIam(BranchA1Officer, BranchB1Officer), _clock, Options.Create(new SlaOptions()),
        new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator());

    private static FakeUser HoAdmin => new("E1001", AppRoles.HoAdmin);

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
        var svc = Service(HoAdmin);

        await svc.ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Amount reversed"), default);
        Assert.Equal(_clock.Now, _db.Complaints.Single().ResolvedAt);

        await svc.ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default);
        Assert.Equal(_clock.Now, _db.Complaints.Single().ClosedAt);

        await svc.ChangeStatusAsync(c.Id, new ChangeStatusRequest("REOPENED", "Customer disputes"), default);
        var reopened = _db.Complaints.Single();
        Assert.Null(reopened.ClosedAt);
        Assert.Null(reopened.ResolvedAt);
    }

    [Fact]
    public async Task Out_of_scope_complaint_is_not_found()
    {
        var c = _data.AddComplaint(_db, _data.BranchB1, status: "ASSIGNED");
        var branchUser = new FakeUser("E-A1", AppRoles.BranchUser) { BranchCode = "A1", RegionCode = "RA" };
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service(branchUser).ChangeStatusAsync(c.Id, new ChangeStatusRequest("UNDER_PROCESS", null), default));
    }

    [Fact]
    public async Task Assign_records_assignment_and_moves_to_assigned_status()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        await Service(HoAdmin).AssignAsync(c.Id, new AssignComplaintRequest("E-A1", "DBD", "Please resolve"), default);

        var saved = _db.Complaints.Single();
        Assert.Equal(("E-A1", "ASSIGNED", _data.Dept.Id), (saved.AssignedEmployeeId, saved.StatusCode, saved.AssignedDepartmentId));
        Assert.Single(_db.ComplaintAssignments);
    }

    [Fact]
    public async Task Regional_user_cannot_assign_outside_region()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        var ro = new FakeUser("E-RO", AppRoles.RegionalOfficeUser) { RegionCode = "RA" };
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
    public async Task Detail_masks_identifiers_for_roles_without_unmasked_permission()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1);
        var detail = await Service(new FakeUser("E5001", AppRoles.Management)).GetAsync(c.Id, default);

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
