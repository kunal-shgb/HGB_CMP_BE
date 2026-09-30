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
/// Admin configuration: category groups and categories (with TAT, default priority and escalation route), status labels, status transitions and approval
/// routing. Changes apply to new actions; existing complaints and open approvals keep what they had.
/// </summary>
public interface IAdminService
{
    Task<AdminCategoryCatalogue> GetCategoriesAsync(CancellationToken ct);
    Task CreateGroupAsync(CreateCategoryGroupRequest request, CancellationToken ct);
    Task UpdateGroupAsync(string code, UpdateCategoryGroupRequest request, CancellationToken ct);
    Task DeleteGroupAsync(string code, CancellationToken ct);
    /// <summary>Sets the order of all groups on complaint forms.</summary>
    Task ReorderGroupsAsync(ReorderRequest request, CancellationToken ct);
    /// <summary>Sets the order of the categories within one group.</summary>
    Task ReorderCategoriesAsync(string groupCode, ReorderRequest request, CancellationToken ct);
    Task CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct);
    Task UpdateCategoryAsync(string code, UpdateCategoryRequest request, CancellationToken ct);
    Task DeleteCategoryAsync(string code, CancellationToken ct);

    Task<AdminWorkflow> GetWorkflowAsync(CancellationToken ct);
    Task UpdateStatusAsync(string code, UpdateStatusRequest request, CancellationToken ct);
    Task CreateTransitionAsync(CreateTransitionRequest request, CancellationToken ct);
    Task UpdateTransitionAsync(int id, UpdateTransitionRequest request, CancellationToken ct);
    Task UpdateApprovalSettingsAsync(UpdateApprovalSettingsRequest request, CancellationToken ct);
    Task UpdateEscalationSettingsAsync(UpdateEscalationSettingsRequest request, CancellationToken ct);
    Task UpdateFeedbackSettingsAsync(UpdateFeedbackSettingsRequest request, CancellationToken ct);
}

public sealed partial class AdminService(IApplicationDbContext db, IIamOrganisationService org, ICurrentUser user, IAuditLogger audit, TimeProvider clock) : IAdminService
{
    private const string Module = "Admin";

    [GeneratedRegex("^[A-Z0-9_]{2,40}$")]
    private static partial Regex CodePattern();

    // ---------- Groups and categories ----------
    // Removing something is a hard delete and is allowed only while nothing depends on it; otherwise the
    // admin hides it (IsActive = false), so existing complaints keep their category and history.

