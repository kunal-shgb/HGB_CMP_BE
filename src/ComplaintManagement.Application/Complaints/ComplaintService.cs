using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
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
    Task ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct);
    Task AssignAsync(Guid id, AssignComplaintRequest request, CancellationToken ct);
    Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, CancellationToken ct);
}

public sealed class ComplaintService(
    IApplicationDbContext db,
    ICurrentUser user,
    IAuditLogger audit,
    IIamUserService iam,
    TimeProvider clock,
    IOptions<SlaOptions> slaOptions,
    IValidator<ComplaintFilterRequest> filterValidator,
    IValidator<ChangeStatusRequest> statusValidator,
    IValidator<AssignComplaintRequest> assignValidator,
    IValidator<AddRemarkRequest> remarkValidator) : IComplaintService
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
                c.Id, c.ComplaintNumber, c.CustomerName, c.MobileNumber, c.CreatedAt, c.SlaDueDate, c.ClosedAt,
                c.AssignedEmployeeId, c.AssignedEmployeeName,
                Branch = new OrgRef(c.Branch!.Code, c.Branch.Name),
                Region = new OrgRef(c.Branch.Region!.Code, c.Branch.Region.Name),
                Category = new OrgRef(c.Category!.Code, c.Category.Name),
                SubCategory = new OrgRef(c.SubCategory!.Code, c.SubCategory.Name),
                Status = new StatusRef(c.Status!.Code, c.Status.Name, c.Status.IsTerminal),
                Priority = new OrgRef(c.Priority!.Code, c.Priority.Name),
            })
            .ToListAsync(ct);

        var now = clock.GetUtcNow();
        var items = rows.Select(r => new ComplaintListItem(
            r.Id, r.ComplaintNumber, r.CustomerName, Masking.Mobile(r.MobileNumber),
            r.Branch, r.Region, r.Category, r.SubCategory, r.Status, r.Priority,
            r.AssignedEmployeeId is null ? null : new EmployeeRef(r.AssignedEmployeeId, r.AssignedEmployeeName),
            BuildSla(r.CreatedAt, r.SlaDueDate, r.ClosedAt, now),
            r.CreatedAt)).ToList();

        audit.Log("LIST", Module, null, $"page={filter.Page}");
        await db.SaveChangesAsync(ct);

        return new PagedResponse<ComplaintListItem>(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ComplaintDetail> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await LoadScopedAsync(id, ct, includeChildren: true);
        var unmasked = user.HasPermission(Permissions.ComplaintViewUnmasked);
        var now = clock.GetUtcNow();

        var transitions = await (
                from t in db.StatusTransitions.AsNoTracking()
                join s in db.Statuses.AsNoTracking() on t.ToStatusCode equals s.Code
                where t.IsActive && s.IsActive && t.FromStatusCode == c.StatusCode
                orderby s.SortOrder
                select new AllowedTransition(s.Code, s.Name, t.RequiresRemark))
            .ToListAsync(ct);

        if (!user.HasPermission(Permissions.ComplaintChangeStatus)) transitions = [];

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
            c.Description,
            new OrgRef(c.Branch!.Code, c.Branch.Name),
            new OrgRef(c.Branch.Region!.Code, c.Branch.Region.Name),
            new OrgRef(c.Category!.Code, c.Category.Name),
            new OrgRef(c.SubCategory!.Code, c.SubCategory.Name),
            new StatusRef(c.Status!.Code, c.Status.Name, c.Status.IsTerminal),
            new OrgRef(c.Priority!.Code, c.Priority.Name),
            c.AssignedEmployeeId is null ? null : new EmployeeRef(c.AssignedEmployeeId, c.AssignedEmployeeName),
            c.AssignedDepartment is null ? null : new OrgRef(c.AssignedDepartment.Code, c.AssignedDepartment.Name),
            c.EscalationLevel,
            BuildSla(c.CreatedAt, c.SlaDueDate, c.ClosedAt, now),
            c.CreatedAt,
            c.UpdatedAt,
            c.ResolvedAt,
            c.ClosedAt,
            transitions,
            c.Remarks.OrderByDescending(r => r.CreatedAt).Select(ToRemarkItem).ToList(),
            c.Attachments.OrderBy(a => a.UploadedAt)
                .Select(a => new AttachmentItem(a.Id, a.FileName, a.ContentType, a.FileSize, a.UploadedAt)).ToList());
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
        events.AddRange(c.Attachments.Select(a => new TimelineEvent(
            a.UploadedAt, "ATTACHMENT", "Attachment uploaded", a.FileName, new EmployeeRef(a.UploadedBy, null))));

        return events.OrderByDescending(e => e.At).ToList();
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);

        var transition = await db.StatusTransitions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.FromStatusCode == c.StatusCode && t.ToStatusCode == request.NewStatus, ct)
            ?? throw new DomainException("status.transition_not_allowed",
                $"A complaint in status {c.StatusCode} cannot be moved to {request.NewStatus}.");

        if (transition.RequiresRemark && string.IsNullOrWhiteSpace(request.Remarks))
            throw new DomainException("status.remark_required", "A remark is required for this status change.");

        var target = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == request.NewStatus, ct);
        ApplyStatus(c, target, request.Remarks?.Trim());

        audit.Log("CHANGE_STATUS", Module, c.Id.ToString(), $"{transition.FromStatusCode}->{target.Code}");
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignAsync(Guid id, AssignComplaintRequest request, CancellationToken ct)
    {
        await assignValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);

        var status = await db.Statuses.AsNoTracking().SingleAsync(s => s.Code == c.StatusCode, ct);
        if (status.IsTerminal)
            throw new DomainException("assign.terminal_status", "A closed complaint cannot be assigned. Reopen it first.");

        var assignee = await iam.GetUserAsync(request.AssignedToEmployeeId, ct)
            ?? throw new DomainException("assign.unknown_employee", "The selected employee was not found in the Bank directory.");
        if (!ComplaintScope.CanTarget(user, assignee))
            throw new ForbiddenAccessException("You can only assign complaints to employees within your office.");

        Department? department = null;
        if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            department = await db.Departments.FirstOrDefaultAsync(d => d.Code == request.DepartmentCode && d.IsActive, ct)
                ?? throw new DomainException("assign.unknown_department", "The selected department was not found.");
        }

        var now = clock.GetUtcNow();
        db.ComplaintAssignments.Add(new ComplaintAssignment
        {
            ComplaintId = c.Id,
            AssignedFromEmployeeId = c.AssignedEmployeeId,
            AssignedToEmployeeId = assignee.EmployeeId,
            AssignedToName = assignee.Name,
            AssignedDepartmentId = department?.Id ?? c.AssignedDepartmentId,
            Remarks = request.Remarks?.Trim(),
            AssignedByEmployeeId = user.EmployeeId,
            AssignedAt = now,
        });
        c.AssignedEmployeeId = assignee.EmployeeId;
        c.AssignedEmployeeName = assignee.Name;
        if (department is not null) c.AssignedDepartmentId = department.Id;

        // Move to the workflow's "assigned" status when the current status allows it.
        var assignedStatus = await (
                from t in db.StatusTransitions.AsNoTracking()
                join s in db.Statuses.AsNoTracking() on t.ToStatusCode equals s.Code
                where t.IsActive && t.FromStatusCode == c.StatusCode && s.IsAssignment && !t.RequiresRemark
                select s)
            .FirstOrDefaultAsync(ct);
        if (assignedStatus is not null) ApplyStatus(c, assignedStatus, request.Remarks?.Trim());

        audit.Log("ASSIGN", Module, c.Id.ToString(), $"to={assignee.EmployeeId}");
        await db.SaveChangesAsync(ct);
    }

    public async Task<RemarkItem> AddRemarkAsync(Guid id, AddRemarkRequest request, CancellationToken ct)
    {
        await remarkValidator.ValidateAndThrowAsync(request, ct);
        var c = await LoadScopedAsync(id, ct, includeChildren: false);

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

        audit.Log("ADD_REMARK", Module, c.Id.ToString(), remark.Visibility.ToString());
        await db.SaveChangesAsync(ct);
        return ToRemarkItem(remark);
    }

    private void ApplyStatus(Complaint c, ComplaintStatus target, string? remarks)
    {
        var now = clock.GetUtcNow();
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
    }

    private async Task<Complaint> LoadScopedAsync(Guid id, CancellationToken ct, bool includeChildren)
    {
        IQueryable<Complaint> query = db.Complaints
            .Include(c => c.Branch!).ThenInclude(b => b.Region)
            .Include(c => c.Category)
            .Include(c => c.SubCategory)
            .Include(c => c.Status)
            .Include(c => c.Priority)
            .Include(c => c.AssignedDepartment);

        if (includeChildren)
        {
            query = query
                .Include(c => c.StatusHistory)
                .Include(c => c.Assignments)
                .Include(c => c.Remarks)
                .Include(c => c.Attachments);
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

    private static RemarkItem ToRemarkItem(ComplaintRemark r) => new(
        r.Id, r.Remark, r.Visibility.ToString().ToUpperInvariant(),
        new EmployeeRef(r.CreatedByEmployeeId, r.CreatedByName), r.CreatedAt);

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
        if (Clean(f.BranchCode) is { } branch) q = q.Where(c => c.Branch!.Code == branch);
        if (Clean(f.RegionCode) is { } region) q = q.Where(c => c.Branch!.Region!.Code == region);
        if (Clean(f.CategoryCode) is { } cat) q = q.Where(c => c.Category!.Code == cat);
        if (Clean(f.SubCategoryCode) is { } sub) q = q.Where(c => c.SubCategory!.Code == sub);
        if (Clean(f.Status) is { } status) q = q.Where(c => c.StatusCode == status);
        if (Clean(f.Priority) is { } priority) q = q.Where(c => c.PriorityCode == priority);
        if (Clean(f.AssignedEmployeeId) is { } emp) q = q.Where(c => c.AssignedEmployeeId == emp);
        if (f.FromDate is { } from) q = q.Where(c => c.CreatedAt >= IstDate.StartOfDayUtc(from));
        if (f.ToDate is { } to) q = q.Where(c => c.CreatedAt < IstDate.StartOfDayUtc(to.AddDays(1)));
        if (f.OverdueOnly == true) q = q.Where(c => c.ClosedAt == null && c.SlaDueDate != null && c.SlaDueDate < now);
        return q;
    }
}
