using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Application.Complaints;

public interface IComplaintService
{
    Task<PagedResponse<ComplaintListItem>> ListAsync(ComplaintFilterRequest filter, CancellationToken ct);
    Task<ComplaintDetail> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TimelineEvent>> GetHistoryAsync(Guid id, CancellationToken ct);
    Task<ChangeStatusResponse> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct);
    Task<IReadOnlyList<ApprovalListItem>> ListPendingApprovalsAsync(CancellationToken ct);
    Task ApproveAsync(Guid approvalId, DecideApprovalRequest request, CancellationToken ct);
    Task ReturnAsync(Guid approvalId, DecideApprovalRequest request, CancellationToken ct);
    Task AssignAsync(Guid id, AssignComplaintRequest request, CancellationToken ct);
    Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, CancellationToken ct);
    /// <summary>A remark with files attached to it. Files need the AddAttachment right as well.</summary>
    Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, IReadOnlyList<UploadFile> files, CancellationToken ct);
    Task EscalateAsync(Guid id, EscalateRequest request, CancellationToken ct);
    Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync(Guid id, CancellationToken ct);
    /// <summary>Marks the customer's "not resolved" feedback as reviewed. Needs the right to change the complaint's status.</summary>
    Task ReviewFeedbackAsync(Guid id, ReviewFeedbackRequest request, CancellationToken ct);
}

