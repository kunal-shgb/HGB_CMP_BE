using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

/// <summary>Regional Office. Master data is owned by the Bank; the portal keeps a local copy for joins and filtering.</summary>
public class Region : AuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Branch> Branches { get; set; } = [];
}

public class Branch : AuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid RegionId { get; set; }
    public Region? Region { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Head Office department (e.g. Digital Banking Division).</summary>
public class Department : AuditableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}
