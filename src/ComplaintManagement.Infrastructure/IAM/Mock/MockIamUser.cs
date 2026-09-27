namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>
/// TEMPORARY stand-in for a Bank IAM user until the IAM login API is integrated. Same fields as the
/// IAM user profile plus a PBKDF2 password hash. Lives in its own "mock_iam" schema, outside the
/// product migrations, and is never used when the real IAM client is configured.
/// </summary>
public sealed class MockIamUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string EmployeeCode { get; set; }
    public required string FullName { get; set; }
    public string? Designation { get; set; }
    public required string AccessRole { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? OfficeId { get; set; }
    public required string OfficeCode { get; set; }
    public required string OfficeName { get; set; }
    public required string OfficeType { get; set; }
    public string? Mobile { get; set; }
    public bool IsActive { get; set; } = true;
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
