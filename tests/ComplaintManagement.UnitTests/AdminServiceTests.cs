using ComplaintManagement.Application.Admin;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;

namespace ComplaintManagement.UnitTests;

public class AdminServiceTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly FakeAudit _audit = new();
    private readonly AdminService _admin;

    public AdminServiceTests()
    {
        var data = TestData.Seed(_db);
        _admin = new AdminService(_db, data.Org, new FakeUser("900001", AppRoles.Admin), _audit, new FixedClock(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task New_category_gets_normalised_code_and_a_general_subcategory()
    {
        await _admin.CreateCategoryAsync(new CreateCategoryRequest("kcc", "Kisan Credit Card", "Banking Services", null, null), default);
        var cat = Assert.Single(await _admin.GetCategoriesAsync(default), c => c.Code == "KCC");
        Assert.Equal(["GENERAL"], cat.SubCategories.Select(s => s.Code));
        Assert.Contains(_audit.Entries, e => e.Action == "CREATE_CATEGORY");
    }

    [Theory]
    [InlineData("UPI")]   // duplicate
    [InlineData("a b")]   // invalid characters
    [InlineData("X")]     // too short
    public async Task Bad_or_duplicate_category_codes_are_rejected(string code) =>
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateCategoryAsync(new CreateCategoryRequest(code, "Name", "Group", null, null), default));

    [Fact]
    public async Task Subcategory_TAT_priority_and_department_are_validated()
    {
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateSubCategoryAsync("UPI", "GENERAL", new UpdateSubCategoryRequest("General", 0, null, null, 1, true), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateSubCategoryAsync("UPI", "GENERAL", new UpdateSubCategoryRequest("General", 7, "URGENT", null, 1, true), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateSubCategoryAsync("UPI", "GENERAL", new UpdateSubCategoryRequest("General", 7, null, "NOPE", 1, true), default));

        await _admin.UpdateSubCategoryAsync("UPI", "GENERAL", new UpdateSubCategoryRequest("General", 7, "MEDIUM", "DBD", 1, true), default);
        var sub = _db.SubCategories.Single(s => s.Code == "GENERAL");
        Assert.Equal((7, "MEDIUM"), (sub.TatDays, sub.DefaultPriorityCode));
        Assert.Equal("DBD", sub.DefaultDepartmentCode);
    }

    [Fact]
    public async Task Transitions_cannot_touch_the_approval_status_or_loop()
    {
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateTransitionAsync(new CreateTransitionRequest("NEW", "PENDING_APPROVAL", false, false), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateTransitionAsync(new CreateTransitionRequest("NEW", "NEW", false, false), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateTransitionAsync(new CreateTransitionRequest("NEW", "ASSIGNED", false, false), default)); // exists
    }

    [Fact]
    public async Task Approval_required_moves_always_require_a_remark()
    {
        await _admin.CreateTransitionAsync(new CreateTransitionRequest("ASSIGNED", "CLOSED", false, true), default);
        var t = _db.StatusTransitions.Single(x => x.FromStatusCode == "ASSIGNED" && x.ToStatusCode == "CLOSED");
        Assert.True(t.RequiresApproval && t.RequiresRemark);

        await _admin.UpdateTransitionAsync(t.Id, new UpdateTransitionRequest(false, true, true), default);
        Assert.True(_db.StatusTransitions.Single(x => x.Id == t.Id).RequiresRemark);
    }

    [Fact]
    public async Task HO_checker_department_must_exist_and_can_be_cleared()
    {
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateApprovalSettingsAsync(new UpdateApprovalSettingsRequest("NOPE"), default));

        await _admin.UpdateApprovalSettingsAsync(new UpdateApprovalSettingsRequest("DBD"), default);
        Assert.Equal("DBD", _db.AppSettings.Single().Value);
        Assert.Equal("900001", _db.AppSettings.Single().UpdatedBy);

        await _admin.UpdateApprovalSettingsAsync(new UpdateApprovalSettingsRequest(" "), default);
        Assert.Null((await _admin.GetWorkflowAsync(default)).Approvals.HeadOfficeMakerCheckerDepartment);
    }
}
