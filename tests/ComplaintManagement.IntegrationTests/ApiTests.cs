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
    public async Task Tampered_token_is_rejected()
    {
        var client = await factory.ClientForAsync("300001");
        client.DefaultRequestHeaders.Authorization = new("Bearer", client.DefaultRequestHeaders.Authorization!.Parameter + "x");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
    }

    [Theory]
    [InlineData("300001", "wrong-password")]
    [InlineData("999999", ApiFactory.DevPassword)]
    [InlineData("300099", ApiFactory.DevPassword)] // inactive
    public async Task Failed_sign_in_gets_one_generic_message(string code, string password)
    {
        var response = await factory.LoginAsync(code, password);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Invalid employee code or password.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("300001", "OFFICE_HEAD", "Branch", "BR-ROH-001")]
    [InlineData("300005", "VIEWER", "Branch", "BR-ROH-001")]
    [InlineData("200001", "CHECKER", "Region", "RO-ROH")]
    [InlineData("100001", "CHECKER", "HeadOffice", "0000")]
    [InlineData("900001", "ADMIN", "HeadOffice", "0000")]
    public async Task Access_role_and_office_come_from_the_IAM_profile(string code, string role, string scope, string officeCode)
    {
        var me = await (await factory.ClientForAsync(code)).GetFromJsonAsync<CurrentUserResponse>("/api/v1/me");
        Assert.NotNull(me);
        Assert.Contains(role, me.Roles);
        Assert.Contains("VIEWER", me.Roles); // every employee can view
        Assert.Equal(scope, me.ScopeLevel);
        Assert.Equal(officeCode, me.OfficeCode);
    }

    [Fact]
    public async Task Branch_user_only_sees_own_branch()
    {
        var page = await (await factory.ClientForAsync("300001")).GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=100");
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, i => Assert.True(i.Branch.Code == "BR-ROH-001" || i.AssignedTo?.EmployeeId == "300001"));
    }

    [Fact]
    public async Task Regional_office_sees_only_its_branches()
    {
        var page = await (await factory.ClientForAsync("200001")).GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=100");
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, i => Assert.Equal("RO-ROH", i.Region.Code));
    }

    [Fact]
    public async Task Branch_maker_escalates_to_RO_and_it_shows_in_the_filter()
    {
        var maker = await factory.ClientForAsync("300002");
        var id = (await maker.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?status=NEW&pageSize=1"))!.Items[0].Id;

        Assert.Equal(HttpStatusCode.NoContent, (await maker.PostAsJsonAsync($"/api/v1/complaints/{id}/escalate", new EscalateRequest("Customer waiting too long"))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await maker.PostAsJsonAsync($"/api/v1/complaints/{id}/escalate", new EscalateRequest("again"))).StatusCode);

        var ro = await factory.ClientForAsync("200001");
        var escalated = await ro.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?minEscalationLevel=2&pageSize=100");
        Assert.Contains(escalated!.Items, i => i.Id == id && i.EscalationLevel == 2);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await factory.ClientForAsync("900001")).PostAsJsonAsync($"/api/v1/complaints/{id}/escalate", new EscalateRequest("x"))).StatusCode);
    }

    [Fact]
    public async Task Branch_staff_act_only_on_complaints_their_office_head_assigns_them()
    {
        var head = await factory.ClientForAsync("300001");
        var clerk = await factory.ClientForAsync("300005");
        var id = (await head.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?status=UNDER_PROCESS&branchCode=BR-ROH-001&pageSize=1"))!.Items[0].Id;

        // View only: masked, and every action refused.
        var seen = await clerk.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{id}");
        Assert.True(seen!.Customer.IsMasked);
        Assert.False(seen.Abilities.AddRemark);
        Assert.Equal(HttpStatusCode.Forbidden, (await clerk.PostAsJsonAsync($"/api/v1/complaints/{id}/remarks", new AddRemarkRequest("x", "INTERNAL"))).StatusCode);

        // The office head sees the clerk in the assignable list and assigns the complaint.
        var staff = await head.GetFromJsonAsync<List<EmployeeResponse>>("/api/v1/employees");
        Assert.Contains(staff!, e => e.EmployeeId == "300005");
        Assert.Equal(HttpStatusCode.NoContent, (await head.PostAsJsonAsync($"/api/v1/complaints/{id}/assign", new AssignComplaintRequest("300005", null, "Please handle"))).StatusCode);

        var assigned = await clerk.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{id}");
        Assert.True(assigned!.Abilities is { AddRemark: true, ChangeStatus: true, IsAssignedToMe: true, Assign: false });
        Assert.False(assigned.Customer.IsMasked);
        Assert.Equal(HttpStatusCode.OK, (await clerk.PostAsJsonAsync($"/api/v1/complaints/{id}/remarks", new AddRemarkRequest("Called the customer", "INTERNAL"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clerk.PostAsJsonAsync($"/api/v1/complaints/{id}/assign", new AssignComplaintRequest("300001", null, null))).StatusCode);
    }

    [Fact]
    public async Task RO_checker_assigns_only_at_branches_without_an_office_head()
    {
        var checker = await factory.ClientForAsync("200001");
        async Task<Guid> OpenAt(string branch) =>
            (await checker.GetFromJsonAsync<PagedResponse<ComplaintListItem>>($"/api/v1/complaints?branchCode={branch}&status=RECEIVED&pageSize=1"))!.Items[0].Id;

        // Rohtak Main has an OfficeHead (300001).
        var withHead = await OpenAt("BR-ROH-001");
        Assert.False((await checker.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{withHead}"))!.Abilities.Assign);
        Assert.Equal(HttpStatusCode.Forbidden, (await checker.PostAsJsonAsync($"/api/v1/complaints/{withHead}/assign", new AssignComplaintRequest("200003", null, null))).StatusCode);

        // Meham has no OfficeHead, so the RO Checker steps in.
        var noHead = await OpenAt("BR-ROH-003");
        Assert.True((await checker.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{noHead}"))!.Abilities.Assign);
        Assert.Equal(HttpStatusCode.NoContent, (await checker.PostAsJsonAsync($"/api/v1/complaints/{noHead}/assign", new AssignComplaintRequest("200003", null, "Branch has no head"))).StatusCode);
    }

    [Fact]
    public async Task Checker_cannot_change_status_directly()
    {
        var checker = await factory.ClientForAsync("200001");
        var page = await checker.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=1");
        var response = await checker.PostAsJsonAsync($"/api/v1/complaints/{page!.Items[0].Id}/status", new ChangeStatusRequest("RECEIVED", null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Branch_maker_resolution_is_approved_by_the_RO_checker()
    {
        var maker = await factory.ClientForAsync("300001");
        var checker = await factory.ClientForAsync("200001");
        var complaint = await RegisterAsync("BR-ROH-001");

        // Move the new complaint along until it can be resolved.
        foreach (var step in new[] { "RECEIVED", "UNDER_PROCESS" })
            (await maker.PostAsJsonAsync($"/api/v1/complaints/{complaint}/status", new ChangeStatusRequest(step, null))).EnsureSuccessStatusCode();

        var request = await maker.PostAsJsonAsync($"/api/v1/complaints/{complaint}/status", new ChangeStatusRequest("RESOLVED", "Refund processed"));
        var result = await request.Content.ReadFromJsonAsync<ChangeStatusResponse>();
        Assert.True(result!.PendingApproval);

        var queue = await checker.GetFromJsonAsync<List<ApprovalListItem>>("/api/v1/approvals");
        var approval = Assert.Single(queue!, a => a.ComplaintId == complaint);

        Assert.Equal(HttpStatusCode.NoContent, (await checker.PostAsJsonAsync($"/api/v1/approvals/{approval.ApprovalId}/approve", new DecideApprovalRequest("Verified"))).StatusCode);
        var detail = await maker.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{complaint}");
        Assert.Equal("RESOLVED", detail!.Status.Code);
        Assert.Null(detail.PendingApproval);
    }

    [Fact]
    public async Task Only_admin_reaches_configuration()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await (await factory.ClientForAsync("300001")).GetAsync("/api/v1/admin/workflow")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await factory.ClientForAsync("200001")).GetAsync("/api/v1/admin/categories")).StatusCode);

        var workflow = await (await factory.ClientForAsync("900001")).GetFromJsonAsync<AdminWorkflow>("/api/v1/admin/workflow");
        Assert.Contains(workflow!.Transitions, t => t.ToStatusCode == "RESOLVED" && t.RequiresApproval);
        Assert.Equal("CSD", workflow.Approvals.HeadOfficeMakerCheckerDepartment);
    }

    [Fact]
    public async Task Concurrent_public_registrations_get_unique_numbers()
    {
        var client = factory.CreateClient();
        // Stays under the public rate limit of 10 per minute per client across this class's public calls.
        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PostAsJsonAsync("/api/v1/public/complaints", NewComplaint("BR-ROH-001"))));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        var numbers = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<CreateComplaintResponse>())!.ComplaintNumber));
        Assert.Equal(numbers.Length, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.True(ComplaintNumber.IsValid(n)));
    }

    [Fact]
    public async Task Customer_tracks_a_complaint_with_a_one_time_code()
    {
        var anon = factory.CreateClient();
        var created = await (await anon.PostAsJsonAsync("/api/v1/public/complaints", NewComplaint("BR-ROH-002")))
            .Content.ReadFromJsonAsync<CreateComplaintResponse>();

        var otp = await (await anon.PostAsJsonAsync("/api/v1/public/tracking/otp", new TrackingOtpRequest(created!.ComplaintNumber, "9876543210")))
            .Content.ReadFromJsonAsync<TrackingOtpResponse>();
        Assert.NotNull(otp!.DevelopmentOtp); // Development only

        var bad = await anon.PostAsJsonAsync("/api/v1/public/tracking/verify", new TrackingVerifyRequest(created.ComplaintNumber, "9876543210", "000000"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        var view = await (await anon.PostAsJsonAsync("/api/v1/public/tracking/verify", new TrackingVerifyRequest(created.ComplaintNumber, "9876543210", otp.DevelopmentOtp!)))
            .Content.ReadFromJsonAsync<TrackingView>();
        Assert.Equal(("Registered", "Sampla"), (view!.Status, view.Branch));
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

    [Fact]
    public async Task Organisation_data_comes_from_the_IAM()
    {
        var client = await factory.ClientForAsync("100001");
        var branches = await client.GetFromJsonAsync<List<BranchResponse>>("/api/v1/branches?regionCode=RO-ROH");
        Assert.Equal(["BR-ROH-003", "BR-ROH-001", "BR-ROH-002"], branches!.Select(b => b.Code)); // ordered by name
        var departments = await client.GetFromJsonAsync<List<DepartmentResponse>>("/api/v1/departments");
        Assert.Contains(departments!, d => d.Code == "DBD");
    }

    [Fact]
    public async Task Public_form_options_are_anonymous_and_omit_internal_config()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/public/complaints/form-options");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BR-ROH-001", body);
        Assert.DoesNotContain("tatDays", body);
    }

    private static readonly byte[] PdfBytes = [.. "%PDF-1.7\n"u8.ToArray(), .. new byte[64]];
    private static readonly byte[] ExeBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0, 0, 0, 0];

    private static MultipartFormDataContent Documents(PublicCreateComplaintRequest? complaint, params (string Name, byte[] Bytes)[] files)
    {
        var form = new MultipartFormDataContent();
        if (complaint is not null) form.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(complaint)), "complaint");
        foreach (var (name, bytes) in files)
        {
            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new("application/octet-stream");
            form.Add(part, "files", name);
        }
        return form;
    }

    [Fact]
    public async Task Customer_documents_are_stored_and_only_full_access_staff_can_download()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/public/complaints",
            Documents(NewComplaint("BR-ROH-001"), ("../bank statement.pdf", PdfBytes)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var number = (await response.Content.ReadFromJsonAsync<CreateComplaintResponse>())!.ComplaintNumber;

        var maker = await factory.ClientForAsync("300001");
        var id = (await maker.GetFromJsonAsync<PagedResponse<ComplaintListItem>>($"/api/v1/complaints?complaintNumber={number}"))!.Items.Single().Id;
        var detail = await maker.GetFromJsonAsync<ComplaintDetail>($"/api/v1/complaints/{id}");
        var doc = Assert.Single(detail!.Attachments);
        Assert.Equal(("bank statement.pdf", "application/pdf", "CUSTOMER", "NOT_SCANNED"), (doc.FileName, doc.ContentType, doc.UploadedBy.EmployeeId, doc.ScanStatus));

        var download = await maker.GetAsync($"/api/v1/complaints/{id}/attachments/{doc.Id}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(PdfBytes, await download.Content.ReadAsByteArrayAsync());

        var messages = await maker.GetFromJsonAsync<List<NotificationItem>>($"/api/v1/complaints/{id}/notifications");
        var registered = Assert.Single(messages!);
        Assert.Equal(("REGISTERED", "SMS", "XXXXXX3210"), (registered.Event, registered.Channel, registered.RecipientMasked));

        // Admin sees complaints masked and may not open customer documents.
        var admin = await factory.ClientForAsync("900001");
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync($"/api/v1/complaints/{id}/attachments/{doc.Id}")).StatusCode);
        // Another branch cannot even see the complaint.
        var otherBranch = await factory.ClientForAsync("300003");
        Assert.Equal(HttpStatusCode.NotFound, (await otherBranch.GetAsync($"/api/v1/complaints/{id}/attachments/{doc.Id}")).StatusCode);
    }

    [Fact]
    public async Task Disguised_files_are_refused_for_customers_and_staff()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/public/complaints",
            Documents(NewComplaint("BR-ROH-001"), ("invoice.pdf", ExeBytes)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var maker = await factory.ClientForAsync("300001");
        var id = (await maker.GetFromJsonAsync<PagedResponse<ComplaintListItem>>("/api/v1/complaints?pageSize=1"))!.Items[0].Id;
        var bad = await maker.PostAsync($"/api/v1/complaints/{id}/attachments", Documents(null, ("photo.png", ExeBytes)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        var good = await maker.PostAsync($"/api/v1/complaints/{id}/attachments", Documents(null, ("note.pdf", PdfBytes)));
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var items = await good.Content.ReadFromJsonAsync<List<AttachmentItem>>();
        Assert.Equal("300001", Assert.Single(items!).UploadedBy.EmployeeId);
    }

    private static PublicCreateComplaintRequest NewComplaint(string branch) => new()
    {
        CustomerName = "Integration Test",
        Mobile = "9876543210",
        BranchCode = branch,
        CategoryCode = "UPI",
        SubCategoryCode = "FAILED_TRANSACTION",
        Description = "Amount debited but transaction failed.",
    };

    private async Task<Guid> RegisterAsync(string branch)
    {
        var created = await (await factory.CreateClient().PostAsJsonAsync("/api/v1/public/complaints", NewComplaint(branch)))
            .Content.ReadFromJsonAsync<CreateComplaintResponse>();
        var hq = await factory.ClientForAsync("100001");
        var page = await hq.GetFromJsonAsync<PagedResponse<ComplaintListItem>>($"/api/v1/complaints?complaintNumber={created!.ComplaintNumber}");
        return page!.Items.Single().Id;
    }
}