    public async Task<AdminCategoryCatalogue> GetCategoriesAsync(CancellationToken ct)
    {
        var groups = await db.CategoryGroups.AsNoTracking()
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Select(g => new AdminCategoryGroup(g.Code, g.Name, g.SortOrder, g.IsActive, g.Categories.Count))
            .ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().Include(c => c.Group)
            .OrderBy(c => c.Group!.SortOrder).ThenBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
        var used = await db.Complaints.AsNoTracking().GroupBy(c => c.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return new AdminCategoryCatalogue(groups, categories.Select(c => new AdminCategory(
            c.Code, c.Name, c.Group!.Code, c.Group.Name, c.TatDays, c.DefaultPriorityCode,
            c.SortOrder, c.IsActive, c.Description, used.GetValueOrDefault(c.Id),
            c.RoDivisionCode, c.HoDivisionCode, c.DirectToHeadOffice)).ToList());
    }

    public async Task CreateGroupAsync(CreateCategoryGroupRequest r, CancellationToken ct)
    {
        var code = NormaliseCode(r.Code);
        ValidateText(r.Name, "Name", 100);
        ValidateSort(r.SortOrder);
        if (await db.CategoryGroups.AnyAsync(g => g.Code == code, ct))
            throw new DomainException("admin.duplicate_code", $"A group with code {code} already exists.");
        await EnsureUniqueGroupNameAsync(r.Name, null, ct);

        var now = clock.GetUtcNow();
        db.CategoryGroups.Add(new ComplaintCategoryGroup
        {
            Code = code,
            Name = r.Name.Trim(),
            SortOrder = r.SortOrder ?? ((await db.CategoryGroups.MaxAsync(g => (int?)g.SortOrder, ct) ?? 0) + 10),
            CreatedAt = now,
            UpdatedAt = now,
        });
        audit.Log("CREATE_CATEGORY_GROUP", Module, code);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateGroupAsync(string code, UpdateCategoryGroupRequest r, CancellationToken ct)
    {
        ValidateText(r.Name, "Name", 100);
        ValidateSort(r.SortOrder);
        var group = await db.CategoryGroups.FirstOrDefaultAsync(g => g.Code == code, ct) ?? throw new NotFoundException("Group", code);
        await EnsureUniqueGroupNameAsync(r.Name, group.Id, ct);

        group.Name = r.Name.Trim();
        group.SortOrder = r.SortOrder;
        group.IsActive = r.IsActive;
        group.UpdatedAt = clock.GetUtcNow();
        audit.Log("UPDATE_CATEGORY_GROUP", Module, code, r.IsActive ? null : "inactive");
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteGroupAsync(string code, CancellationToken ct)
    {
        var group = await db.CategoryGroups.FirstOrDefaultAsync(g => g.Code == code, ct) ?? throw new NotFoundException("Group", code);
        var categories = await db.Categories.CountAsync(c => c.GroupId == group.Id, ct);
        if (categories > 0)
            throw new DomainException("admin.group_in_use",
                $"\"{group.Name}\" still has {categories} {(categories == 1 ? "category" : "categories")}. Move or delete them first, or hide the group instead.");

        db.CategoryGroups.Remove(group);
        audit.Log("DELETE_CATEGORY_GROUP", Module, code);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderGroupsAsync(ReorderRequest r, CancellationToken ct)
    {
        var groups = await db.CategoryGroups.ToListAsync(ct);
        ApplyOrder(groups, g => g.Code, (g, order) => g.SortOrder = order, r.Codes, "group");
        var now = clock.GetUtcNow();
        foreach (var g in groups) g.UpdatedAt = now;
        audit.Log("REORDER_CATEGORY_GROUPS", Module, null, string.Join(",", r.Codes));
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderCategoriesAsync(string groupCode, ReorderRequest r, CancellationToken ct)
    {
        var group = await db.CategoryGroups.FirstOrDefaultAsync(g => g.Code == groupCode, ct) ?? throw new NotFoundException("Group", groupCode);
        var categories = await db.Categories.Where(c => c.GroupId == group.Id).ToListAsync(ct);
        ApplyOrder(categories, c => c.Code, (c, order) => c.SortOrder = order, r.Codes, "category");
        var now = clock.GetUtcNow();
        foreach (var c in categories) c.UpdatedAt = now;
        audit.Log("REORDER_CATEGORIES", Module, groupCode, string.Join(",", r.Codes));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Numbers the items 10, 20, 30... in the requested order. The request must list every item exactly once,
    /// so a stale screen cannot drop or duplicate one.
    /// </summary>
    private static void ApplyOrder<T>(IReadOnlyList<T> items, Func<T, string> code, Action<T, int> setOrder, IReadOnlyList<string>? requested, string what)
    {
        var codes = (requested ?? []).Select(c => (c ?? "").Trim()).ToList();
        if (codes.Count != items.Count || codes.Distinct(StringComparer.Ordinal).Count() != codes.Count
            || !items.Select(code).ToHashSet(StringComparer.Ordinal).SetEquals(codes))
            throw new DomainException("admin.stale_order", $"The {what} list has changed since you opened it. Reload the page and try again.");
        var byCode = items.ToDictionary(code, StringComparer.Ordinal);
        for (var i = 0; i < codes.Count; i++) setOrder(byCode[codes[i]], (i + 1) * 10);
    }

    public async Task CreateCategoryAsync(CreateCategoryRequest r, CancellationToken ct)
    {
        var code = NormaliseCode(r.Code);
        ValidateText(r.Name, "Name", 150);
        ValidateTat(r.TatDays);
        ValidateSort(r.SortOrder);
        ValidateOptional(r.Description, "Description", 500);
        var group = await ResolveGroupAsync(r.GroupCode, ct);
        if (await db.Categories.AnyAsync(c => c.Code == code, ct))
            throw new DomainException("admin.duplicate_code", $"A category with code {code} already exists.");

        var now = clock.GetUtcNow();
        db.Categories.Add(new ComplaintCategory
        {
            Code = code,
            Name = r.Name.Trim(),
            GroupId = group.Id,
            TatDays = r.TatDays,
            DefaultPriorityCode = await ResolvePriorityAsync(r.DefaultPriorityCode, ct),
            // A category that skips the RO has no RO division.
            RoDivisionCode = r.DirectToHeadOffice ? null : await ResolveDepartmentAsync(r.RoDivisionCode, ct),
            HoDivisionCode = await ResolveDepartmentAsync(r.HoDivisionCode, ct),
            DirectToHeadOffice = r.DirectToHeadOffice,
            // New categories go to the end of their group.
            SortOrder = r.SortOrder ?? ((await db.Categories.Where(c => c.GroupId == group.Id).MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 10),
            Description = Blank(r.Description),
            CreatedAt = now,
            UpdatedAt = now,
        });
        audit.Log("CREATE_CATEGORY", Module, code, $"group={group.Code} tat={r.TatDays?.ToString() ?? "none"} {Route(r.RoDivisionCode, r.HoDivisionCode, r.DirectToHeadOffice)}");
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(string code, UpdateCategoryRequest r, CancellationToken ct)
    {
        ValidateText(r.Name, "Name", 150);
        ValidateTat(r.TatDays);
        ValidateSort(r.SortOrder);
        ValidateOptional(r.Description, "Description", 500);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Code == code, ct) ?? throw new NotFoundException("Category", code);
        var group = await ResolveGroupAsync(r.GroupCode, ct);

        // Moving to another group puts the category at the end of that group; otherwise the given order stands.
        var sortOrder = category.GroupId == group.Id
            ? r.SortOrder
            : (await db.Categories.Where(c => c.GroupId == group.Id).MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 10;
        category.Name = r.Name.Trim();
        category.GroupId = group.Id;
        category.TatDays = r.TatDays;
        category.DefaultPriorityCode = await ResolvePriorityAsync(r.DefaultPriorityCode, ct);
        category.RoDivisionCode = r.DirectToHeadOffice ? null : await ResolveDepartmentAsync(r.RoDivisionCode, ct);
        category.HoDivisionCode = await ResolveDepartmentAsync(r.HoDivisionCode, ct);
        category.DirectToHeadOffice = r.DirectToHeadOffice;
        category.SortOrder = sortOrder;
        category.IsActive = r.IsActive;
        category.Description = Blank(r.Description);
        category.UpdatedAt = clock.GetUtcNow();
        audit.Log("UPDATE_CATEGORY", Module, code,
            $"group={group.Code} tat={r.TatDays?.ToString() ?? "none"} active={r.IsActive} {Route(r.RoDivisionCode, r.HoDivisionCode, r.DirectToHeadOffice)}");
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteCategoryAsync(string code, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Code == code, ct) ?? throw new NotFoundException("Category", code);
        var used = await db.Complaints.CountAsync(c => c.CategoryId == category.Id, ct);
        if (used > 0)
            throw new DomainException("admin.category_in_use",
                $"{used} {(used == 1 ? "complaint uses" : "complaints use")} \"{category.Name}\", so it cannot be deleted. Hide it instead.");

        db.Categories.Remove(category);
        audit.Log("DELETE_CATEGORY", Module, code);
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
            departments, priorities, new FeedbackSettingsDto(await Feedback.FeedbackRules.WindowDaysAsync(db, ct)));
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

    public async Task UpdateFeedbackSettingsAsync(UpdateFeedbackSettingsRequest r, CancellationToken ct)
    {
        if (r.WindowDays is < 1 or > 365)
            throw new DomainException("admin.invalid_feedback_window", "The feedback window must be between 1 and 365 days.");
        await SetAsync(AppSettingKeys.FeedbackWindowDays, r.WindowDays.ToString(), ct);
        audit.Log("UPDATE_FEEDBACK_SETTINGS", Module, "feedback", $"window={r.WindowDays}d");
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

    private async Task<ComplaintCategoryGroup> ResolveGroupAsync(string? code, CancellationToken ct)
    {
        var c = Blank(code) ?? throw new DomainException("admin.required", "Group is required.");
        return await db.CategoryGroups.FirstOrDefaultAsync(g => g.Code == c, ct)
            ?? throw new DomainException("admin.unknown_group", "Choose an existing group.");
    }

    private async Task EnsureUniqueGroupNameAsync(string name, Guid? except, CancellationToken ct)
    {
        var lowered = name.Trim().ToLower();
        if (await db.CategoryGroups.AnyAsync(g => g.Name.ToLower() == lowered && g.Id != except, ct))
            throw new DomainException("admin.duplicate_name", $"A group called \"{name.Trim()}\" already exists.");
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

    private static string Route(string? ro, string? ho, bool direct) =>
        direct ? $"route=HO:{Blank(ho) ?? "any"}" : $"route=RO:{Blank(ro) ?? "any"}>HO:{Blank(ho) ?? "any"}";
}
