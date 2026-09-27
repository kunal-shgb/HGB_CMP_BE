using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.UnitTests;

public class CustomerNotificationTests
{
    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly TestData _data;
    private readonly FixedClock _clock = new(DateTimeOffset.UtcNow);

    public CustomerNotificationTests() => _data = TestData.Seed(_db);

    private ComplaintService Service(FakeUser user) => new(
        _db, user, new FakeAudit(), new FakeIam(Staff.At("E-A1", "Branch", "A1")), _data.Org, _clock, Options.Create(new SlaOptions()),
        new ComplaintFilterValidator(), new ChangeStatusValidator(), new AssignComplaintValidator(), new AddRemarkValidator(),
        new CustomerNotifier(_db, _clock));

    private static FakeUser Maker => FakeUser.AtBranch("E-A1", "A1", AppRoles.Maker);
    private static FakeUser Checker => FakeUser.AtRegion("E-RC", "RA", AppRoles.Checker);

    private Complaint Add(string status, string? email = null, string? channel = null)
    {
        var c = _data.AddComplaint(_db, _data.BranchA1, status);
        c.Email = email;
        c.PreferredChannel = channel;
        _db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task Only_changes_the_customer_can_see_produce_a_message()
    {
        var c = Add("NEW");
        // New → Assigned: "Registered" → "Under review" is visible.
        await Service(Maker).AssignAsync(c.Id, new AssignComplaintRequest("E-A1", null, null), default);
        // Assigned → Under process: "Under review" → "Under process" is visible.
        await Service(Maker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("UNDER_PROCESS", null), default);
        Assert.Equal(["STATUS_UPDATE", "STATUS_UPDATE"], _db.Notifications.OrderBy(n => n.CreatedAt).Select(n => n.Event));
        Assert.Contains("Under process", _db.Notifications.OrderBy(n => n.Id).Last().Body);
    }

    [Fact]
    public async Task Checker_approval_sends_resolution_but_the_approval_step_itself_is_silent()
    {
        var c = Add("UNDER_PROCESS");
        await Service(Maker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("RESOLVED", "Refund done"), default);
        Assert.Empty(_db.Notifications); // pending checker approval is internal

        await Service(Checker).ApproveAsync(_db.ComplaintApprovals.Single().Id, new DecideApprovalRequest(null), default);
        var n = Assert.Single(_db.Notifications);
        Assert.Equal(("RESOLVED", "SMS", "9876543210"), (n.Event, n.Channel, n.Recipient));
        Assert.Contains(c.ComplaintNumber, n.Body);
    }

    [Fact]
    public async Task Email_is_used_when_preferred_and_given()
    {
        var c = Add("RESOLVED", email: "customer@example.com", channel: "EMAIL");
        await Service(Maker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default);
        var n = Assert.Single(_db.Notifications);
        Assert.Equal(("CLOSED", "EMAIL", "customer@example.com"), (n.Event, n.Channel, n.Recipient));
        Assert.NotNull(n.Subject);
    }

    [Fact]
    public async Task Staff_see_messages_with_masked_recipients()
    {
        var c = Add("RESOLVED", email: "customer@example.com", channel: "EMAIL");
        await Service(Maker).ChangeStatusAsync(c.Id, new ChangeStatusRequest("CLOSED", null), default);
        var item = Assert.Single(await Service(Maker).GetNotificationsAsync(c.Id, default));
        Assert.Equal("c***@example.com", item.RecipientMasked);
    }
}

public class NotificationDispatcherTests
{
    private sealed class FlakySender(params bool[] outcomes) : INotificationSender
    {
        private int _call;
        public Task<SendResult> SendAsync(Notification message, CancellationToken ct) =>
            Task.FromResult(outcomes[Math.Min(_call++, outcomes.Length - 1)] ? new SendResult(true, "ref-1") : new SendResult(false, Error: "gateway down"));
    }

    private readonly TestDbContext _db = TestDbContext.Create();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 9, 27, 6, 0, 0, TimeSpan.Zero));

    private Notification Queue()
    {
        var n = new Notification { Event = "REGISTERED", Channel = "SMS", Recipient = "9876543210", Body = "x", CreatedAt = _clock.Now, NextAttemptAt = _clock.Now };
        _db.Notifications.Add(n);
        _db.SaveChanges();
        return n;
    }

    private NotificationDispatcher Dispatcher(INotificationSender sender) => new(_db, sender, _clock, NullLogger<NotificationDispatcher>.Instance);

    [Fact]
    public async Task Successful_send_is_marked_sent()
    {
        var n = Queue();
        Assert.Equal(1, await Dispatcher(new FlakySender(true)).DispatchDueAsync(default));
        Assert.Equal((NotificationStatus.Sent, "ref-1", 1), (n.Status, n.ProviderReference, n.Attempts));
    }

    [Fact]
    public async Task Failures_back_off_then_give_up_after_five_attempts()
    {
        var n = Queue();
        var sender = new FlakySender(false);
        await Dispatcher(sender).DispatchDueAsync(default);
        Assert.Equal((NotificationStatus.Pending, _clock.Now.AddMinutes(1)), (n.Status, n.NextAttemptAt));

        // Not due yet: nothing is attempted.
        Assert.Equal(0, await Dispatcher(sender).DispatchDueAsync(default));

        for (var i = 0; i < 4; i++)
        {
            _clock.Now = n.NextAttemptAt;
            await Dispatcher(sender).DispatchDueAsync(default);
        }
        Assert.Equal((NotificationStatus.Failed, 5, "gateway down"), (n.Status, n.Attempts, n.LastError));
    }
}
