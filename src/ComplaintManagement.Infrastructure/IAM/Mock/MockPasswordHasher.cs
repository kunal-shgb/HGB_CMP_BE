using System.Security.Cryptography;

namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>PBKDF2-SHA256 hashes for the mock IAM store: "pbkdf2-sha256$iterations$salt$hash" (base64).</summary>
public static class MockPasswordHasher
{
    private const int Iterations = 600_000; // OWASP recommendation for PBKDF2-HMAC-SHA256
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Scheme = "pbkdf2-sha256";

    // Verified against when the employee code is unknown, so response time does not reveal which codes exist.
    private static readonly Lazy<string> DummyHash = new(() => Hash(Guid.NewGuid().ToString()));

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string? stored)
    {
        var parts = (stored ?? DummyHash.Value).Split('$');
        if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out var iterations) || iterations < 100_000)
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected) && stored is not null;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
