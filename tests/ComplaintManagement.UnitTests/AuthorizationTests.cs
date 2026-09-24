using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;

namespace ComplaintManagement.UnitTests;

public class AuthorizationTests
{
    [Theory]
    [InlineData(ScopeLevel.HeadOffice, AppRoles.HoAdmin)]
    [InlineData(ScopeLevel.HeadOffice, AppRoles.Auditor)]
    [InlineData(ScopeLevel.Department, AppRoles.HoDepartmentUser)]
    [InlineData(ScopeLevel.Region, AppRoles.RegionalOfficeUser)]
    [InlineData(ScopeLevel.Branch, AppRoles.BranchUser)]
    [InlineData(ScopeLevel.Region, AppRoles.BranchUser, AppRoles.RegionalOfficeUser)]
    public void Scope_is_widest_granted_by_roles(ScopeLevel expected, params string[] roles) =>
        Assert.Equal(expected, AppRoles.ResolveScope(roles));

    [Fact]
    public void No_roles_means_branch_scope_and_no_permissions()
    {
        Assert.Equal(ScopeLevel.Branch, AppRoles.ResolveScope([]));
        Assert.Empty(Permissions.ForRoles([]));
    }

    [Fact]
    public void Auditor_is_read_only()
    {
        var p = Permissions.ForRoles([AppRoles.Auditor]);
        Assert.Contains(Permissions.ComplaintView, p);
        Assert.DoesNotContain(Permissions.ComplaintAssign, p);
        Assert.DoesNotContain(Permissions.ComplaintChangeStatus, p);
        Assert.DoesNotContain(Permissions.ComplaintAddRemark, p);
    }

    [Fact]
    public void Management_sees_masked_data_only() =>
        Assert.DoesNotContain(Permissions.ComplaintViewUnmasked, Permissions.ForRoles([AppRoles.Management]));

    [Fact]
    public void Only_super_admin_manages_configuration()
    {
        foreach (var role in AppRoles.All.Where(r => r != AppRoles.SuperAdmin))
            Assert.DoesNotContain(Permissions.AdminManage, Permissions.ForRoles([role]));
        Assert.Contains(Permissions.AdminManage, Permissions.ForRoles([AppRoles.SuperAdmin]));
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
        _data.AddComplaint(_db, _data.BranchA2, dept: _data.Dept);
        _data.AddComplaint(_db, _data.BranchB1);
        _data.AddComplaint(_db, _data.BranchB1, assignedTo: "E-BRANCH-A1");
    }

    private string[] Visible(ICurrentUser user) =>
        _db.Complaints.VisibleTo(user).Select(c => c.Branch!.Code).OrderBy(x => x).ToArray();

    [Fact]
    public void Head_office_sees_everything() =>
        Assert.Equal(4, Visible(new FakeUser("E1", AppRoles.HoAdmin)).Length);

    [Fact]
    public void Regional_user_sees_only_own_region() =>
        Assert.Equal(["A1", "A2"], Visible(new FakeUser("E2", AppRoles.RegionalOfficeUser) { RegionCode = "RA" }));

    [Fact]
    public void Branch_user_sees_own_branch_and_complaints_assigned_to_them() =>
        Assert.Equal(["A1", "B1"], Visible(new FakeUser("E-BRANCH-A1", AppRoles.BranchUser) { BranchCode = "A1" }));

    [Fact]
    public void Department_user_sees_department_complaints() =>
        Assert.Equal(["A2"], Visible(new FakeUser("E3", AppRoles.HoDepartmentUser) { DepartmentCode = "DBD" }));

    [Fact]
    public void Scoped_user_missing_org_claim_sees_nothing_unassigned() =>
        Assert.Empty(Visible(new FakeUser("E4", AppRoles.BranchUser)));

    [Fact]
    public void Assignment_targets_are_limited_to_callers_office()
    {
        var ro = new FakeUser("E2", AppRoles.RegionalOfficeUser) { RegionCode = "RA" };
        Assert.True(ComplaintScope.CanTarget(ro, new IamUser("X", "X", null, null, [], "RA", "A1", null)));
        Assert.False(ComplaintScope.CanTarget(ro, new IamUser("Y", "Y", null, null, [], "RB", "B1", null)));
        Assert.True(ComplaintScope.CanTarget(new FakeUser("E1", AppRoles.HoAdmin), new IamUser("Y", "Y", null, null, [], "RB", "B1", null)));
    }
}
