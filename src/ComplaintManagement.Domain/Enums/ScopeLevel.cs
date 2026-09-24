namespace ComplaintManagement.Domain.Enums;

/// <summary>Organisational level a user operates at. Drives complaint visibility.</summary>
public enum ScopeLevel
{
    Branch = 0,
    Region = 1,
    Department = 2,
    HeadOffice = 3,
}
