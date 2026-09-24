namespace ComplaintManagement.Domain.Enums;

/// <summary>Who may see a remark. Customer-visible remarks may be exposed through public tracking.</summary>
public enum RemarkVisibility
{
    Internal = 0,
    Customer = 1,
}
