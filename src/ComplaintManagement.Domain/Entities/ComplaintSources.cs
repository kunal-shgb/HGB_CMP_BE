namespace ComplaintManagement.Domain.Entities;

/// <summary>Where a complaint was lodged: by the customer on the Bank website, or by staff on the customer's behalf.</summary>
public static class ComplaintSources
{
    public const string Website = "WEBSITE";
    public const string Branch = "BRANCH";
    public const string RegionalOffice = "REGIONAL_OFFICE";
    public const string HeadOffice = "HEAD_OFFICE";

    public static readonly IReadOnlyList<string> All = [Website, Branch, RegionalOffice, HeadOffice];

    public static string Name(string source) => source switch
    {
        Website => "Website",
        Branch => "Branch",
        RegionalOffice => "Regional Office",
        HeadOffice => "Head Office",
        _ => source,
    };
}
