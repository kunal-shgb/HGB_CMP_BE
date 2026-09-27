namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>
/// Dummy organisation and employees for the mock IAM store (fictional people; sample offices).
/// Add rows here: missing codes are inserted on the next startup.
/// </summary>
public static class MockIamSeedData
{
    public sealed record SeedUser(
        string EmployeeCode, string FullName, string Designation, string AccessRole,
        int? DepartmentId, string? DepartmentName, int OfficeId, string OfficeCode, string OfficeName, string OfficeType,
        string Mobile, bool IsActive = true);

    private const string Ho = "Head Office";
    private const string Ro = "Regional Office";
    private const string Br = "Branch";
    private const string HoName = "Head Office — Rohtak";

    public static readonly IReadOnlyList<(string Code, string Name)> Regions =
    [
        ("RO-ROH", "Regional Office — Rohtak"),
        ("RO-HSR", "Regional Office — Hisar"),
        ("RO-KNL", "Regional Office — Karnal"),
        ("RO-RWR", "Regional Office — Rewari"),
        // Clean office for trying the flow end to end: no sample complaints.
        ("RO-JHJ", "Regional Office — Jhajjar"),
    ];

    public static readonly IReadOnlyList<(string Code, string Name, string RegionCode)> Branches =
    [
        ("BR-ROH-001", "Rohtak Main", "RO-ROH"), ("BR-ROH-002", "Sampla", "RO-ROH"), ("BR-ROH-003", "Meham", "RO-ROH"),
        ("BR-HSR-001", "Hisar City", "RO-HSR"), ("BR-HSR-002", "Hansi", "RO-HSR"), ("BR-HSR-003", "Barwala", "RO-HSR"),
        ("BR-KNL-001", "Karnal Sector 12", "RO-KNL"), ("BR-KNL-002", "Assandh", "RO-KNL"), ("BR-KNL-003", "Nilokheri", "RO-KNL"),
        ("BR-RWR-001", "Rewari Main", "RO-RWR"), ("BR-RWR-002", "Bawal", "RO-RWR"),
        ("BR-JHJ-001", "Jhajjar Main", "RO-JHJ"),
    ];

    public static readonly IReadOnlyList<(string Code, string Name)> Departments =
    [
        ("DBD", "Digital Banking Division"),
        ("CSD", "Customer Service Department"),
        ("CRD", "Credit Department"),
        ("OPS", "Operations Department"),
    ];

    public static readonly IReadOnlyList<SeedUser> Users =
    [
        // Head Office
        new("900001", "DEEPAK ARORA", "SYSTEM ADMINISTRATOR", "Admin", 23, "DBD", 5, "0000", HoName, Ho, "9000000001"),
        new("100001", "ANIL SHARMA", "CHIEF MANAGER", "Checker", 20, "CSD", 5, "0000", HoName, Ho, "9000000002"),
        new("100004", "MEENAKSHI JAIN", "CHIEF MANAGER - IT", "Checker", 23, "DBD", 5, "0000", HoName, Ho, "9000000003"),
        new("100002", "SUNITA MALIK", "MANAGER", "Maker", 20, "CSD", 5, "0000", HoName, Ho, "9000000004"),
        new("100003", "ROHIT BANSAL", "MANAGER - IT", "Maker", 23, "DBD", 5, "0000", HoName, Ho, "9000000005"),
        // Regional Offices
        new("200001", "PRIYA VERMA", "SENIOR MANAGER", "Checker", null, null, 11, "RO-ROH", "Regional Office — Rohtak", Ro, "9000000006"),
        new("200003", "SANJAY DALAL", "MANAGER", "Maker", null, null, 11, "RO-ROH", "Regional Office — Rohtak", Ro, "9000000007"),
        new("200002", "MANOJ JANGRA", "SENIOR MANAGER", "Checker", null, null, 12, "RO-HSR", "Regional Office — Hisar", Ro, "9000000008"),
        new("200004", "KULDEEP MALIK", "SENIOR MANAGER", "Checker", null, null, 13, "RO-KNL", "Regional Office — Karnal", Ro, "9000000009"),
        // Branches
        new("300001", "RAKESH KUMAR", "BRANCH MANAGER", "Maker", null, null, 101, "BR-ROH-001", "Rohtak Main", Br, "9000000010"),
        new("300002", "NEHA SAINI", "OFFICER", "Maker", null, null, 102, "BR-ROH-002", "Sampla", Br, "9000000011"),
        new("300003", "AJAY PUNIA", "BRANCH MANAGER", "Maker", null, null, 104, "BR-HSR-001", "Hisar City", Br, "9000000012"),
        new("300004", "POONAM HOODA", "OFFICER", "Maker", null, null, 107, "BR-KNL-001", "Karnal Sector 12", Br, "9000000013"),
        // Jhajjar: a branch Maker and the RO Checker who approves their decisions. Both start with zero complaints.
        new("300010", "AMIT DESWAL", "BRANCH MANAGER", "Maker", null, null, 110, "BR-JHJ-001", "Jhajjar Main", Br, "9000000015"),
        new("200010", "RITU SANGWAN", "SENIOR MANAGER", "Checker", null, null, 14, "RO-JHJ", "Regional Office — Jhajjar", Ro, "9000000016"),
        new("300099", "INACTIVE OFFICER", "OFFICER", "Maker", null, null, 101, "BR-ROH-001", "Rohtak Main", Br, "9000000014", IsActive: false),
    ];
}
