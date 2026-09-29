namespace ComplaintManagement.Contracts.Requests;

/// <summary>Staff complaint list filters. All filters are optional and combined with AND.</summary>
public sealed record ComplaintFilterRequest
{
    public string? ComplaintNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? Mobile { get; init; }
    public string? AccountNumber { get; init; }
    public string? CustomerId { get; init; }
    public string? TransactionId { get; init; }
    public string? BranchCode { get; init; }
    public string? RegionCode { get; init; }
    public string? CategoryCode { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? AssignedEmployeeId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool? OverdueOnly { get; init; }
    /// <summary>Only complaints escalated to at least this level (2 = Regional Office, 3 = Head Office).</summary>
    public int? MinEscalationLevel { get; init; }
    /// <summary>WEBSITE, BRANCH, REGIONAL_OFFICE or HEAD_OFFICE.</summary>
    public string? Source { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record ChangeStatusRequest(string NewStatus, string? Remarks);

public sealed record AssignComplaintRequest(string AssignedToEmployeeId, string? DepartmentCode, string? Remarks);

public sealed record AddRemarkRequest(string Remark, string Visibility);

/// <summary>Escalate a complaint one level up. A reason is required.</summary>
public sealed record EscalateRequest(string Remarks);

/// <summary>Checker decision. Remarks are required when returning a request to the Maker.</summary>
public sealed record DecideApprovalRequest(string? Remarks);

/// <summary>The customer and complaint details shared by every way of lodging a complaint.</summary>
public interface IComplaintIntakeDetails
{
    string CustomerName { get; }
    string Mobile { get; }
    string? Email { get; }
    string? CustomerId { get; }
    string? AccountNumber { get; }
    string CategoryCode { get; }
    string? TransactionId { get; }
    DateOnly? TransactionDate { get; }
    decimal? Amount { get; }
    string Title { get; }
    string Description { get; }
    string? PreferredChannel { get; }
}

/// <summary>Complaint lodged by a customer through the Bank website.</summary>
public sealed record PublicCreateComplaintRequest : IComplaintIntakeDetails
{
    public required string CustomerName { get; init; }
    public required string Mobile { get; init; }
    public string? Email { get; init; }
    public string? CustomerId { get; init; }
    public string? AccountNumber { get; init; }
    public required string BranchCode { get; init; }
    public required string CategoryCode { get; init; }
    public string? TransactionId { get; init; }
    public DateOnly? TransactionDate { get; init; }
    public decimal? Amount { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? PreferredChannel { get; init; }
}

/// <summary>
/// Complaint lodged by staff on a customer's behalf. Branch staff always lodge for their own branch, so
/// BranchCode is ignored for them; Regional and Head Office staff must choose the branch.
/// </summary>
public sealed record StaffCreateComplaintRequest : IComplaintIntakeDetails
{
    public required string CustomerName { get; init; }
    public required string Mobile { get; init; }
    public string? Email { get; init; }
    public string? CustomerId { get; init; }
    public string? AccountNumber { get; init; }
    public string? BranchCode { get; init; }
    public required string CategoryCode { get; init; }
    public string? TransactionId { get; init; }
    public DateOnly? TransactionDate { get; init; }
    public decimal? Amount { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? PreferredChannel { get; init; }
}

/// <summary>Customer asks for a one-time code to view a complaint.</summary>
public sealed record TrackingOtpRequest(string ComplaintNumber, string Mobile);

/// <summary>Customer submits the one-time code.</summary>
public sealed record TrackingVerifyRequest(string ComplaintNumber, string Mobile, string Otp)
{
    public override string ToString() => $"TrackingVerifyRequest {{ ComplaintNumber = {ComplaintNumber} }}";
}
