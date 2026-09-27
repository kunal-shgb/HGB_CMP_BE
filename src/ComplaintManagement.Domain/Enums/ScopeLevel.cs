namespace ComplaintManagement.Domain.Enums;

/// <summary>Organisational level a user works at, taken from the IAM office type. Drives complaint visibility.</summary>
public enum ScopeLevel
{
    /// <summary>Complaints logged for the user's own branch.</summary>
    Branch = 0,
    /// <summary>Complaints of every branch under the user's Regional Office.</summary>
    Region = 1,
    /// <summary>All complaints.</summary>
    HeadOffice = 2,
}