public sealed class ComplaintService(
    IApplicationDbContext db,
    ICurrentUser user,
    IAuditLogger audit,
    IIamUserService iam,
    IIamOrganisationService org,
    TimeProvider clock,
    IOptions<SlaOptions> slaOptions,
    IValidator<ComplaintFilterRequest> filterValidator,
    IValidator<ChangeStatusRequest> statusValidator,
    IValidator<AssignComplaintRequest> assignValidator,
    IValidator<AddRemarkRequest> remarkValidator,
    CustomerNotifier notifier,
    AssignmentPolicy assignment,
    AttachmentStore attachmentStore) : IComplaintService
{
    private const string Module = "Complaint";

    public async Task<PagedResponse<ComplaintListItem>> ListAsync(ComplaintFilterRequest filter, CancellationToken ct)
    {
        await filterValidator.ValidateAndThrowAsync(filter, ct);

        var query = db.Complaints.AsNoTracking().VisibleTo(user);
        query = ApplyFilters(query, filter, clock.GetUtcNow());

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new
            {
                c.Id, c.ComplaintNumber, c.Title, c.CustomerName, c.MobileNumber, c.CreatedAt, c.SlaDueDate, c.ClosedAt, c.EscalationLevel, c.Source,
                c.AssignedEmployeeId, c.AssignedEmployeeName,
                Branch = new OrgRef(c.BranchCode, c.BranchName),
                Region = new OrgRef(c.RegionCode, c.RegionName),
                Category = new OrgRef(c.Category!.Code, c.Category.Name),
                Status = new StatusRef(c.Status!.Code, c.Status.Name, c.Status.IsTerminal),
                Priority = new OrgRef(c.Priority!.Code, c.Priority.Name),
            })
            .ToListAsync(ct);

        var now = clock.GetUtcNow();
        var items = rows.Select(r => new ComplaintListItem(
            r.Id, r.ComplaintNumber, r.Title, r.CustomerName, Masking.Mobile(r.MobileNumber),
            r.Branch, r.Region, r.Category, r.Status, r.Priority,
            r.AssignedEmployeeId is null ? null : new EmployeeRef(r.AssignedEmployeeId, r.AssignedEmployeeName),
            BuildSla(r.CreatedAt, r.SlaDueDate, r.ClosedAt, now),
            r.EscalationLevel,
            SourceRef(r.Source),
            r.CreatedAt)).ToList();

        audit.Log("LIST", Module, null, $"page={filter.Page}");
        await db.SaveChangesAsync(ct);

        return new PagedResponse<ComplaintListItem>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ComplaintDetail> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await LoadScopedAsync(id, ct, includeChildren: true);
        var unmasked = ComplaintAccess.Can(user, c, Permissions.ComplaintViewUnmasked);
        var now = clock.GetUtcNow();

        var transitions = await (
                from t in db.StatusTransitions.AsNoTracking()
                join s in db.Statuses.AsNoTracking() on t.ToStatusCode equals s.Code
                where t.IsActive && s.IsActive && t.FromStatusCode == c.StatusCode
                orderby s.SortOrder
                select new AllowedTransition(s.Code, s.Name, t.RequiresRemark, t.RequiresApproval))
            .ToListAsync(ct);

        if (!ComplaintAccess.Can(user, c, Permissions.ComplaintChangeStatus)) transitions = [];

        var pending = await db.ComplaintApprovals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ComplaintId == c.Id && a.Status == ApprovalStatus.Pending, ct);
        PendingApprovalInfo? pendingInfo = null;
        if (pending is not null)
        {
            // Nothing else moves while a decision is with the Checker.
            transitions = [];
            var requested = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == pending.RequestedStatusCode, ct);
            pendingInfo = new PendingApprovalInfo(
                pending.Id,
                new StatusRef(requested.Code, requested.Name, requested.IsTerminal),
                new EmployeeRef(pending.RequestedByEmployeeId, pending.RequestedByName),
                pending.RequestedByOfficeName,
                pending.RequestedAt,
                pending.MakerRemarks,
                pending.ApproverLevel.ToString(),
                pending.ApproverOfficeCode,
                pending.ApproverDepartment,
                ApprovalRules.CanDecide(user, pending));
        }

        audit.Log("VIEW", Module, c.Id.ToString(), unmasked ? "unmasked" : null);
        await db.SaveChangesAsync(ct);

        return new ComplaintDetail(
            c.Id,
            c.ComplaintNumber,
            new CustomerInfo(
                c.CustomerName,
                unmasked ? c.MobileNumber : Masking.Mobile(c.MobileNumber),
                c.Email,
                c.CustomerId is null ? null : unmasked ? c.CustomerId : Masking.Identifier(c.CustomerId),
                c.AccountNumber is null ? null : unmasked ? c.AccountNumber : Masking.Account(c.AccountNumber),
                c.PreferredChannel,
                !unmasked),
            new TransactionInfo(c.TransactionId, c.TransactionDate, c.TransactionAmount),
            c.Title,
            c.Description,
            SourceRef(c.Source),
            c.LodgedByEmployeeId is null ? null : new LodgedByInfo(c.LodgedByEmployeeId, c.LodgedByName, c.LodgedByOfficeName),
            new OrgRef(c.BranchCode, c.BranchName),
            new OrgRef(c.RegionCode, c.RegionName),
            new OrgRef(c.Category!.Code, c.Category.Name),
            new StatusRef(c.Status!.Code, c.Status.Name, c.Status.IsTerminal),
            new OrgRef(c.Priority!.Code, c.Priority.Name),
            c.AssignedEmployeeId is null ? null : new EmployeeRef(c.AssignedEmployeeId, c.AssignedEmployeeName),
            c.AssignedDepartmentCode is null ? null : new OrgRef(c.AssignedDepartmentCode, c.AssignedDepartmentName ?? c.AssignedDepartmentCode),
            c.EscalationLevel,
            c.EscalatedDivisionCode is null ? null : new OrgRef(c.EscalatedDivisionCode, c.EscalatedDivisionName ?? c.EscalatedDivisionCode),
            NextEscalationLevel(c) is not null,
            BuildSla(c.CreatedAt, c.SlaDueDate, c.ClosedAt, now),
            c.CreatedAt,
            c.UpdatedAt,
            c.ResolvedAt,
            c.ClosedAt,
            transitions,
            pendingInfo,
            c.Remarks.OrderByDescending(r => r.CreatedAt)
                .Select(r => ToRemarkItem(r, c.Attachments.Where(a => a.RemarkId == r.Id))).ToList(),
            c.Attachments.Where(a => a.RemarkId == null).OrderBy(a => a.UploadedAt)
                .Select(Attachments.AttachmentService.ToItem).ToList(),
            ComplaintAccess.Abilities(user, c, NextEscalationLevel(c) is not null, await assignment.CanAssignAsync(user, c, ct)),
            ToFeedbackInfo(c));
    }

    public async Task<IReadOnlyList<TimelineEvent>> GetHistoryAsync(Guid id, CancellationToken ct)
    {
        var c = await LoadScopedAsync(id, ct, includeChildren: true);
        var statusNames = await db.Statuses.AsNoTracking().ToDictionaryAsync(s => s.Code, s => s.Name, ct);
        string Name(string? code) => code is null ? "-" : statusNames.GetValueOrDefault(code, code);

        var events = new List<TimelineEvent>();
        events.AddRange(c.StatusHistory.Select(h => new TimelineEvent(
            h.ChangedAt,
            "STATUS",
            h.OldStatusCode is null ? "Complaint registered" : $"Status changed to {Name(h.NewStatusCode)}",
            h.OldStatusCode is null ? h.Remarks : $"{Name(h.OldStatusCode)} → {Name(h.NewStatusCode)}" + (h.Remarks is null ? "" : $". {h.Remarks}"),
            new EmployeeRef(h.ChangedByEmployeeId, h.ChangedByName))));
        events.AddRange(c.Assignments.Select(a => new TimelineEvent(
            a.AssignedAt,
            "ASSIGNMENT",
            $"Assigned to {a.AssignedToName ?? a.AssignedToEmployeeId}",
            a.Remarks,
            new EmployeeRef(a.AssignedByEmployeeId, null))));
        events.AddRange(c.Remarks.Select(r => new TimelineEvent(
            r.CreatedAt,
            r.Visibility == RemarkVisibility.Internal ? "REMARK_INTERNAL" : "REMARK_CUSTOMER",
            r.Visibility == RemarkVisibility.Internal ? "Internal remark added" : "Customer-visible remark added",
            r.Remark,
            new EmployeeRef(r.CreatedByEmployeeId, r.CreatedByName))));
        events.AddRange(c.Escalations.Select(e => new TimelineEvent(
            e.EscalatedAt,
            "ESCALATION",
            $"Escalated to {EscalationLevels.Name(e.ToLevel)}" + (e.ToDivisionName is null ? "" : $", {e.ToDivisionName}") + $" (level {e.ToLevel})",
            e.Reason,
            new EmployeeRef(e.EscalatedBy, e.EscalatedByName))));
        events.AddRange(c.Feedback.Select(f => new TimelineEvent(
            f.SubmittedAt,
            "FEEDBACK",
            $"Customer feedback: {(f.Resolved ? "resolved" : "not resolved")}, rated {f.Rating} of 5",
            f.Comment,
            new EmployeeRef("CUSTOMER", "Customer"))));
        events.AddRange(c.Feedback.Where(f => f.ReviewedAt is not null).Select(f => new TimelineEvent(
            f.ReviewedAt!.Value,
            "FEEDBACK_REVIEWED",
            "Customer feedback reviewed",
            f.ReviewNote,
            new EmployeeRef(f.ReviewedBy ?? "", f.ReviewedByName))));
        events.AddRange(c.Attachments.Select(a => new TimelineEvent(
            a.UploadedAt, "ATTACHMENT", "Attachment uploaded", a.FileName, new EmployeeRef(a.UploadedBy, a.UploadedByName))));

        return events.OrderByDescending(e => e.At).ToList();
    }

    public async Task<ChangeStatusResponse> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);
        ComplaintAccess.Demand(user, c, Permissions.ComplaintChangeStatus);

        if (await db.ComplaintApprovals.AnyAsync(a => a.ComplaintId == c.Id && a.Status == ApprovalStatus.Pending, ct))
            throw new DomainException("approval.pending", "This complaint is waiting for a Checker's decision.");

        var transition = await db.StatusTransitions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.FromStatusCode == c.StatusCode && t.ToStatusCode == request.NewStatus, ct)
            ?? throw new DomainException("status.transition_not_allowed",
                $"A complaint in status {c.StatusCode} cannot be moved to {request.NewStatus}.");

        if (transition.RequiresRemark && string.IsNullOrWhiteSpace(request.Remarks))
            throw new DomainException("status.remark_required", "A remark is required for this status change.");

        var target = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == request.NewStatus, ct);
        var remarks = request.Remarks?.Trim();

        if (!transition.RequiresApproval)
        {
            ApplyStatus(c, target, remarks);
            audit.Log("CHANGE_STATUS", Module, c.Id.ToString(), $"{transition.FromStatusCode}->{target.Code}");
            await db.SaveChangesAsync(ct);
            return new ChangeStatusResponse(false, $"Status changed to {target.Name}.");
        }

        // Maker-checker: record the request and hold the complaint until a Checker decides.
        var holding = await db.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.IsApprovalPending && s.IsActive, ct)
            ?? throw new InvalidOperationException("No approval-pending status is configured.");
        var hoDepartment = await db.AppSettings.AsNoTracking()
            .Where(x => x.Key == AppSettingKeys.HeadOfficeMakerCheckerDepartment).Select(x => x.Value).FirstOrDefaultAsync(ct);
        var (level, approverOffice, approverDepartment) = ApprovalRules.ApproverFor(user, c, hoDepartment);
        db.ComplaintApprovals.Add(new ComplaintApproval
        {
            ComplaintId = c.Id,
            RequestedStatusCode = target.Code,
            PreviousStatusCode = c.StatusCode,
            MakerRemarks = remarks,
            RequestedByEmployeeId = user.EmployeeId,
            RequestedByName = user.Name,
            RequestedByOfficeName = user.OfficeName,
            RequestedAt = clock.GetUtcNow(),
            ApproverLevel = level,
            ApproverOfficeCode = approverOffice,
            ApproverDepartment = approverDepartment,
        });
        ApplyStatus(c, holding, $"Requested: {target.Name}" + (remarks is null ? "" : $". {remarks}"));
        audit.Log("REQUEST_APPROVAL", Module, c.Id.ToString(), $"{transition.FromStatusCode}->{target.Code} approver={level}:{approverOffice ?? approverDepartment}");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index on one pending approval per complaint: someone else asked first.
            throw new DomainException("approval.pending", "This complaint is already waiting for a Checker's decision.");
        }

        var approver = level != ScopeLevel.HeadOffice ? "your Regional Office"
            : approverDepartment is null ? "Head Office" : $"Head Office ({approverDepartment})";
        return new ChangeStatusResponse(true, $"Sent to a Checker at {approver} for approval.");
    }

    public async Task<IReadOnlyList<ApprovalListItem>> ListPendingApprovalsAsync(CancellationToken ct)
    {
        var rows = await db.ComplaintApprovals.AsNoTracking()
            .DecidableBy(user)
            .OrderBy(a => a.RequestedAt)
            .Take(200)
            .Select(a => new
            {
                a.Id, a.ComplaintId, a.RequestedStatusCode, a.RequestedByEmployeeId, a.RequestedByName,
                a.RequestedByOfficeName, a.RequestedAt, a.MakerRemarks,
                a.Complaint!.ComplaintNumber, a.Complaint.CustomerName, a.Complaint.CreatedAt, a.Complaint.SlaDueDate, a.Complaint.ClosedAt,
                Branch = new OrgRef(a.Complaint.BranchCode, a.Complaint.BranchName),
                Category = new OrgRef(a.Complaint.Category!.Code, a.Complaint.Category.Name),
            })
            .ToListAsync(ct);

        var statuses = await db.Statuses.AsNoTracking().ToDictionaryAsync(s => s.Code, ct);
        var now = clock.GetUtcNow();
        return rows.Select(r =>
        {
            var s = statuses[r.RequestedStatusCode];
            return new ApprovalListItem(
                r.Id, r.ComplaintId, r.ComplaintNumber, r.CustomerName, r.Branch, r.Category,
                new StatusRef(s.Code, s.Name, s.IsTerminal),
                new EmployeeRef(r.RequestedByEmployeeId, r.RequestedByName),
                r.RequestedByOfficeName, r.RequestedAt, r.MakerRemarks,
                BuildSla(r.CreatedAt, r.SlaDueDate, r.ClosedAt, now));
        }).ToList();
    }

    public Task ApproveAsync(Guid approvalId, DecideApprovalRequest request, CancellationToken ct) =>
        DecideAsync(approvalId, request, approve: true, ct);

    public Task ReturnAsync(Guid approvalId, DecideApprovalRequest request, CancellationToken ct) =>
        DecideAsync(approvalId, request, approve: false, ct);

    private async Task DecideAsync(Guid approvalId, DecideApprovalRequest request, bool approve, CancellationToken ct)
    {
        var remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        if (remarks is { Length: > 4000 })
            throw new DomainException("approval.remark_too_long", "Keep remarks under 4,000 characters.");
        if (!approve && remarks is null)
            throw new DomainException("approval.remark_required", "Tell the Maker why the request is being returned.");

        var approval = await db.ComplaintApprovals.FirstOrDefaultAsync(a => a.Id == approvalId, ct)
            ?? throw new NotFoundException("Approval", approvalId);
        var c = await LoadScopedAsync(approval.ComplaintId, ct, includeChildren: false);

        if (approval.Status != ApprovalStatus.Pending)
            throw new DomainException("approval.already_decided", "This request has already been decided.");
        if (!ApprovalRules.CanDecide(user, approval))
            throw new ForbiddenAccessException(
                string.Equals(user.EmployeeId, approval.RequestedByEmployeeId, StringComparison.OrdinalIgnoreCase)
                    ? "You cannot approve your own request."
                    : "This request must be decided by a Checker at the approving office or department.");

        var targetCode = approve ? approval.RequestedStatusCode : approval.PreviousStatusCode;
        var target = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == targetCode, ct);

        approval.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Returned;
        approval.DecidedByEmployeeId = user.EmployeeId;
        approval.DecidedByName = user.Name;
        approval.DecidedAt = clock.GetUtcNow();
        approval.DecisionRemarks = remarks;
        ApplyStatus(c, target, (approve ? "Approved by Checker" : "Returned to Maker") + (remarks is null ? "" : $". {remarks}"));

        audit.Log(approve ? "APPROVE" : "RETURN", Module, c.Id.ToString(), $"approval={approval.Id} -> {target.Code}");
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("approval.already_decided", "Another Checker decided this request first.");
        }
    }

    public async Task AssignAsync(Guid id, AssignComplaintRequest request, CancellationToken ct)
    {
        await assignValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);
        await assignment.DemandAsync(user, c, ct);

        var status = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == c.StatusCode, ct);
        if (status.IsTerminal)
            throw new DomainException("assign.terminal_status", "A closed complaint cannot be assigned. Reopen it first.");

        var assignee = await iam.GetUserAsync(request.AssignedToEmployeeId, ct)
            ?? throw new DomainException("assign.unknown_employee", "The selected employee was not found in the Bank directory.");
        if (!await ComplaintScope.CanTargetAsync(org, user, assignee, ct))
            throw new ForbiddenAccessException("You can only assign complaints to active employees within your office.");

        IamDepartment? department = null;
        if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            department = await org.FindDepartmentAsync(request.DepartmentCode.Trim(), ct);
            if (department is not { IsActive: true })
                throw new DomainException("assign.unknown_department", "The selected department was not found.");
        }

        var now = clock.GetUtcNow();
        db.ComplaintAssignments.Add(new ComplaintAssignment
        {
            ComplaintId = c.Id,
            AssignedFromEmployeeId = c.AssignedEmployeeId,
            AssignedToEmployeeId = assignee.EmployeeCode,
            AssignedToName = assignee.FullName,
            AssignedDepartmentCode = department?.Code ?? c.AssignedDepartmentCode,
            Remarks = request.Remarks?.Trim(),
            AssignedByEmployeeId = user.EmployeeId,
            AssignedAt = now,
        });
        c.AssignedEmployeeId = assignee.EmployeeCode;
        c.AssignedEmployeeName = assignee.FullName;
        if (department is not null)
        {
            c.AssignedDepartmentCode = department.Code;
            c.AssignedDepartmentName = department.Name;
        }

        // Move to the workflow's "assigned" status when the current status allows it.
        var assignedStatus = await (
                from t in db.StatusTransitions.AsNoTracking()
                join s in db.Statuses.AsNoTracking() on t.ToStatusCode equals s.Code
                where t.IsActive && t.FromStatusCode == c.StatusCode && s.IsAssignment && !t.RequiresRemark
                select s)
            .FirstOrDefaultAsync(ct);
        if (assignedStatus is not null) ApplyStatus(c, assignedStatus, request.Remarks?.Trim());

        audit.Log("ASSIGN", Module, c.Id.ToString(), $"to={assignee.EmployeeCode}");
        await db.SaveChangesAsync(ct);
    }

    public Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, CancellationToken ct) =>
        AddRemarkAsync(id, request, [], ct);

    public async Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, IReadOnlyList<UploadFile> files, CancellationToken ct)
    {
        await remarkValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);
        ComplaintAccess.Demand(user, c, Permissions.ComplaintAddRemark);
        if (files.Count > 0)
        {
            ComplaintAccess.Demand(user, c, Permissions.ComplaintAddAttachment);
            await attachmentStore.EnsureRoomAsync(c.Id, files.Count, ct);
        }

        var remark = new ComplaintRemark
        {
            ComplaintId = c.Id,
            Remark = request.Remark.Trim(),
            Visibility = Enum.Parse<RemarkVisibility>(request.Visibility, ignoreCase: true),
            CreatedByEmployeeId = user.EmployeeId,
            CreatedByName = user.Name,
            CreatedAt = clock.GetUtcNow(),
        };
        db.ComplaintRemarks.Add(remark);
        c.UpdatedAt = remark.CreatedAt;

        var stored = files.Count == 0 ? [] : await attachmentStore.StoreAsync(c.Id, files, user.EmployeeId, user.Name, ct);
        foreach (var a in stored)
        {
            a.RemarkId = remark.Id;
            audit.Log("UPLOAD_ATTACHMENT", "Attachment", c.Id.ToString(), $"{a.Id} {a.ContentType} {a.FileSize}B remark={remark.Id}");
        }
        audit.Log("ADD_REMARK", Module, c.Id.ToString(), remark.Visibility.ToString() + (stored.Count == 0 ? "" : $" files={stored.Count}"));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await attachmentStore.DiscardAsync(stored);
            throw;
        }
        return ToRemarkItem(remark, stored);
    }

    public async Task EscalateAsync(Guid id, EscalateRequest request, CancellationToken ct)
    {
        var remarks = request.Remarks?.Trim();
        if (string.IsNullOrEmpty(remarks))
            throw new DomainException("escalation.remark_required", "Give a reason for escalating.");
        if (remarks.Length > 4000)
            throw new DomainException("escalation.remark_too_long", "Keep the reason under 4,000 characters.");

        var c = await LoadScopedAsync(id, ct, includeChildren: false);
        ComplaintAccess.Demand(user, c, Permissions.ComplaintEscalate);
        var target = NextEscalationLevel(c)
            ?? throw new DomainException("escalation.not_allowed", c.ClosedAt is not null
                ? "A closed complaint cannot be escalated."
                : "This complaint is already escalated beyond your office.");

        var from = c.EscalationLevel;
        var step = await Escalation.EscalationStep.ApplyAsync(db, org, c, target, remarks, user.EmployeeId, user.Name, clock.GetUtcNow(), ct);
        audit.Log("ESCALATE", Module, c.Id.ToString(), $"level {from}->{target}" + (step.ToDivisionCode is null ? "" : $" division={step.ToDivisionCode}"));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The level the caller may escalate this complaint to, or null. Escalation goes one step above the
    /// higher of the complaint's level and the caller's own office (Branch 1, RO 2, HO 3): a branch sends
    /// it to its RO (or straight to Head Office when its category skips the RO), an RO sends it to Head
    /// Office, and nobody escalates past Head Office.
    /// </summary>
    private int? NextEscalationLevel(Complaint c)
    {
        if (!ComplaintAccess.Can(user, c, Permissions.ComplaintEscalate) || c.ClosedAt is not null) return null;
        var own = ComplaintRouting.LevelOf(user);
        if (own is null || own < c.EscalationLevel) return null;
        var target = ComplaintRouting.NextLevel(Math.Max(c.EscalationLevel, own.Value), c.Category);
        return target <= EscalationLevels.Max ? target : null;
    }

    public async Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync(Guid id, CancellationToken ct)
    {
        var c = await LoadScopedAsync(id, ct, includeChildren: false);
        var rows = await db.Notifications.AsNoTracking().Where(n => n.ComplaintId == c.Id).OrderByDescending(n => n.CreatedAt).ToListAsync(ct);
        return rows.Select(n => new NotificationItem(
            n.Id, n.Event, n.Channel,
            n.Channel == NotificationChannel.Email ? Masking.Email(n.Recipient) : Masking.Mobile(n.Recipient),
            n.Subject, n.Event == NotificationEvents.TrackingOtp ? NotificationEvents.RedactedBody : n.Body,
            n.Status, n.Attempts, n.CreatedAt, n.SentAt, n.LastError)).ToList();
    }

    private void ApplyStatus(Complaint c, ComplaintStatus target, string? remarks)
    {
        var now = clock.GetUtcNow();
        var from = c.Status;
        db.ComplaintStatusHistory.Add(new ComplaintStatusHistory
        {
            ComplaintId = c.Id,
            OldStatusCode = c.StatusCode,
            NewStatusCode = target.Code,
            Remarks = remarks,
            ChangedByEmployeeId = user.EmployeeId,
            ChangedByName = user.Name,
            ChangedAt = now,
        });

        c.StatusCode = target.Code;
        c.UpdatedAt = now;
        if (target.IsResolution) c.ResolvedAt = now;
        c.ClosedAt = target.IsTerminal ? now : null;
        if (!target.IsTerminal && !target.IsResolution) c.ResolvedAt = null;
        // After the dates are set, so a closure message can carry a feedback link for this closure.
        if (from is not null && from.Code != target.Code) notifier.StatusChanged(c, from, target);
    }

    private async Task<Complaint> LoadScopedAsync(Guid id, CancellationToken ct, bool includeChildren)
    {
        IQueryable<Complaint> query = db.Complaints
            .Include(c => c.Category)
            .Include(c => c.Status)
            .Include(c => c.Priority);

        if (includeChildren)
        {
            query = query
                .Include(c => c.StatusHistory)
                .Include(c => c.Assignments)
                .Include(c => c.Remarks)
                .Include(c => c.Attachments)
                .Include(c => c.Escalations)
                .Include(c => c.Feedback);
        }

        // Out-of-scope complaints are reported as not found so their existence is not disclosed.
        return await query.VisibleTo(user).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Complaint", id);
    }

    private SlaInfo BuildSla(DateTimeOffset createdAt, DateTimeOffset? due, DateTimeOffset? closedAt, DateTimeOffset now)
    {
        var (state, age, overdue) = SlaCalculator.Evaluate(createdAt, due, closedAt, now, slaOptions.Value.WarningWindowHours);
        return new SlaInfo(due, state, age, overdue);
    }

    private static RemarkItem ToRemarkItem(ComplaintRemark r, IEnumerable<ComplaintAttachment> files) => new(
        r.Id, r.Remark, r.Visibility.ToString().ToUpperInvariant(),
        new EmployeeRef(r.CreatedByEmployeeId, r.CreatedByName), r.CreatedAt,
        files.OrderBy(a => a.UploadedAt).Select(Attachments.AttachmentService.ToItem).ToList());

    private static IQueryable<Complaint> ApplyFilters(IQueryable<Complaint> q, ComplaintFilterRequest f, DateTimeOffset now)
    {
        static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        if (Clean(f.ComplaintNumber) is { } number) q = q.Where(c => c.ComplaintNumber == number.ToUpperInvariant());
        if (Clean(f.CustomerName) is { } name)
        {
            var lowered = name.ToLowerInvariant();
            q = q.Where(c => c.CustomerName.ToLower().Contains(lowered));
        }
        if (Clean(f.Mobile) is { } mobile) q = q.Where(c => c.MobileNumber == mobile);
        if (Clean(f.AccountNumber) is { } account) q = q.Where(c => c.AccountNumber == account);
        if (Clean(f.CustomerId) is { } customerId) q = q.Where(c => c.CustomerId == customerId);
        if (Clean(f.TransactionId) is { } txn) q = q.Where(c => c.TransactionId == txn);
        if (Clean(f.BranchCode) is { } branch) q = q.Where(c => c.BranchCode == branch);
        if (Clean(f.RegionCode) is { } region) q = q.Where(c => c.RegionCode == region);
        if (Clean(f.CategoryCode) is { } cat) q = q.Where(c => c.Category!.Code == cat);
        if (Clean(f.Status) is { } status) q = q.Where(c => c.StatusCode == status);
        if (Clean(f.Priority) is { } priority) q = q.Where(c => c.PriorityCode == priority);
        if (Clean(f.AssignedEmployeeId) is { } emp) q = q.Where(c => c.AssignedEmployeeId == emp);
        if (f.FromDate is { } from) q = q.Where(c => c.CreatedAt >= IstDate.StartOfDayUtc(from));
        if (f.ToDate is { } to) q = q.Where(c => c.CreatedAt < IstDate.StartOfDayUtc(to.AddDays(1)));
        if (f.OverdueOnly == true) q = q.Where(c => c.ClosedAt == null && c.SlaDueDate != null && c.SlaDueDate < now);
        if (f.MinEscalationLevel is { } minLevel) q = q.Where(c => c.EscalationLevel >= minLevel);
        if (Clean(f.Source) is { } source) q = q.Where(c => c.Source == source);
        if (f.FeedbackNeedsReview == true)
            q = q.Where(c => c.Feedback.Any(x => !x.Resolved && x.ReviewedAt == null && c.ClosedAt != null && x.ForClosedAt == c.ClosedAt));
        return q;
    }

    public async Task ReviewFeedbackAsync(Guid id, ReviewFeedbackRequest request, CancellationToken ct)
    {
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > 1000) throw new DomainException("feedback.note_too_long", "Keep the note under 1,000 characters.");
        var c = await LoadScopedAsync(id, ct, includeChildren: true);
        ComplaintAccess.Demand(user, c, Permissions.ComplaintChangeStatus);
        var feedback = c.Feedback.FirstOrDefault(f => Feedback.FeedbackRules.NeedsReview(f, c))
            ?? throw new DomainException("feedback.nothing_to_review", "There is no customer feedback waiting for review on this complaint.");

        feedback.ReviewedAt = clock.GetUtcNow();
        feedback.ReviewedBy = user.EmployeeId;
        feedback.ReviewedByName = user.Name;
        feedback.ReviewNote = note;
        audit.Log("REVIEW_FEEDBACK", Module, c.Id.ToString(), note is null ? null : "with note");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The latest feedback, for staff. Review is possible for whoever may change the complaint's status.</summary>
    private FeedbackInfo? ToFeedbackInfo(Complaint c)
    {
        var f = c.Feedback.OrderByDescending(x => x.SubmittedAt).FirstOrDefault();
        if (f is null) return null;
        var needsReview = Feedback.FeedbackRules.NeedsReview(f, c);
        return new FeedbackInfo(
            f.Resolved, f.Rating, f.Comment, f.SubmittedAt, needsReview,
            f.ReviewedBy is null ? null : new EmployeeRef(f.ReviewedBy, f.ReviewedByName), f.ReviewedAt, f.ReviewNote,
            needsReview && ComplaintAccess.Can(user, c, Permissions.ComplaintChangeStatus));
    }

    private static OrgRef SourceRef(string source) => new(source, ComplaintSources.Name(source));
}
