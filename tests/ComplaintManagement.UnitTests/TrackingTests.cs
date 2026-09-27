using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class TrackingTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 27, 6, 0, 0, TimeSpan.Zero));
    private readonly Complaint _complaint;

    public TrackingTests()
    {
        _data = TestData.Seed(_db);
        _complaint = _data.AddComplaint(_db, _data.BranchA1, "UNDER_PROCESS");
        _db.ComplaintStatusHistory.Add(new ComplaintStatusHistory { ComplaintId = _complaint.Id, NewStatusCode = "NEW", ChangedByEmployeeId = "CUSTOMER", ChangedAt = _clock.Now.AddDays(-3) });
        _db.ComplaintStatusHistory.Add(new ComplaintStatusHistory { ComplaintId = _complaint.Id, OldStatusCode = "NEW", NewStatusCode = "UNDER_PROCESS", ChangedByEmployeeId = "300001", ChangedByName = "RAKESH", ChangedAt = _clock.Now.AddDays(-2) });
        _db.ComplaintRemarks.Add(new ComplaintRemark { ComplaintId = _complaint.Id, Remark = "Switch team says it is a fraud case", Visibility = RemarkVisibility.Internal, CreatedByEmployeeId = "300001", CreatedAt = _clock.Now.AddDays(-1) });
        _db.ComplaintRemarks.Add(new ComplaintRemark { ComplaintId = _complaint.Id, Remark = "Reversal is in progress", Visibility = RemarkVisibility.Customer, CreatedByEmployeeId = "300001", CreatedByName = "RAKESH", CreatedAt = _clock.Now.AddHours(-2) });
        _db.SaveChanges();
    }

    private TrackingService Service => new(_db, _clock, Options.Create(new TrackingOptions { ExposeOtpForTesting = true }));

    private async Task<string> CodeAsync() => (await Service.RequestOtpAsync(new(_complaint.ComplaintNumber, "9876543210"), null, default)).DevelopmentOtp!;

    [Fact]
    public async Task Wrong_mobile_gets_the_same_answer_and_no_code()
    {
        var right = await Service.RequestOtpAsync(new(_complaint.ComplaintNumber, "9876543210"), null, default);
        var wrong = await Service.RequestOtpAsync(new(_complaint.ComplaintNumber, "9000000000"), null, default);
        Assert.Equal(right.Message, wrong.Message);
        Assert.Null(wrong.DevelopmentOtp);
        Assert.Single(_db.TrackingOtps);
    }

    [Fact]
    public async Task Code_is_sent_by_SMS_and_stored_only_as_a_hash()
    {
        var code = await CodeAsync();
        var otp = _db.TrackingOtps.Single();
        Assert.DoesNotContain(code, otp.CodeHash);
        var sms = Assert.Single(_db.Notifications, n => n.Event == NotificationEvents.TrackingOtp);
        Assert.Equal(("SMS", "9876543210"), (sms.Channel, sms.Recipient));
    }

    [Fact]
    public async Task Correct_code_shows_customer_safe_details_only()
    {
        var view = await Service.VerifyAsync(new(_complaint.ComplaintNumber, "+91 98765 43210", await CodeAsync()), null, default);

        Assert.Equal(("Under process", "Branch A1"), (view.Status, view.Branch));
        Assert.Contains(view.Updates, u => u.Detail == "Reversal is in progress");
        Assert.DoesNotContain(view.Updates, u => u.Detail?.Contains("fraud") == true);
        Assert.DoesNotContain(view.Updates, u => (u.Title + u.Detail).Contains("RAKESH"));
        Assert.Contains(view.Updates, u => u.Title == "Complaint registered");
    }

    [Fact]
    public async Task Codes_work_once_and_expire()
    {
        var code = await CodeAsync();
        await Service.VerifyAsync(new(_complaint.ComplaintNumber, "9876543210", code), null, default);
        await Assert.ThrowsAsync<DomainException>(() => Service.VerifyAsync(new(_complaint.ComplaintNumber, "9876543210", code), null, default));

        var second = await CodeAsync();
        _clock.Now = _clock.Now.AddMinutes(11);
        await Assert.ThrowsAsync<DomainException>(() => Service.VerifyAsync(new(_complaint.ComplaintNumber, "9876543210", second), null, default));
    }

    [Fact]
    public async Task Five_wrong_guesses_lock_the_code()
    {
        var code = await CodeAsync();
        var wrong = code == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<DomainException>(() => Service.VerifyAsync(new(_complaint.ComplaintNumber, "9876543210", wrong), null, default));
        await Assert.ThrowsAsync<DomainException>(() => Service.VerifyAsync(new(_complaint.ComplaintNumber, "9876543210", code), null, default));
    }

    [Fact]
    public async Task At_most_three_codes_per_window()
    {
        for (var i = 0; i < 3; i++) await CodeAsync();
        await Assert.ThrowsAsync<DomainException>(CodeAsync);
        _clock.Now = _clock.Now.AddMinutes(16);
        Assert.NotNull(await CodeAsync());
    }
}
