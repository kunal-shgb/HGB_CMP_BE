using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

public class ComplaintCategory : AuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Category group for display, e.g. "Digital Banking".</summary>
    public required string GroupName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ComplaintSubCategory> SubCategories { get; set; } = [];
}

public class ComplaintSubCategory : AuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public ComplaintCategory? Category { get; set; }
    /// <summary>Turn-around time in calendar days. Null until the Bank fixes SLA values.</summary>
    public int? TatDays { get; set; }
    /// <summary>Department that should own complaints of this type by default, if any.</summary>
    public Guid? DefaultDepartmentId { get; set; }
    public string? DefaultPriorityCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A workflow status. Codes are the natural key (e.g. NEW, ASSIGNED).</summary>
public class ComplaintStatus
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    /// <summary>Label shown to customers on public tracking.</summary>
    public required string CustomerLabel { get; set; }
    public bool IsInitial { get; set; }
    /// <summary>Terminal statuses stop the SLA clock and count as disposed.</summary>
    public bool IsTerminal { get; set; }
    /// <summary>Entering this status stamps the complaint's resolved time.</summary>
    public bool IsResolution { get; set; }
    /// <summary>Status the complaint moves to when it is assigned, if the workflow allows it.</summary>
    public bool IsAssignment { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>An allowed move in the status workflow.</summary>
public class ComplaintStatusTransition
{
    public int Id { get; set; }
    public required string FromStatusCode { get; set; }
    public required string ToStatusCode { get; set; }
    /// <summary>When true the user must supply a remark to make this move.</summary>
    public bool RequiresRemark { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ComplaintPriority
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Rank { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Maps a Bank IAM role name to an application role.</summary>
public class ApplicationRoleMapping : AuditableEntity
{
    public required string IamRole { get; set; }
    public required string ApplicationRole { get; set; }
    public bool IsActive { get; set; } = true;
}
