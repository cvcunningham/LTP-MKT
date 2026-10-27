using System.Security.Cryptography;

namespace api.Auth;

// Passwords are never stored as typed. employees.csv's PasswordHash column holds
// "pbkdf2$<iterations>$<salt>$<hash>" (PBKDF2-SHA256, built into .NET).
// An empty PasswordHash means the starting password, DefaultPassword (new interns, and after a reset).
public static class Passwords
{
    public const string DefaultPassword = "LTPMKT";
    public const int MinLength = 8;

    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (storedHash == "")
            return password == DefaultPassword;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2" || !int.TryParse(parts[1], out var iterations))
            return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected); // same time whether it matches or not
    }
}
