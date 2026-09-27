using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;

namespace ComplaintManagement.UnitTests;

public class AuthorizationTests
{
    [Theory]
    [InlineData("Head Office", ScopeLevel.HeadOffice)]
    [InlineData("head office", ScopeLevel.HeadOffice)]
    [InlineData("Regional Office", ScopeLevel.Region)]
    [InlineData(" Branch ", ScopeLevel.Branch)]
    public void Office_type_sets_scope(string officeType, ScopeLevel expected) =>
        Assert.Equal(expected, new OfficeScopeOptions().Resolve(officeType));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Zonal Office")]
    public void Unknown_office_type_gets_no_scope(string? officeType) =>
        Assert.Null(new OfficeScopeOptions().Resolve(officeType));

    private static readonly RoleMapping[] Mappings =
    [
        new("Maker", null, AppRoles.Maker),
        new("Checker", null, AppRoles.Checker),
        new("Admin", null, AppRoles.Admin),
    ];

    [Theory]
    [InlineData("Maker", "Branch", AppRoles.Maker)]
    [InlineData("maker", "Regional Office", AppRoles.Maker)]
    [InlineData("Checker", "Regional Office", AppRoles.Checker)]
    [InlineData("Checker", "Head Office", AppRoles.Checker)]
    [InlineData("Admin", "Head Office", AppRoles.Admin)]
    public void Access_role_maps_to_app_role(string accessRole, string officeType, string expected) =>
        Assert.Equal([expected], RoleMappingService.Resolve(Mappings, [accessRole], officeType));

    [Fact]
    public void Office_specific_mapping_applies_only_to_that_office()
    {
        RoleMapping[] mappings = [new("Maker", "Branch", "X")];
        Assert.Equal(["X"], RoleMappingService.Resolve(mappings, ["Maker"], "Branch"));
        Assert.Empty(RoleMappingService.Resolve(mappings, ["Maker"], "Head Office"));
    }

    [Fact]
    public void Unmapped_access_role_gets_no_roles() =>
        Assert.Empty(RoleMappingService.Resolve(Mappings, ["Auditor"], "Head Office"));

    [Fact]
    public void Maker_works_complaints_but_cannot_approve()
    {
        var p = Permissions.ForRoles([AppRoles.Maker]);
        Assert.Contains(Permissions.ComplaintChangeStatus, p);
        Assert.Contains(Permissions.ComplaintAssign, p);
        Assert.Contains(Permissions.ComplaintAddRemark, p);
        Assert.DoesNotContain(Permissions.ComplaintApprove, p);
    }

    [Fact]
    public void Checker_approves_and_assigns_but_does_not_change_status()
    {
        var p = Permissions.ForRoles([AppRoles.Checker]);
        Assert.Contains(Permissions.ComplaintApprove, p);
        Assert.Contains(Permissions.ComplaintAssign, p);
        Assert.DoesNotContain(Permissions.ComplaintChangeStatus, p);
    }

    [Fact]
    public void Only_admin_manages_configuration_and_admin_does_not_work_complaints()
    {
        Assert.Contains(Permissions.AdminManage, Permissions.ForRoles([AppRoles.Admin]));
        Assert.DoesNotContain(Permissions.AdminManage, Permissions.ForRoles([AppRoles.Maker, AppRoles.Checker]));
        var admin = Permissions.ForRoles([AppRoles.Admin]);
        Assert.DoesNotContain(Permissions.ComplaintChangeStatus, admin);
        Assert.DoesNotContain(Permissions.ComplaintApprove, admin);
        Assert.DoesNotContain(Permissions.ComplaintViewUnmasked, admin);
    }

    [Fact]
    public void Unknown_roles_grant_nothing() => Assert.Empty(Permissions.ForRoles(["SOMETHING_ELSE"]));
}

public class ComplaintScopeTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;

    public ComplaintScopeTests()
    {
        _data = TestData.Seed(_db);
        _data.AddComplaint(_db, _data.BranchA1);
        _data.AddComplaint(_db, _data.BranchA2);
        _data.AddComplaint(_db, _data.BranchB1);
        _data.AddComplaint(_db, _data.BranchB1, assignedTo: "E-A1");
    }

    private string[] Visible(ICurrentUser user) =>
        _db.Complaints.VisibleTo(user).Select(c => c.BranchCode).OrderBy(x => x).ToArray();

    [Fact]
    public void Head_office_sees_everything() =>
        Assert.Equal(4, Visible(new FakeUser("E1", AppRoles.Maker)).Length);

    [Fact]
    public void Regional_office_sees_all_branches_under_it() =>
        Assert.Equal(["A1", "A2"], Visible(FakeUser.AtRegion("E2", "RA", AppRoles.Maker)));

    [Fact]
    public void Branch_sees_own_branch_and_complaints_assigned_to_the_user() =>
        Assert.Equal(["A1", "B1"], Visible(FakeUser.AtBranch("E-A1", "A1", AppRoles.Maker)));

    [Fact]
    public void Another_branch_user_sees_only_their_branch() =>
        Assert.Equal(["A2"], Visible(FakeUser.AtBranch("E-A2", "A2", AppRoles.Maker)));

    [Fact]
    public void Scope_ignores_roles_and_follows_office() =>
        // A Checker at a branch still sees only that branch.
        Assert.Equal(["A2"], Visible(FakeUser.AtBranch("E-X", "A2", AppRoles.Checker)));

    [Fact]
    public void Unknown_office_type_sees_only_assigned_complaints()
    {
        Assert.Empty(Visible(new FakeUser("E-Z", AppRoles.Maker) { OfficeType = "Zonal Office", OfficeCode = "A1" }));
        Assert.Equal(["B1"], Visible(new FakeUser("E-A1", AppRoles.Maker) { OfficeType = null }));
    }

    [Fact]
    public async Task Assignment_targets_follow_office_hierarchy()
    {
        var ro = FakeUser.AtRegion("E2", "RA", AppRoles.Maker);
        Assert.True(await ComplaintScope.CanTargetAsync(_data.Org, ro, Staff.At("X", "Branch", "A1"), default));
        Assert.True(await ComplaintScope.CanTargetAsync(_data.Org, ro, Staff.At("Y", "Regional Office", "RA"), default));
        Assert.False(await ComplaintScope.CanTargetAsync(_data.Org, ro, Staff.At("Z", "Branch", "B1"), default));

        var branch = FakeUser.AtBranch("E3", "A1", AppRoles.Maker);
        Assert.True(await ComplaintScope.CanTargetAsync(_data.Org, branch, Staff.At("X", "Branch", "A1"), default));
        Assert.False(await ComplaintScope.CanTargetAsync(_data.Org, branch, Staff.At("W", "Branch", "A2"), default));

        var ho = new FakeUser("E1", AppRoles.Maker);
        Assert.True(await ComplaintScope.CanTargetAsync(_data.Org, ho, Staff.At("Z", "Branch", "B1"), default));
        Assert.False(await ComplaintScope.CanTargetAsync(_data.Org, ho, Staff.At("Q", "Branch", "B1", active: false), default));
    }

    [Fact]
    public async Task Assignable_list_is_filtered_to_office()
    {
        var ro = FakeUser.AtRegion("E2", "RA", AppRoles.Maker);
        var result = await ComplaintScope.AssignableAsync(_data.Org, ro,
            [Staff.At("1", "Branch", "A1"), Staff.At("2", "Branch", "B1"), Staff.At("3", "Regional Office", "RA"), Staff.At("4", "Branch", "A2", active: false)], default);
        Assert.Equal(["1", "3"], result.Select(u => u.EmployeeCode));
    }
}
