using System.Security.Cryptography;
using System.Text;
using ComplaintManagement.Domain.Entities;

namespace ComplaintManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Initial configuration seeded by migrations. Categories and statuses follow the product spec
/// (sections 14 and 15). Maker/Checker role mappings match the Bank IAM accessRole values.
/// The transition set, which moves need Checker approval, and the default priority are PROVISIONAL until the Bank
/// confirms its workflow; change them with a new migration or through the admin screens.
/// TAT values are deliberately left null because the Bank has not fixed them yet.
/// </summary>
internal static class ReferenceSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly ComplaintStatus[] Statuses =
    [
        new() { Code = "NEW", Name = "New", CustomerLabel = "Registered", IsInitial = true, SortOrder = 10 },
        new() { Code = "RECEIVED", Name = "Received", CustomerLabel = "Under review", SortOrder = 20 },
        new() { Code = "ASSIGNED", Name = "Assigned", CustomerLabel = "Under review", IsAssignment = true, SortOrder = 30 },
        new() { Code = "UNDER_PROCESS", Name = "Under process", CustomerLabel = "Under process", SortOrder = 40 },
        new() { Code = "CUSTOMER_RESPONSE", Name = "Awaiting customer response", CustomerLabel = "Awaiting your response", SortOrder = 50 },
        new() { Code = "ESCALATED", Name = "Escalated", CustomerLabel = "Under process", SortOrder = 60 },
        new() { Code = "TRANSFERRED", Name = "Transferred", CustomerLabel = "Under process", SortOrder = 70 },
        new() { Code = "PENDING_APPROVAL", Name = "Pending checker approval", CustomerLabel = "Under process", IsApprovalPending = true, SortOrder = 75 },
        new() { Code = "RESOLVED", Name = "Resolved", CustomerLabel = "Resolved", IsResolution = true, SortOrder = 80 },
        new() { Code = "REOPENED", Name = "Reopened", CustomerLabel = "Reopened", SortOrder = 90 },
        new() { Code = "CLOSED", Name = "Closed", CustomerLabel = "Closed", IsTerminal = true, SortOrder = 100 },
        new() { Code = "REJECTED", Name = "Rejected", CustomerLabel = "Closed", IsTerminal = true, SortOrder = 110 },
        new() { Code = "DUPLICATE", Name = "Duplicate", CustomerLabel = "Closed", IsTerminal = true, SortOrder = 120 },
        new() { Code = "WITHDRAWN", Name = "Withdrawn", CustomerLabel = "Withdrawn", IsTerminal = true, SortOrder = 130 },
    ];

    private static readonly (string From, string To, bool Remark)[] TransitionRows =
    [
        ("NEW", "RECEIVED", false), ("NEW", "ASSIGNED", false), ("NEW", "REJECTED", true), ("NEW", "DUPLICATE", true),
        ("RECEIVED", "ASSIGNED", false), ("RECEIVED", "UNDER_PROCESS", false), ("RECEIVED", "TRANSFERRED", true),
        ("RECEIVED", "REJECTED", true), ("RECEIVED", "DUPLICATE", true),
        ("ASSIGNED", "UNDER_PROCESS", false), ("ASSIGNED", "TRANSFERRED", true), ("ASSIGNED", "ESCALATED", true),
        ("UNDER_PROCESS", "CUSTOMER_RESPONSE", true), ("UNDER_PROCESS", "RESOLVED", true),
        ("UNDER_PROCESS", "ESCALATED", true), ("UNDER_PROCESS", "TRANSFERRED", true),
        ("CUSTOMER_RESPONSE", "UNDER_PROCESS", false), ("CUSTOMER_RESPONSE", "RESOLVED", true), ("CUSTOMER_RESPONSE", "WITHDRAWN", true),
        ("ESCALATED", "ASSIGNED", false), ("ESCALATED", "UNDER_PROCESS", false), ("ESCALATED", "RESOLVED", true),
        ("TRANSFERRED", "ASSIGNED", false), ("TRANSFERRED", "UNDER_PROCESS", false),
        ("RESOLVED", "CLOSED", false), ("RESOLVED", "REOPENED", true),
        ("CLOSED", "REOPENED", true),
        ("REOPENED", "ASSIGNED", false), ("REOPENED", "UNDER_PROCESS", false),
    ];

    /// <summary>Decisions a Maker cannot finalise alone: a Checker must approve them.</summary>
    private static readonly HashSet<string> ApprovalTargets = ["RESOLVED", "REJECTED", "DUPLICATE"];

    public static readonly ComplaintStatusTransition[] Transitions = TransitionRows
        .Select((t, i) => new ComplaintStatusTransition
        {
            Id = i + 1,
            FromStatusCode = t.From,
            ToStatusCode = t.To,
            RequiresRemark = t.Remark,
            RequiresApproval = ApprovalTargets.Contains(t.To),
        })
        .ToArray();

    /// <summary>
    /// The Bank IAM accessRole values and the application roles they grant. Every employee also gets Viewer,
    /// and IAM system administrators get Admin (see RoleMappingService.ResolveAll).
    /// </summary>
    public static readonly ApplicationRoleMapping[] RoleMappings =
    [
        // Branches: only the OfficeHead works complaints (others view, or work what the OfficeHead assigns them).
        new() { Id = StableGuid("role:OfficeHead:Branch"), IamRole = "OfficeHead", OfficeType = "Branch", ApplicationRole = "OFFICE_HEAD", CreatedAt = SeededAt, UpdatedAt = SeededAt },
        // Regional Offices and Head Office keep Maker / Checker.
        new() { Id = StableGuid("role:Maker"), IamRole = "Maker", OfficeType = "Regional Office", ApplicationRole = "MAKER", CreatedAt = SeededAt, UpdatedAt = SeededAt },
        new() { Id = StableGuid("role:Maker:HO"), IamRole = "Maker", OfficeType = "Head Office", ApplicationRole = "MAKER", CreatedAt = SeededAt, UpdatedAt = SeededAt },
        new() { Id = StableGuid("role:Checker"), IamRole = "Checker", OfficeType = "Regional Office", ApplicationRole = "CHECKER", CreatedAt = SeededAt, UpdatedAt = SeededAt },
        new() { Id = StableGuid("role:Checker:HO"), IamRole = "Checker", OfficeType = "Head Office", ApplicationRole = "CHECKER", CreatedAt = SeededAt, UpdatedAt = SeededAt },
        // Admin now comes from the IAM isSystemAdmin flag, not an access role.
    ];

    /// <summary>Admin-managed settings. The HO checking department is left unset until the Bank names it.</summary>
    public static readonly AppSetting[] Settings =
    [
        new() { Key = AppSettingKeys.HeadOfficeMakerCheckerDepartment, Value = null, UpdatedAt = SeededAt },
        // Provisional escalation rules until the Bank sets them: to the RO as soon as the TAT is missed, to HO a week later.
        new() { Key = AppSettingKeys.EscalationEnabled, Value = "true", UpdatedAt = SeededAt },
        new() { Key = AppSettingKeys.EscalateToRegionalOfficeAfterDays, Value = "0", UpdatedAt = SeededAt },
        new() { Key = AppSettingKeys.EscalateToHeadOfficeAfterDays, Value = "7", UpdatedAt = SeededAt },
    ];

    public static readonly ComplaintPriority[] Priorities =
    [
        new() { Code = "LOW", Name = "Low", Rank = 1 },
        new() { Code = "MEDIUM", Name = "Medium", Rank = 2, IsDefault = true },
        new() { Code = "HIGH", Name = "High", Rank = 3 },
        new() { Code = "CRITICAL", Name = "Critical", Rank = 4 },
    ];

    private static readonly (string Group, (string Code, string Name)[] Items)[] CategoryRows =
    [
        ("Digital Banking",
        [
            ("UPI", "UPI"), ("IMPS", "IMPS"), ("AEPS", "AePS"), ("NFS_ATM", "NFS / ATM"), ("DEBIT_CARD", "Debit Card"),
            ("POS", "POS"), ("ECOMMERCE", "E-Commerce"), ("INTERNET_BANKING", "Internet Banking"),
            ("MOBILE_BANKING", "Mobile Banking"), ("QR_MERCHANT", "QR / Merchant"), ("BBPS", "BBPS"),
        ]),
        ("Banking Services",
        [
            ("DEPOSIT_ACCOUNTS", "Deposit Accounts"), ("LOANS_ADVANCES", "Loans / Advances"), ("CHEQUE_CTS", "Cheque / CTS"),
            ("NEFT", "NEFT"), ("RTGS", "RTGS"), ("NACH", "NACH"), ("CASH_TRANSACTIONS", "Cash Transactions"),
            ("PASSBOOK", "Passbook"), ("ACCOUNT_SERVICES", "Account Services"), ("CUSTOMER_SERVICE", "Customer Service"),
        ]),
        ("Other",
        [
            ("PENSION", "Pension"), ("GOVT_SCHEMES", "Government Schemes"), ("STAFF_BEHAVIOUR", "Staff Behaviour"),
            ("BRANCH_SERVICES", "Branch Services"), ("CHARGES_FEES", "Charges / Fees"),
            ("FRAUD", "Fraud / Suspected Fraud"), ("OTHER", "Other"),
        ]),
    ];

    public static readonly ComplaintCategory[] Categories = CategoryRows
        .SelectMany(g => g.Items.Select(i => (g.Group, i.Code, i.Name)))
        .Select((c, i) => new ComplaintCategory
        {
            Id = StableGuid($"category:{c.Code}"),
            Code = c.Code,
            Name = c.Name,
            GroupName = c.Group,
            SortOrder = (i + 1) * 10,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
        })
        .ToArray();

    /// <summary>Every category starts with a "General" sub-category; UPI also carries the spec's example.</summary>
    public static readonly ComplaintSubCategory[] SubCategories = Categories
        .Select(c => new ComplaintSubCategory
        {
            Id = StableGuid($"subcategory:{c.Code}:GENERAL"),
            Code = "GENERAL",
            Name = "General",
            CategoryId = c.Id,
            SortOrder = 100,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
        })
        .Append(new ComplaintSubCategory
        {
            Id = StableGuid("subcategory:UPI:FAILED_TRANSACTION"),
            Code = "FAILED_TRANSACTION",
            Name = "Amount debited but transaction failed",
            CategoryId = StableGuid("category:UPI"),
            SortOrder = 10,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
        })
        .ToArray();

    /// <summary>Deterministic GUIDs so HasData produces stable migrations.</summary>
    internal static Guid StableGuid(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes("hgb-cmp:" + key)).AsSpan(0, 16));
}
