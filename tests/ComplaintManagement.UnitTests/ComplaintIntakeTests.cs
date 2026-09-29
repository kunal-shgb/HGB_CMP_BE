using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;
using FluentValidation;

namespace ComplaintManagement.UnitTests;

public class ComplaintIntakeTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FakeAudit _audit = new();

    public ComplaintIntakeTests() => _data = TestData.Seed(_db);

    private ComplaintIntakeService Service(FakeUser user) =>
        new(_db, user, _audit, _data.Org, new StaffCreateComplaintValidator(), SequentialNumbers.Registrar(_db, _data.Org));

    private static StaffCreateComplaintRequest Request(string? branch = null) => new()
    {
        CustomerName = "Walk-in Customer", Mobile = "9876543210", BranchCode = branch,
        CategoryCode = "UPI", Title = "ATM did not dispense cash", Description = "Cash not dispensed but account debited.",
    };

    [Fact]
    public void Every_employee_may_lodge()
    {
        Assert.Contains(Permissions.ComplaintCreate, Permissions.ForRoles([AppRoles.Viewer]));
    }

    [Fact]
    public async Task Branch_staff_lodge_for_their_own_branch_whatever_the_request_says()
    {
        var clerk = FakeUser.AtBranch("300005", "A1", AppRoles.Viewer);
        var result = await Service(clerk).LodgeAsync(Request(branch: "B1"), [], default);

        var c = _db.Complaints.Single();
        Assert.Equal(("A1", "RA"), (c.BranchCode, c.RegionCode));
        Assert.Equal(("ATM did not dispense cash", "Cash not dispensed but account debited."), (c.Title, c.Description));
        Assert.Equal((ComplaintSources.Branch, "300005", "A1"), (c.Source, c.LodgedByEmployeeId, c.LodgedByOfficeName));
        Assert.Equal("300005", c.StatusHistory.Single().ChangedByEmployeeId);
        Assert.True(result.CanOpen);
        Assert.Contains(_audit.Entries, e => e.Action == "LODGE" && e.RecordId == c.Id.ToString());
        Assert.Single(_db.Notifications); // the customer is told the complaint number
    }

    [Fact]
    public async Task Branch_form_has_the_branch_fixed()
    {
        var options = await Service(FakeUser.AtBranch("300005", "A1", AppRoles.Viewer)).GetFormOptionsAsync(default);
        Assert.Equal("A1", options.FixedBranch?.Code);
        Assert.Empty(options.Branches);
        Assert.Equal(ComplaintSources.Branch, options.Source);
    }

    [Fact]
    public async Task Regional_staff_must_choose_a_branch()
    {
        var ro = FakeUser.AtRegion("200003", "RA", AppRoles.Maker, AppRoles.Viewer);
        await Assert.ThrowsAsync<ValidationException>(() => Service(ro).LodgeAsync(Request(), [], default));

        var options = await Service(ro).GetFormOptionsAsync(default);
        Assert.Null(options.FixedBranch);
        Assert.Equal(3, options.Branches.Count);
    }

    [Theory]
    [InlineData("A2", true)]   // under the caller's RO
    [InlineData("B1", false)]  // another region: lodged, but outside what the caller can open
    public async Task Regional_staff_can_lodge_for_any_branch(string branch, bool canOpen)
    {
        var ro = FakeUser.AtRegion("200003", "RA", AppRoles.Viewer);
        var result = await Service(ro).LodgeAsync(Request(branch), [], default);

        var c = _db.Complaints.Single();
        Assert.Equal((branch, ComplaintSources.RegionalOffice), (c.BranchCode, c.Source));
        Assert.Equal(canOpen, result.CanOpen);
    }

    [Fact]
    public async Task Head_office_lodges_with_head_office_source()
    {
        var ho = new FakeUser("100001", AppRoles.Viewer);
        await Service(ho).LodgeAsync(Request("B1"), [], default);
        Assert.Equal(ComplaintSources.HeadOffice, _db.Complaints.Single().Source);
    }

    [Fact]
    public async Task Unrecognised_offices_cannot_lodge()
    {
        var odd = new FakeUser("999", AppRoles.Viewer) { OfficeType = "Training Centre" };
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(odd).LodgeAsync(Request("A1"), [], default));
        Assert.Empty(_db.Complaints);
    }
}
