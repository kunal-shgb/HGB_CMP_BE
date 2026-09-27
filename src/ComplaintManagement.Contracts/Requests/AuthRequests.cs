namespace ComplaintManagement.Contracts.Requests;

/// <summary>Staff sign-in. The password is relayed to the Bank IAM and never stored or logged.</summary>
public sealed record LoginRequest(string EmployeeCode, string Password)
{
    // Records print every property by default; keep the password out of any log or debug output.
    public override string ToString() => $"LoginRequest {{ EmployeeCode = {EmployeeCode} }}";
}
