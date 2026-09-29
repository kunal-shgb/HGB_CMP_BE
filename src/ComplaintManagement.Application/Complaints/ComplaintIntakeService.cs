using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Reference;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Complaints;

/// <summary>
/// Staff lodging a complaint on a customer's behalf (walk-in, phone, letter). Any employee may do it; no OTP.
/// Branch staff lodge for their own branch; Regional and Head Office staff choose the branch.
/// </summary>
public interface IComplaintIntakeService
{
    Task<LodgeFormOptions> GetFormOptionsAsync(CancellationToken ct);
    Task<LodgeComplaintResponse> LodgeAsync(StaffCreateComplaintRequest request, IReadOnlyList<UploadFile> files, CancellationToken ct);
}

public sealed class ComplaintIntakeService(
    IApplicationDbContext db,
    ICurrentUser user,
    IAuditLogger audit,
    IIamOrganisationService org,
    IValidator<StaffCreateComplaintRequest> validator,
    ComplaintRegistrar registrar) : IComplaintIntakeService
{
    public async Task<LodgeFormOptions> GetFormOptionsAsync(CancellationToken ct)
    {
        var source = SourceFor(user);
        var categories = await db.Categories.AsNoTracking()
            .UsableOnForms()
            .Select(c => new PublicCategory(c.Code, c.Name, c.Group!.Name))
            .ToListAsync(ct);

        if (user.ScopeLevel == ScopeLevel.Branch)
        {
            var own = await OwnBranchAsync(ct);
            return new LodgeFormOptions(categories, [], [], new BranchResponse(own.Code, own.Name, own.RegionCode), source);
        }

        var regions = (await org.GetRegionsAsync(ct)).Where(r => r.IsActive).OrderBy(r => r.Name)
            .Select(r => new RegionResponse(r.Code, r.Name)).ToList();
        var branches = (await org.GetBranchesAsync(ct)).Where(b => b.IsActive).OrderBy(b => b.Name)
            .Select(b => new BranchResponse(b.Code, b.Name, b.RegionCode)).ToList();
        return new LodgeFormOptions(categories, regions, branches, null, source);
    }

    public async Task<LodgeComplaintResponse> LodgeAsync(StaffCreateComplaintRequest request, IReadOnlyList<UploadFile> files, CancellationToken ct)
    {
        var source = SourceFor(user);
        await validator.ValidateAndThrowAsync(request, ct);

        IamBranch branch;
        if (user.ScopeLevel == ScopeLevel.Branch)
        {
            branch = await OwnBranchAsync(ct);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.BranchCode))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.BranchCode), "Select the customer's branch.")]);
            var chosen = await org.FindBranchAsync(request.BranchCode.Trim(), ct);
            branch = chosen is { IsActive: true }
                ? chosen
                : throw new DomainException("complaint.unknown_branch", "Please select a valid branch.");
        }

        var actor = new IntakeActor(user.EmployeeId, user.Name, source,
            $"Lodged at {user.OfficeName ?? ComplaintSources.Name(source)} on the customer's behalf",
            user.EmployeeId, user.OfficeName);
        var (complaint, documents) = await registrar.StageAsync(request, branch, actor, files, ct);
        audit.Log("LODGE", "Complaint", complaint.Id.ToString(),
            $"source={source} branch={branch.Code}" + (documents.Count == 0 ? "" : $" documents={documents.Count}"));
        await registrar.SaveAsync(documents, ct);

        var canOpen = user.ScopeLevel switch
        {
            ScopeLevel.HeadOffice or ScopeLevel.Branch => true,
            ScopeLevel.Region => branch.RegionCode == user.OfficeCode,
            _ => false,
        };
        return new LodgeComplaintResponse(complaint.Id, complaint.ComplaintNumber, canOpen);
    }

    /// <summary>The complaint source follows the office the employee works at. Unrecognised offices cannot lodge.</summary>
    public static string SourceFor(ICurrentUser user) => user.ScopeLevel switch
    {
        ScopeLevel.Branch when user.OfficeCode is not null => ComplaintSources.Branch,
        ScopeLevel.Region when user.OfficeCode is not null => ComplaintSources.RegionalOffice,
        ScopeLevel.HeadOffice => ComplaintSources.HeadOffice,
        _ => throw new ForbiddenAccessException("Your office is not recognised, so you cannot lodge complaints."),
    };

    private async Task<IamBranch> OwnBranchAsync(CancellationToken ct) =>
        await org.FindBranchAsync(user.OfficeCode!, ct) is { IsActive: true } b
            ? b
            : throw new DomainException("complaint.own_branch_unknown", "Your branch was not found in the Bank's records. Please contact the IT helpdesk.");
}
