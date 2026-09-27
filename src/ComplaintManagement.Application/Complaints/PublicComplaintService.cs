using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Complaints;

/// <summary>Anonymous customer-facing operations. Never returns staff-only data.</summary>
public interface IPublicComplaintService
{
    /// <summary>Registers a complaint with optional supporting documents.</summary>
    Task<CreateComplaintResponse> RegisterAsync(PublicCreateComplaintRequest request, IReadOnlyList<UploadFile> files, string? ipAddress, CancellationToken ct);
    Task<PublicFormOptions> GetFormOptionsAsync(CancellationToken ct);
}

public sealed class PublicComplaintService(
    IApplicationDbContext db,
    IComplaintNumberGenerator numbers,
    IIamOrganisationService org,
    TimeProvider clock,
    IValidator<PublicCreateComplaintRequest> validator,
    AttachmentStore attachments,
    CustomerNotifier notifier) : IPublicComplaintService
{
    public const string CustomerActor = "CUSTOMER";

    public async Task<CreateComplaintResponse> RegisterAsync(PublicCreateComplaintRequest request, IReadOnlyList<UploadFile> files, string? ipAddress, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var branch = await org.FindBranchAsync(request.BranchCode.Trim(), ct);
        if (branch is not { IsActive: true })
            throw new DomainException("complaint.unknown_branch", "Please select a valid branch.");
        var sub = await db.SubCategories.Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Code == request.SubCategoryCode && s.IsActive
                && s.Category!.Code == request.CategoryCode && s.Category.IsActive, ct)
            ?? throw new DomainException("complaint.unknown_category", "Please select a valid complaint category.");
        var initial = await db.Statuses.FirstOrDefaultAsync(s => s.IsInitial && s.IsActive, ct)
            ?? throw new InvalidOperationException("No initial complaint status is configured.");
        var priority = sub.DefaultPriorityCode
            ?? await db.Priorities.Where(p => p.IsDefault && p.IsActive).Select(p => p.Code).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No default complaint priority is configured.");

        var defaultDepartment = sub.DefaultDepartmentCode is { } deptCode ? await org.FindDepartmentAsync(deptCode, ct) : null;

        // Documents are validated and stored before a number is issued, so a bad file does not burn a number.
        var complaintId = Guid.CreateVersion7();
        var documents = files.Count == 0 ? [] : await attachments.StoreAsync(complaintId, files, CustomerActor, "Customer", ct);

        var now = clock.GetUtcNow();
        var complaint = new Complaint
        {
            Id = complaintId,
            ComplaintNumber = await numbers.NextAsync(IstDate.ToIstDate(now).Year, ct),
            CustomerName = request.CustomerName.Trim(),
            MobileNumber = request.Mobile.Trim(),
            Email = NullIfBlank(request.Email),
            CustomerId = NullIfBlank(request.CustomerId),
            AccountNumber = NullIfBlank(request.AccountNumber),
            PreferredChannel = NullIfBlank(request.PreferredChannel),
            BranchCode = branch.Code,
            BranchName = branch.Name,
            RegionCode = branch.RegionCode,
            RegionName = branch.RegionName,
            CategoryId = sub.CategoryId,
            SubCategoryId = sub.Id,
            PriorityCode = priority,
            StatusCode = initial.Code,
            Description = request.Description.Trim(),
            TransactionId = NullIfBlank(request.TransactionId),
            TransactionDate = request.TransactionDate,
            TransactionAmount = request.Amount,
            AssignedDepartmentCode = defaultDepartment?.Code,
            AssignedDepartmentName = defaultDepartment?.Name,
            SlaDueDate = SlaCalculator.DueDate(now, sub.TatDays),
            CreatedAt = now,
            UpdatedAt = now,
        };
        complaint.StatusHistory.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            NewStatusCode = initial.Code,
            ChangedByEmployeeId = CustomerActor,
            ChangedByName = "Customer",
            Remarks = "Lodged through the Bank website",
            ChangedAt = now,
        });
        db.Complaints.Add(complaint);
        notifier.Registered(complaint);
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = CustomerActor,
            Action = "REGISTER",
            Module = "Complaint",
            RecordId = complaint.Id.ToString(),
            IpAddress = ipAddress,
            Details = documents.Count == 0 ? null : $"{documents.Count} document(s)",
            CreatedAt = now,
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await attachments.DiscardAsync(documents);
            throw;
        }

        return new CreateComplaintResponse(true, complaint.ComplaintNumber, "Complaint registered successfully.");
    }

    public async Task<PublicFormOptions> GetFormOptionsAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new PublicCategory(c.Code, c.Name, c.GroupName,
                c.SubCategories.Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                    .Select(s => new PublicSubCategory(s.Code, s.Name)).ToList()))
            .ToListAsync(ct);
        var branches = (await org.GetBranchesAsync(ct))
            .Where(b => b.IsActive)
            .OrderBy(b => b.RegionName).ThenBy(b => b.Name)
            .Select(b => new PublicBranch(b.Code, b.Name, b.RegionName))
            .ToList();
        return new PublicFormOptions(categories, branches);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
