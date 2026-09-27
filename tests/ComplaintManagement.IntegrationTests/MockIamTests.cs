using ComplaintManagement.Infrastructure.IAM.Mock;

namespace ComplaintManagement.IntegrationTests;

public class MockPasswordHasherTests
{
    [Fact]
    public void Hash_verifies_only_the_right_password_and_is_salted()
    {
        var hash = MockPasswordHasher.Hash("s3cret-pass");
        Assert.StartsWith("pbkdf2-sha256$600000$", hash);
        Assert.DoesNotContain("s3cret-pass", hash);
        Assert.True(MockPasswordHasher.Verify("s3cret-pass", hash));
        Assert.False(MockPasswordHasher.Verify("S3cret-pass", hash));
        Assert.NotEqual(hash, MockPasswordHasher.Hash("s3cret-pass"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plaintext")]
    [InlineData("pbkdf2-sha256$1$AAAA$AAAA")] // too few iterations
    public void Missing_or_malformed_hashes_never_verify(string? stored) =>
        Assert.False(MockPasswordHasher.Verify("anything", stored));
}
