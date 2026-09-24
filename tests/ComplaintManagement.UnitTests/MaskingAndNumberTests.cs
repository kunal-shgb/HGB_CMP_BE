using ComplaintManagement.Application.Common;
using ComplaintManagement.Domain.ValueObjects;

namespace ComplaintManagement.UnitTests;

public class MaskingAndNumberTests
{
    [Fact]
    public void Mobile_keeps_last_four() => Assert.Equal("XXXXXX3210", Masking.Mobile("9876543210"));

    [Fact]
    public void Account_uses_bank_display_format() => Assert.Equal("XXXX XXXX 8397", Masking.Account("1234 5678 8397"));

    [Fact]
    public void Short_values_are_fully_masked() => Assert.Equal("XXX", Masking.Identifier("123"));

    [Fact]
    public void Complaint_number_matches_spec_format()
    {
        Assert.Equal("HGB-2026-00001245", ComplaintNumber.Format(2026, 1245));
        Assert.True(ComplaintNumber.IsValid("HGB-2026-00001245"));
        Assert.False(ComplaintNumber.IsValid("HGB-26-1245"));
    }

    [Fact]
    public void Complaint_number_rejects_out_of_range_sequence() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ComplaintNumber.Format(2026, 100_000_000));
}
