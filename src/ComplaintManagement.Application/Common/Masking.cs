namespace ComplaintManagement.Application.Common;

/// <summary>Masks customer identifiers for display and logs, keeping only the last four characters.</summary>
public static class Masking
{
    public static string Mobile(string value) => MaskKeepLast(value, 4, 'X');

    /// <summary>Account numbers render as "XXXX XXXX 8397".</summary>
    public static string Account(string value)
    {
        var digits = new string(value.Where(char.IsLetterOrDigit).ToArray());
        if (digits.Length <= 4) return new string('X', digits.Length);
        return $"XXXX XXXX {digits[^4..]}";
    }

    public static string Identifier(string value) => MaskKeepLast(value, 4, 'X');

    /// <summary>"r***@example.com": first letter of the mailbox and the domain.</summary>
    public static string Email(string value)
    {
        var at = value.IndexOf('@');
        if (at <= 0) return "***";
        return $"{value[0]}***{value[at..]}";
    }

    private static string MaskKeepLast(string value, int keep, char mask)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= keep) return new string(mask, trimmed.Length);
        return new string(mask, trimmed.Length - keep) + trimmed[^keep..];
    }
}
