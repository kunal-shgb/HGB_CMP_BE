using System.Text.RegularExpressions;
using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Admin;

/// <summary>
/// Admin configuration: categories and sub-categories, status labels, status transitions and approval
/// routing. Changes apply to new actions; existing complaints and open approvals keep what they had.
/// </summary>
public interface IAdminService
{
    Task<IReadOnlyList<AdminCategory>> GetCategoriesAsync(CancellationToken ct);
    Task CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct);
    Task UpdateCategoryAsync(string code, UpdateCategoryRequest request, CancellationToken ct);
    Task CreateSubCategoryAsync(string categoryCode, CreateSubCategoryRequest request, CancellationToken ct);
    Task UpdateSubCategoryAsync(string categoryCode, string subCode, UpdateSubCategoryRequest request, CancellationToken ct);

    Task<AdminWorkflow> GetWorkflowAsync(CancellationToken ct);
    Task UpdateStatusAsync(string code, UpdateStatusRequest request, CancellationToken ct);
    Task CreateTransitionAsync(CreateTransitionRequest request, CancellationToken ct);
    Task UpdateTransitionAsync(int id, UpdateTransitionRequest request, CancellationToken ct);
    Task UpdateApprovalSettingsAsync(UpdateApprovalSettingsRequest request, CancellationToken ct);
    Task UpdateEscalationSettingsAsync(UpdateEscalationSettingsRequest request, CancellationToken ct);
}

public sealed partial class AdminService(IApplicationDbContext db, IIamOrganisationService org, ICurrentUser user, IAuditLogger audit, TimeProvider clock) : IAdminService
{
    private const string Module = "Admin";

    [GeneratedRegex("^[A-Z0-9_]{2,40}$")]
    private static partial Regex CodePattern();

    // ---------- Categories ----------

