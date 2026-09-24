using System.Net;
using System.Net.Http.Json;
using ComplaintManagement.Contracts;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.ValueObjects;

namespace ComplaintManagement.IntegrationTests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Staff_endpoints_reject_anonymous_callers()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/complaints");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_dev_identity_is_rejected()
    {
        var response = await factory.ClientFor("NOBODY").GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_mapped_roles_and_scope()
    {
        var me = await factory.ClientFor("E2001").GetFromJsonAsync<CurrentUserResponse>("/api/v1/me");
        Assert.NotNull(me);
        Assert.Equal(["REGIONAL_OFFICE_USER"], me.Roles);
        Assert.Equal("Region", me.ScopeLevel);
        Assert.Equal("RO-ROH", me.RegionCode);
    }

    [Fact]
    public async Task Branch_user_only_sees_own_branch()
    {
        var page = await factory.ClientFor("E3001").GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=100");
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, i => Assert.True(i.Branch.Code == "BR-ROH-001" || i.AssignedTo?.EmployeeId == "E3001"));
        Assert.All(page.Items, i => Assert.StartsWith("XXXXXX", i.MobileMasked));
    }

    [Fact]
    public async Task Read_only_role_cannot_change_status()
    {
        var auditor = factory.ClientFor("E6001");
        var page = await auditor.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=1");
        var response = await auditor.PostAsJsonAsync($"/api/v1/complaints/{page!.Items[0].Id}/status", new ChangeStatusRequest("RECEIVED", null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_public_registrations_get_unique_numbers()
    {
        var client = factory.CreateClient();
        var request = new PublicCreateComplaintRequest
        {
            CustomerName = "Integration Test",
            Mobile = "9876543210",
            BranchCode = "BR-ROH-001",
            CategoryCode = "UPI",
            SubCategoryCode = "FAILED_TRANSACTION",
            Description = "Amount debited but transaction failed.",
        };

        // Stays under the public rate limit of 10 per minute per client.
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync("/api/v1/public/complaints", request)));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        var numbers = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<CreateComplaintResponse>())!.ComplaintNumber));
        Assert.Equal(numbers.Length, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.True(ComplaintNumber.IsValid(n)));
    }

    [Fact]
    public async Task Public_registration_validates_input()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/public/complaints", new PublicCreateComplaintRequest
        {
            CustomerName = "", Mobile = "12345", BranchCode = "BR-ROH-001", CategoryCode = "UPI", SubCategoryCode = "GENERAL", Description = "short",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
