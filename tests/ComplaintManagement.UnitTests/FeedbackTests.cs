using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Feedback;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class FeedbackTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 27, 6, 0, 0, TimeSpan.Zero));
    private static readonly FakeUser Head = FakeUser.AtBranch("300001", "A1", AppRoles.OfficeHead, AppRoles.Viewer);
    private static readonly FakeUser Clerk = FakeUser.AtBranch("300005", "A1", AppRoles.Viewer);

    public FeedbackTests() => _data = TestData.Seed(_db);

    private FeedbackService Feedback => new(_db, _clock);

    private ComplaintService Staff(FakeUser user)
    {
        var iam = new FakeIam();
        var notifier = new CustomerNotifier(_db, _clock, Options.Create(new FeedbackOptions { LinkBaseUrl = "https://bank.example/feedback/" }));
        return new(_db, user, new FakeAudit(), iam, _data.Org, _clock, Options.Create(new SlaOptions()),
            new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
            notifier, FakeRoleMappings.Policy(iam), null!);
    }

    /// <summary>A complaint closed now, with a feedback token for that closure.</summary>
    private (Complaint Complaint, string Token) Closed()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "CLOSED");
        c.ClosedAt = _clock.Now;
        var token = FeedbackRules.Invite(_db, c, "CLOSURE_MESSAGE", _clock.Now);
        _db.SaveChanges();
        return (c, token);
    }

    [Fact]
    public async Task Closing_a_complaint_sends_a_one_time_feedback_link()
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, "RESOLVED");
        await Staff(Head).ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default);

        var message = _db.Notifications.Single(n => n.Event == "CLOSED");
        Assert.Contains("https://bank.example/feedback/", message.Body);
        var invitation = _db.FeedbackInvitations.Single();
        Assert.Equal(c.ClosedAt, invitation.ForClosedAt);
        var token = message.Body.Split("/feedback/")[1].Split(' ')[0];
        Assert.Equal(invitation.TokenHash, FeedbackRules.Hash(token)); // only the hash is stored
        Assert.Equal("OPEN", (await Feedback.GetAsync(token, default)).State);
    }

    [Fact]
    public async Task Customer_rates_once_per_closure()
    {
        var (c, token) = Closed();
        var result = await Feedback.SubmitAsync(token, new SubmitFeedbackRequest(true, 5, "  Quick refund, thanks  "), "10.0.0.1", default);
        Assert.False(result.FlaggedForReview);

        var f = _db.ComplaintFeedback.Single();
        Assert.Equal((true, 5, "Quick refund, thanks", c.ClosedAt!.Value), (f.Resolved, f.Rating, f.Comment, f.ForClosedAt));
        Assert.Equal("SUBMITTED", (await Feedback.GetAsync(token, default)).State);
        await Assert.ThrowsAsync<DomainException>(() => Feedback.SubmitAsync(token, new SubmitFeedbackRequest(true, 4, null), null, default));

        // A second link for the same closure (e.g. from tracking) cannot rate it again.
        var other = FeedbackRules.Invite(_db, c, "TRACKING", _clock.Now);
        _db.SaveChanges();
        Assert.Equal("SUBMITTED", (await Feedback.GetAsync(other, default)).State);
        Assert.Contains(_db.AuditLogs, a => a.Action == "FEEDBACK" && a.EmployeeId == "CUSTOMER");
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 6)]
    public async Task Answers_are_validated(bool? resolved, int rating)
    {
        var (_, token) = Closed();
        await Assert.ThrowsAsync<ValidationException>(() => Feedback.SubmitAsync(token, new SubmitFeedbackRequest(resolved, rating, null), null, default));
        Assert.Empty(_db.ComplaintFeedback);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("has spaces in it but is long enough")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // well-formed but unknown
    public async Task Unknown_or_malformed_links_are_not_found(string token) =>
        await Assert.ThrowsAsync<NotFoundException>(() => Feedback.GetAsync(token, default));

    [Fact]
    public async Task Links_expire_after_the_admin_window_and_when_the_complaint_is_reopened()
    {
        var (c, token) = Closed();
        _db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.FeedbackWindowDays, Value = "7" });
        _db.SaveChanges();
        _clock.Now = _clock.Now.AddDays(8);
        Assert.Equal("EXPIRED", (await Feedback.GetAsync(token, default)).State);
        await Assert.ThrowsAsync<DomainException>(() => Feedback.SubmitAsync(token, new SubmitFeedbackRequest(true, 4, null), null, default));

        _clock.Now = _clock.Now.AddDays(-8);
        c.ClosedAt = null; // reopened
        c.StatusCode = "REOPENED";
        _db.SaveChanges();
        Assert.Equal("EXPIRED", (await Feedback.GetAsync(token, default)).State);
    }

    [Fact]
    public async Task Not_resolved_is_flagged_until_staff_review_it()
    {
        var (c, token) = Closed();
        var result = await Feedback.SubmitAsync(token, new SubmitFeedbackRequest(false, 2, "Money still not received"), null, default);
        Assert.True(result.FlaggedForReview);

        var detail = await Staff(Head).GetAsync(c.Id, default);
        Assert.True(detail.Feedback is { Resolved: false, Rating: 2, NeedsReview: true, CanReview: true });
        Assert.Single((await Staff(Head).ListAsync(new ComplaintFilterRequest { FeedbackNeedsReview = true }, default)).Items);
        Assert.False((await Staff(Clerk).GetAsync(c.Id, default)).Feedback!.CanReview); // view-only staff
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Staff(Clerk).ReviewFeedbackAsync(c.Id, new ReviewFeedbackRequest(null), default));

        await Staff(Head).ReviewFeedbackAsync(c.Id, new ReviewFeedbackRequest("Called the customer; refund reached today."), default);
        detail = await Staff(Head).GetAsync(c.Id, default);
        Assert.True(detail.Feedback is { NeedsReview: false, ReviewNote: "Called the customer; refund reached today." });
        Assert.Equal("300001", detail.Feedback!.ReviewedBy?.EmployeeId);
        Assert.Empty((await Staff(Head).ListAsync(new ComplaintFilterRequest { FeedbackNeedsReview = true }, default)).Items);
        Assert.Contains(await Staff(Head).GetHistoryAsync(c.Id, default), e => e.Type == "FEEDBACK");
    }

    [Fact]
    public async Task Reopening_the_complaint_clears_the_flag()
    {
        var (c, token) = Closed();
        await Feedback.SubmitAsync(token, new SubmitFeedbackRequest(false, 1, null), null, default);
        await Staff(Head).ChangeStatusAsync(c.Id, new ChangeStatusRequest("REOPENED", "Customer says not resolved"), default);
        Assert.False((await Staff(Head).GetAsync(c.Id, default)).Feedback!.NeedsReview);
    }
}
