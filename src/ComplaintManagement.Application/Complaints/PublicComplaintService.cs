using ComplaintManagement.Application.Common;
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
    Task<CreateComplaintResponse> RegisterAsync(PublicCreateComplaintRequest request, string? ipAddress, CancellationToken ct);
}

public sealed class PublicComplaintService(
    IApplicationDbContext db,
    IComplaintNumberGenerator numbers,
    TimeProvider clock,
    IValidator<PublicCreateComplaintRequest> validator) : IPublicComplaintService
{
    public const string CustomerActor = "CUSTOMER";

    public async Task<CreateComplaintResponse> RegisterAsync(PublicCreateComplaintRequest request, string? ipAddress, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == request.BranchCode && b.IsActive, ct)
            ?? throw new DomainException("complaint.unknown_branch", "Please select a valid branch.");
        var sub = await db.SubCategories.Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Code == request.SubCategoryCode && s.IsActive
                && s.Category!.Code == request.CategoryCode && s.Category.IsActive, ct)
            ?? throw new DomainException("complaint.unknown_category", "Please select a valid complaint category.");
        var initial = await db.Statuses.FirstOrDefaultAsync(s => s.IsInitial && s.IsActive, ct)
            ?? throw new InvalidOperationException("No initial complaint status is configured.");
        var priority = sub.DefaultPriorityCode
            ?? await db.Priorities.Where(p => p.IsDefault && p.IsActive).Select(p => p.Code).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No default complaint priority is configured.");

        var now = clock.GetUtcNow();
        var complaint = new Complaint
        {
            ComplaintNumber = await numbers.NextAsync(IstDate.ToIstDate(now).Year, ct),
            CustomerName = request.CustomerName.Trim(),
            MobileNumber = request.Mobile.Trim(),
            Email = NullIfBlank(request.Email),
            CustomerId = NullIfBlank(request.CustomerId),
            AccountNumber = NullIfBlank(request.AccountNumber),
            PreferredChannel = NullIfBlank(request.PreferredChannel),
            BranchId = branch.Id,
            CategoryId = sub.CategoryId,
            SubCategoryId = sub.Id,
            PriorityCode = priority,
            StatusCode = initial.Code,
            Description = request.Description.Trim(),
            TransactionId = NullIfBlank(request.TransactionId),
            TransactionDate = request.TransactionDate,
            TransactionAmount = request.Amount,
            AssignedDepartmentId = sub.DefaultDepartmentId,
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
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = CustomerActor,
            Action = "REGISTER",
            Module = "Complaint",
            RecordId = complaint.Id.ToString(),
            IpAddress = ipAddress,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        return new CreateComplaintResponse(true, complaint.ComplaintNumber, "Complaint registered successfully.");
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
