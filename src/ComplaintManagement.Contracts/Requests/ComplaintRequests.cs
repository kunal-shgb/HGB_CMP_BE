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
    public string? SubCategoryCode { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? AssignedEmployeeId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool? OverdueOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record ChangeStatusRequest(string NewStatus, string? Remarks);

public sealed record AssignComplaintRequest(string AssignedToEmployeeId, string? DepartmentCode, string? Remarks);

public sealed record AddRemarkRequest(string Remark, string Visibility);

/// <summary>Complaint lodged by a customer through the Bank website.</summary>
public sealed record PublicCreateComplaintRequest
{
    public required string CustomerName { get; init; }
    public required string Mobile { get; init; }
    public string? Email { get; init; }
    public string? CustomerId { get; init; }
    public string? AccountNumber { get; init; }
    public required string BranchCode { get; init; }
    public required string CategoryCode { get; init; }
    public required string SubCategoryCode { get; init; }
    public string? TransactionId { get; init; }
    public DateOnly? TransactionDate { get; init; }
    public decimal? Amount { get; init; }
    public required string Description { get; init; }
    public string? PreferredChannel { get; init; }
}
