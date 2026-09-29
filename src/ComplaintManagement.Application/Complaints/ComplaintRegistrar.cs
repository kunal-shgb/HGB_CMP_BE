using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Complaints;

/// <summary>Who lodged a complaint and how. Staff fields are null for the customer's own website complaints.</summary>
public sealed record IntakeActor(string ActorId, string ActorName, string Source, string HistoryRemark,
    string? StaffEmployeeId = null, string? StaffOfficeName = null);

/// <summary>
/// Builds and stages a new complaint: category, initial status, priority, default department, SLA,
/// supporting documents, the first history entry and the customer's acknowledgement. The caller adds its
/// audit record and saves through <see cref="SaveAsync"/>.
/// </summary>
public sealed class ComplaintRegistrar(
    IApplicationDbContext db,
    IComplaintNumberGenerator numbers,
    IIamOrganisationService org,
    TimeProvider clock,
    AttachmentStore attachments,
    CustomerNotifier notifier)
{
    public async Task<(Complaint Complaint, IReadOnlyList<ComplaintAttachment> Documents)> StageAsync(
        IComplaintIntakeDetails d, IamBranch branch, IntakeActor actor, IReadOnlyList<UploadFile> files, CancellationToken ct)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Code == d.CategoryCode && c.IsActive && c.Group!.IsActive, ct)
            ?? throw new DomainException("complaint.unknown_category", "Please select a valid complaint category.");
        var initial = await db.Statuses.FirstOrDefaultAsync(s => s.IsInitial && s.IsActive, ct)
            ?? throw new InvalidOperationException("No initial complaint status is configured.");
        var priority = category.DefaultPriorityCode
            ?? await db.Priorities.Where(p => p.IsDefault && p.IsActive).Select(p => p.Code).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No default complaint priority is configured.");

        var defaultDepartment = category.DefaultDepartmentCode is { } deptCode ? await org.FindDepartmentAsync(deptCode, ct) : null;

        // Documents are validated and stored before a number is issued, so a bad file does not burn a number.
        var complaintId = Guid.CreateVersion7();
        var documents = files.Count == 0 ? [] : await attachments.StoreAsync(complaintId, files, actor.ActorId, actor.ActorName, ct);

        var now = clock.GetUtcNow();
        var complaint = new Complaint
        {
            Id = complaintId,
            ComplaintNumber = await numbers.NextAsync(IstDate.ToIstDate(now).Year, ct),
            CustomerName = d.CustomerName.Trim(),
            MobileNumber = d.Mobile.Trim(),
            Email = NullIfBlank(d.Email),
            CustomerId = NullIfBlank(d.CustomerId),
            AccountNumber = NullIfBlank(d.AccountNumber),
            PreferredChannel = NullIfBlank(d.PreferredChannel),
            Source = actor.Source,
            LodgedByEmployeeId = actor.StaffEmployeeId,
            LodgedByName = actor.StaffEmployeeId is null ? null : actor.ActorName,
            LodgedByOfficeName = actor.StaffOfficeName,
            BranchCode = branch.Code,
            BranchName = branch.Name,
            RegionCode = branch.RegionCode,
            RegionName = branch.RegionName,
            CategoryId = category.Id,
            PriorityCode = priority,
            StatusCode = initial.Code,
            Title = d.Title.Trim(),
            Description = d.Description.Trim(),
            TransactionId = NullIfBlank(d.TransactionId),
            TransactionDate = d.TransactionDate,
            TransactionAmount = d.Amount,
            AssignedDepartmentCode = defaultDepartment?.Code,
            AssignedDepartmentName = defaultDepartment?.Name,
            SlaDueDate = SlaCalculator.DueDate(now, category.TatDays),
            CreatedAt = now,
            UpdatedAt = now,
        };
        complaint.StatusHistory.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            NewStatusCode = initial.Code,
            ChangedByEmployeeId = actor.ActorId,
            ChangedByName = actor.ActorName,
            Remarks = actor.HistoryRemark,
            ChangedAt = now,
        });
        db.Complaints.Add(complaint);
        notifier.Registered(complaint);
        return (complaint, documents);
    }

    /// <summary>Saves the unit of work, removing the stored documents if the save fails.</summary>
    public async Task SaveAsync(IReadOnlyList<ComplaintAttachment> documents, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await attachments.DiscardAsync(documents);
            throw;
        }
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
