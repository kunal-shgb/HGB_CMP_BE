using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Reference;
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
    IIamOrganisationService org,
    TimeProvider clock,
    IValidator<PublicCreateComplaintRequest> validator,
    ComplaintRegistrar registrar) : IPublicComplaintService
{
    public const string CustomerActor = "CUSTOMER";

    public async Task<CreateComplaintResponse> RegisterAsync(PublicCreateComplaintRequest request, IReadOnlyList<UploadFile> files, string? ipAddress, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var branch = await org.FindBranchAsync(request.BranchCode.Trim(), ct);
        if (branch is not { IsActive: true })
            throw new DomainException("complaint.unknown_branch", "Please select a valid branch.");

        var (complaint, documents) = await registrar.StageAsync(request, branch,
            new IntakeActor(CustomerActor, "Customer", ComplaintSources.Website, "Lodged through the Bank website"), files, ct);
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = CustomerActor,
            Action = "REGISTER",
            Module = "Complaint",
            RecordId = complaint.Id.ToString(),
            IpAddress = ipAddress,
            Details = documents.Count == 0 ? null : $"{documents.Count} document(s)",
            CreatedAt = clock.GetUtcNow(),
        });
        await registrar.SaveAsync(documents, ct);

        return new CreateComplaintResponse(true, complaint.ComplaintNumber, "Complaint registered successfully.");
    }

    public async Task<PublicFormOptions> GetFormOptionsAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .UsableOnForms()
            .Select(c => new PublicCategory(c.Code, c.Name, c.Group!.Name))
            .ToListAsync(ct);
        var branches = (await org.GetBranchesAsync(ct))
            .Where(b => b.IsActive)
            .OrderBy(b => b.RegionName).ThenBy(b => b.Name)
            .Select(b => new PublicBranch(b.Code, b.Name, b.RegionName))
            .ToList();
        return new PublicFormOptions(categories, branches);
    }
}
