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
    private readonly TestData _data;

    public AdminServiceTests()
    {
        var data = _data = TestData.Seed(_db);
        _admin = new AdminService(_db, data.Org, new FakeUser("900001", AppRoles.Admin), _audit, new FixedClock(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task New_category_gets_normalised_code_and_its_TAT()
    {
        await _admin.CreateCategoryAsync(new CreateCategoryRequest("kcc", "Kisan Credit Card", "DIGITAL_BANKING", 10, null, null, null, null), default);
        var cat = Assert.Single((await _admin.GetCategoriesAsync(default)).Categories, c => c.Code == "KCC");
        Assert.Equal(("DIGITAL_BANKING", "Digital Banking"), (cat.GroupCode, cat.Group));
        Assert.Equal(10, cat.TatDays);
        Assert.Contains(_audit.Entries, e => e.Action == "CREATE_CATEGORY");
    }

    [Theory]
    [InlineData("UPI")]   // duplicate
    [InlineData("a b")]   // invalid characters
    [InlineData("X")]     // too short
    public async Task Bad_or_duplicate_category_codes_are_rejected(string code) =>
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateCategoryAsync(new CreateCategoryRequest(code, "Name", "DIGITAL_BANKING", null, null, null, null, null), default));

    [Fact]
    public async Task Categories_must_belong_to_an_existing_group()
    {
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateCategoryAsync(new CreateCategoryRequest("KCC", "KCC", "NOPE", null, null, null, null, null), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateCategoryAsync("UPI", new UpdateCategoryRequest("UPI", "", 7, null, null, 10, true, null), default));
    }

    [Fact]
    public async Task Groups_can_be_added_renamed_and_categories_moved_between_them()
    {
        await _admin.CreateGroupAsync(new CreateCategoryGroupRequest("agri", "Agri", null), default);
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateGroupAsync(new CreateCategoryGroupRequest("AGRI", "Other name", null), default));      // code taken
        await Assert.ThrowsAsync<DomainException>(() => _admin.CreateGroupAsync(new CreateCategoryGroupRequest("AGRI2", "digital banking", null), default)); // name taken

        await _admin.UpdateGroupAsync("AGRI", new UpdateCategoryGroupRequest("Agriculture & Rural", 5, true), default);
        await _admin.UpdateCategoryAsync("UPI", new UpdateCategoryRequest("UPI", "AGRI", 7, null, null, 10, true, null), default);

        var catalogue = await _admin.GetCategoriesAsync(default);
        Assert.Equal("Agriculture & Rural", catalogue.Groups[0].Name); // sorted first
        Assert.Equal(("AGRI", "Agriculture & Rural"), (catalogue.Categories[0].GroupCode, catalogue.Categories[0].Group));
        Assert.Equal([1, 0], catalogue.Groups.Select(g => g.CategoryCount));
    }

    [Fact]
    public async Task Hiding_a_group_hides_its_categories_from_forms()
    {
        await _admin.UpdateGroupAsync("DIGITAL_BANKING", new UpdateCategoryGroupRequest("Digital Banking", 10, false), default);
        Assert.Empty(await new ComplaintManagement.Application.Reference.ReferenceService(_db, _data.Org).CategoriesAsync(default));
    }

    [Fact]
    public async Task A_group_is_deleted_only_once_it_is_empty()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _admin.DeleteGroupAsync("DIGITAL_BANKING", default));
        Assert.Contains("1 category", ex.Message);

        await _admin.CreateGroupAsync(new CreateCategoryGroupRequest("EMPTY", "Empty", null), default);
        await _admin.DeleteGroupAsync("EMPTY", default);
        Assert.DoesNotContain(_db.CategoryGroups, g => g.Code == "EMPTY");
        Assert.Contains(_audit.Entries, e => e.Action == "DELETE_CATEGORY_GROUP");
    }

    [Fact]
    public async Task Categories_in_use_cannot_be_deleted()
    {
        _data.AddComplaint(_db, _data.BranchA1, "NEW");
        Assert.Contains("Hide it instead", (await Assert.ThrowsAsync<DomainException>(() => _admin.DeleteCategoryAsync("UPI", default))).Message);
        Assert.Equal(1, (await _admin.GetCategoriesAsync(default)).Categories.Single().ComplaintCount);
    }

    [Fact]
    public async Task Unused_categories_are_deleted()
    {
        await _admin.CreateCategoryAsync(new CreateCategoryRequest("KCC", "Kisan Credit Card", "DIGITAL_BANKING", null, null, null, null, null), default);
        await _admin.DeleteCategoryAsync("KCC", default);
        Assert.DoesNotContain(_db.Categories, c => c.Code == "KCC");
        Assert.Contains(_audit.Entries, e => e.Action == "DELETE_CATEGORY");
    }

    [Fact]
    public async Task Category_TAT_priority_and_department_are_validated()
    {
        UpdateCategoryRequest Update(int? tat, string? priority, string? dept) => new("UPI", "DIGITAL_BANKING", tat, priority, dept, 10, true, null);
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateCategoryAsync("UPI", Update(0, null, null), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateCategoryAsync("UPI", Update(7, "URGENT", null), default));
        await Assert.ThrowsAsync<DomainException>(() => _admin.UpdateCategoryAsync("UPI", Update(7, null, "NOPE"), default));

        await _admin.UpdateCategoryAsync("UPI", Update(7, "MEDIUM", "DBD"), default);
        var cat = _db.Categories.Single(c => c.Code == "UPI");
        Assert.Equal((7, "MEDIUM", "DBD"), (cat.TatDays, cat.DefaultPriorityCode, cat.DefaultDepartmentCode));
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