    public async Task<IReadOnlyList<AdminCategory>> GetCategoriesAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().Include(c => c.SubCategories)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
        return categories.Select(c => new AdminCategory(
            c.Code, c.Name, c.GroupName, c.SortOrder, c.IsActive, c.Description,
            c.SubCategories.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).Select(s => new AdminSubCategory(
                s.Code, s.Name, s.TatDays, s.DefaultPriorityCode,
                s.DefaultDepartmentCode,
                s.SortOrder, s.IsActive)).ToList())).ToList();
    }

    public async Task CreateCategoryAsync(CreateCategoryRequest r, CancellationToken ct)
    {
        var code = NormaliseCode(r.Code);
        ValidateText(r.Name, "Name", 150);
        ValidateText(r.Group, "Group", 100);
        ValidateSort(r.SortOrder);
        ValidateOptional(r.Description, "Description", 500);
        if (await db.Categories.AnyAsync(c => c.Code == code, ct))
            throw new DomainException("admin.duplicate_code", $"A category with code {code} already exists.");

        var sort = r.SortOrder ?? ((await db.Categories.MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 10);
        var category = new ComplaintCategory
        {
            Code = code, Name = r.Name.Trim(), GroupName = r.Group.Trim(), SortOrder = sort, Description = Blank(r.Description),
        };
        // Every category starts with a "General" sub-category so it is usable on the complaint form straight away.
        category.SubCategories.Add(new ComplaintSubCategory { Code = "GENERAL", Name = "General", CategoryId = category.Id, SortOrder = 100 });
        db.Categories.Add(category);
        audit.Log("CREATE_CATEGORY", Module, code);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(string code, UpdateCategoryRequest r, CancellationToken ct)
    {
        ValidateText(r.Name, "Name", 150);
        ValidateText(r.Group, "Group", 100);
        ValidateSort(r.SortOrder);
        ValidateOptional(r.Description, "Description", 500);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Code == code, ct) ?? throw new NotFoundException("Category", code);

        category.Name = r.Name.Trim();
        category.GroupName = r.Group.Trim();
        category.SortOrder = r.SortOrder;
        category.IsActive = r.IsActive;
        category.Description = Blank(r.Description);
        audit.Log("UPDATE_CATEGORY", Module, code, r.IsActive ? null : "inactive");
        await db.SaveChangesAsync(ct);
    }

    public async Task CreateSubCategoryAsync(string categoryCode, CreateSubCategoryRequest r, CancellationToken ct)
    {
        var code = NormaliseCode(r.Code);
        ValidateText(r.Name, "Name", 150);
        ValidateTat(r.TatDays);
        ValidateSort(r.SortOrder);
        var category = await db.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Code == categoryCode, ct)
            ?? throw new NotFoundException("Category", categoryCode);
        if (category.SubCategories.Any(s => s.Code == code))
            throw new DomainException("admin.duplicate_code", $"{category.Name} already has a sub-category with code {code}.");

        db.SubCategories.Add(new ComplaintSubCategory
        {
            Code = code,
            Name = r.Name.Trim(),
            CategoryId = category.Id,
            TatDays = r.TatDays,
            DefaultPriorityCode = await ResolvePriorityAsync(r.DefaultPriorityCode, ct),
            DefaultDepartmentCode = await ResolveDepartmentAsync(r.DefaultDepartmentCode, ct),
            SortOrder = r.SortOrder ?? (category.SubCategories.Select(s => (int?)s.SortOrder).Max() ?? 0) + 10,
        });
        audit.Log("CREATE_SUBCATEGORY", Module, $"{categoryCode}/{code}");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSubCategoryAsync(string categoryCode, string subCode, UpdateSubCategoryRequest r, CancellationToken ct)
    {
        ValidateText(r.Name, "Name", 150);
        ValidateTat(r.TatDays);
        ValidateSort(r.SortOrder);
        var sub = await db.SubCategories.FirstOrDefaultAsync(s => s.Code == subCode && s.Category!.Code == categoryCode, ct)
            ?? throw new NotFoundException("Sub-category", $"{categoryCode}/{subCode}");

        sub.Name = r.Name.Trim();
        sub.TatDays = r.TatDays;
        sub.DefaultPriorityCode = await ResolvePriorityAsync(r.DefaultPriorityCode, ct);
        sub.DefaultDepartmentCode = await ResolveDepartmentAsync(r.DefaultDepartmentCode, ct);
        sub.SortOrder = r.SortOrder;
        sub.IsActive = r.IsActive;
        sub.UpdatedAt = clock.GetUtcNow();
        audit.Log("UPDATE_SUBCATEGORY", Module, $"{categoryCode}/{subCode}", $"tat={r.TatDays?.ToString() ?? "none"} active={r.IsActive}");
        await db.SaveChangesAsync(ct);
    }

    // ---------- Workflow ----------

    public async Task<AdminWorkflow> GetWorkflowAsync(CancellationToken ct)
    {
        var statuses = await db.Statuses.AsNoTracking().OrderBy(s => s.SortOrder)
            .Select(s => new AdminStatus(s.Code, s.Name, s.CustomerLabel, s.IsInitial, s.IsTerminal, s.IsResolution,
                s.IsAssignment, s.IsApprovalPending, s.SortOrder, s.IsActive))
            .ToListAsync(ct);
        var transitions = await db.StatusTransitions.AsNoTracking().OrderBy(t => t.Id)
            .Select(t => new AdminTransition(t.Id, t.FromStatusCode, t.ToStatusCode, t.RequiresRemark, t.RequiresApproval, t.IsActive))
            .ToListAsync(ct);
        var department = await db.AppSettings.AsNoTracking()
            .Where(s => s.Key == AppSettingKeys.HeadOfficeMakerCheckerDepartment).Select(s => s.Value).FirstOrDefaultAsync(ct);
        var departments = (await org.GetDepartmentsAsync(ct)).Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Code, d.Name)).ToList();
        var priorities = await db.Priorities.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Rank)
            .Select(p => new PriorityResponse(p.Code, p.Name, p.Rank)).ToListAsync(ct);

        var escalation = await Escalation.EscalationSettings.LoadAsync(db, ct);
        return new AdminWorkflow(statuses, transitions, new ApprovalSettings(department),
            new EscalationSettingsDto(escalation.Enabled, escalation.ToRegionalOfficeAfterDays, escalation.ToHeadOfficeAfterDays),
            departments, priorities);
    }

    public async Task UpdateStatusAsync(string code, UpdateStatusRequest r, CancellationToken ct)
    {
        ValidateText(r.Name, "Name", 100);
        ValidateText(r.CustomerLabel, "Customer label", 100);
        var status = await db.Statuses.FirstOrDefaultAsync(s => s.Code == code, ct) ?? throw new NotFoundException("Status", code);
        status.Name = r.Name.Trim();
        status.CustomerLabel = r.CustomerLabel.Trim();
        audit.Log("UPDATE_STATUS", Module, code);
        await db.SaveChangesAsync(ct);
    }

    public async Task CreateTransitionAsync(CreateTransitionRequest r, CancellationToken ct)
    {
        if (r.FromStatusCode == r.ToStatusCode)
            throw new DomainException("admin.transition_same_status", "A status cannot move to itself.");
        var statuses = await db.Statuses.AsNoTracking()
            .Where(s => s.Code == r.FromStatusCode || s.Code == r.ToStatusCode).ToDictionaryAsync(s => s.Code, ct);
        if (!statuses.TryGetValue(r.FromStatusCode, out var from) || !statuses.TryGetValue(r.ToStatusCode, out var to))
            throw new DomainException("admin.unknown_status", "Choose two existing statuses.");
        if (from.IsApprovalPending || to.IsApprovalPending)
            throw new DomainException("admin.system_status", $"\"{(from.IsApprovalPending ? from.Name : to.Name)}\" is managed by the approval process and cannot be used in manual moves.");
        if (await db.StatusTransitions.AnyAsync(t => t.FromStatusCode == r.FromStatusCode && t.ToStatusCode == r.ToStatusCode, ct))
            throw new DomainException("admin.duplicate_transition", $"A move from {from.Name} to {to.Name} already exists. Edit it instead.");

        var nextId = (await db.StatusTransitions.MaxAsync(t => (int?)t.Id, ct) ?? 0) + 1;
        db.StatusTransitions.Add(new ComplaintStatusTransition
        {
            Id = nextId,
            FromStatusCode = r.FromStatusCode,
            ToStatusCode = r.ToStatusCode,
            RequiresRemark = r.RequiresRemark || r.RequiresApproval,
            RequiresApproval = r.RequiresApproval,
        });
        audit.Log("CREATE_TRANSITION", Module, $"{r.FromStatusCode}->{r.ToStatusCode}", $"approval={r.RequiresApproval}");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateTransitionAsync(int id, UpdateTransitionRequest r, CancellationToken ct)
    {
        var t = await db.StatusTransitions.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Transition", id);
        // A decision a Checker must approve always needs the Maker's reasoning.
        t.RequiresRemark = r.RequiresRemark || r.RequiresApproval;
        t.RequiresApproval = r.RequiresApproval;
        t.IsActive = r.IsActive;
        audit.Log("UPDATE_TRANSITION", Module, $"{t.FromStatusCode}->{t.ToStatusCode}",
            $"remark={t.RequiresRemark} approval={t.RequiresApproval} active={t.IsActive}");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateApprovalSettingsAsync(UpdateApprovalSettingsRequest r, CancellationToken ct)
    {
        var department = Blank(r.HeadOfficeMakerCheckerDepartment);
        if (department is not null)
        {
            department = (await org.FindDepartmentAsync(department, ct)) is { IsActive: true } d
                ? d.Code
                : throw new DomainException("admin.unknown_department", "Choose an existing department.");
        }

        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.HeadOfficeMakerCheckerDepartment, ct);
        if (setting is null)
        {
            setting = new AppSetting { Key = AppSettingKeys.HeadOfficeMakerCheckerDepartment };
            db.AppSettings.Add(setting);
        }
        setting.Value = department;
        setting.UpdatedAt = clock.GetUtcNow();
        setting.UpdatedBy = user.EmployeeId;
        audit.Log("UPDATE_APPROVAL_SETTINGS", Module, AppSettingKeys.HeadOfficeMakerCheckerDepartment, department ?? "any HO checker");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateEscalationSettingsAsync(UpdateEscalationSettingsRequest r, CancellationToken ct)
    {
        if (r.ToRegionalOfficeAfterDays is < 0 or > 365 || r.ToHeadOfficeAfterDays is < 0 or > 365)
            throw new DomainException("admin.invalid_escalation", "Escalation days must be between 0 and 365.");
        if (r.ToHeadOfficeAfterDays < r.ToRegionalOfficeAfterDays)
            throw new DomainException("admin.invalid_escalation", "Escalation to Head Office cannot come before escalation to the Regional Office.");

        await SetAsync(AppSettingKeys.EscalationEnabled, r.Enabled ? "true" : "false", ct);
        await SetAsync(AppSettingKeys.EscalateToRegionalOfficeAfterDays, r.ToRegionalOfficeAfterDays.ToString(), ct);
        await SetAsync(AppSettingKeys.EscalateToHeadOfficeAfterDays, r.ToHeadOfficeAfterDays.ToString(), ct);
        audit.Log("UPDATE_ESCALATION_SETTINGS", Module, "escalation",
            $"enabled={r.Enabled} ro={r.ToRegionalOfficeAfterDays}d ho={r.ToHeadOfficeAfterDays}d");
        await db.SaveChangesAsync(ct);
    }

    private async Task SetAsync(string key, string? value, CancellationToken ct)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            setting = new AppSetting { Key = key };
            db.AppSettings.Add(setting);
        }
        setting.Value = value;
        setting.UpdatedAt = clock.GetUtcNow();
        setting.UpdatedBy = user.EmployeeId;
    }

    // ---------- helpers ----------

    private static string NormaliseCode(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(c))
            throw new DomainException("admin.invalid_code", "Code must be 2–40 characters: capital letters, digits and underscores.");
        return c;
    }

    private static void ValidateText(string? value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("admin.required", $"{field} is required.");
        if (value.Trim().Length > max) throw new DomainException("admin.too_long", $"{field} must be at most {max} characters.");
    }

    private static void ValidateOptional(string? value, string field, int max)
    {
        if (value is not null && value.Trim().Length > max) throw new DomainException("admin.too_long", $"{field} must be at most {max} characters.");
    }

    private static void ValidateTat(int? days)
    {
        if (days is not null && (days < 1 || days > 365)) throw new DomainException("admin.invalid_tat", "TAT must be between 1 and 365 days.");
    }

    private static void ValidateSort(int? sort)
    {
        if (sort is not null && (sort < 0 || sort > 100_000)) throw new DomainException("admin.invalid_sort", "Sort order must be between 0 and 100,000.");
    }

    private async Task<string?> ResolvePriorityAsync(string? code, CancellationToken ct)
    {
        var c = Blank(code);
        if (c is null) return null;
        return await db.Priorities.AnyAsync(p => p.Code == c && p.IsActive, ct)
            ? c
            : throw new DomainException("admin.unknown_priority", "Choose an existing priority.");
    }

    private async Task<string?> ResolveDepartmentAsync(string? code, CancellationToken ct)
    {
        var c = Blank(code);
        if (c is null) return null;
        return (await org.FindDepartmentAsync(c, ct)) is { IsActive: true } d
            ? d.Code
            : throw new DomainException("admin.unknown_department", "Choose an existing department.");
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
