using System.Text.RegularExpressions;

namespace ComplaintManagement.Domain.ValueObjects;

/// <summary>Complaint reference numbers in the form HGB-YYYY-NNNNNNNN.</summary>
public static partial class ComplaintNumber
{
    public const string Prefix = "HGB";

    public static string Format(int year, long sequence)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (sequence is < 1 or > 99_999_999) throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"{Prefix}-{year:D4}-{sequence:D8}";
    }

    public static bool IsValid(string? value) => value is not null && Pattern().IsMatch(value);

    [GeneratedRegex(@"^HGB-\d{4}-\d{8}$")]
    private static partial Regex Pattern();
}
