using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;

namespace ComplaintManagement.UnitTests;

public class PublicRegistrationTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;

    public PublicRegistrationTests() => _data = TestData.Seed(_db);

    private PublicComplaintService Service(IIamOrganisationService org) =>
        new(_db, org, new FixedClock(DateTimeOffset.UtcNow), new PublicCreateComplaintValidator(), SequentialNumbers.Registrar(_db, org));

    private static PublicCreateComplaintRequest Request(string branch) => new()
    {
        CustomerName = "Test", Mobile = "9876543210", BranchCode = branch,
        CategoryCode = "UPI", Title = "UPI payment failed", Description = "Amount debited but transaction failed.",
    };

    [Fact]
    public async Task Registration_records_the_branch_and_RO_from_the_IAM()
    {
        await Service(_data.Org).RegisterAsync(Request("b1"), [], null, default);
        var c = _db.Complaints.Single();
        Assert.Equal(("B1", "Branch B1", "RB", "Region B"), (c.BranchCode, c.BranchName, c.RegionCode, c.RegionName));
        Assert.Equal((ComplaintSources.Website, null), (c.Source, c.LodgedByEmployeeId));
    }

    [Fact]
    public async Task Unknown_or_inactive_branches_are_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => Service(_data.Org).RegisterAsync(Request("ZZ"), [], null, default));

        var closed = new FakeOrg([_data.BranchA1 with { IsActive = false }], []);
        await Assert.ThrowsAsync<DomainException>(() => Service(closed).RegisterAsync(Request("A1"), [], null, default));
        Assert.Empty(_db.Complaints);
    }
}
