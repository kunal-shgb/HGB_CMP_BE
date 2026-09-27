using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Domain.Entities;

/// <summary>
/// A Maker's requested status change waiting for a Checker (maker-checker). A Branch Maker's request
/// is decided by a Checker at that branch's Regional Office; an RO Maker's by a Head Office Checker; an HO Maker's
/// by a Checker of the HO department named in the approvals setting.
/// </summary>
public class ComplaintApproval : Entity
{
    public Guid ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public required string RequestedStatusCode { get; set; }
    /// <summary>Where the complaint goes back to if the Checker returns the request.</summary>
    public required string PreviousStatusCode { get; set; }
    public string? MakerRemarks { get; set; }

    public required string RequestedByEmployeeId { get; set; }
    public string? RequestedByName { get; set; }
    public string? RequestedByOfficeName { get; set; }
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>Level whose Checker must decide: Region or HeadOffice.</summary>
    public ScopeLevel ApproverLevel { get; set; }
    /// <summary>Regional Office code when ApproverLevel is Region; null for Head Office.</summary>
    public string? ApproverOfficeCode { get; set; }
    /// <summary>For Head Office requests: the HO department whose Checkers decide (null = any HO Checker).</summary>
    public string? ApproverDepartment { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? DecidedByEmployeeId { get; set; }
    public string? DecidedByName { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionRemarks { get; set; }

    /// <summary>Concurrency token so two Checkers cannot both decide the same request.</summary>
    public uint Version { get; set; }
}
