using System.Security.Cryptography;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Password handling shared by the admin screens and My Profile. GenerateTemporary is a port of
/// genPassword() in topsupport-app.js; passwords are stored as PBKDF2 hashes instead of the demo's
/// plaintext. Policy checks (length, character classes, history) belong to the security-settings
/// feature, not here.
/// </summary>
public interface IAccountPasswordService
{
    /// <summary>A memorable temporary password like "Harbour-copper42!".</summary>
    string GenerateTemporary();

    /// <summary>False if the account doesn't exist or the password is wrong.</summary>
    Task<bool> VerifyAsync(string accountId, string password, CancellationToken cancellationToken = default);

    /// <summary>Hashes and stores the password, and unlocks the account.</summary>
    Task SetAsync(string accountId, string password, CancellationToken cancellationToken = default);
}

public class AccountPasswordService : IAccountPasswordService
{
    private static readonly string[] Words = { "Harbour", "Copper", "Meadow", "Granite", "Willow", "Falcon", "Lantern", "Summit" };
    private const string Symbols = "!#$%&?";
    private const int Iterations = 100_000;

    private readonly IAccountCredentialRepository _repository;

    public AccountPasswordService(IAccountCredentialRepository repository)
    {
        _repository = repository;
    }

    public string GenerateTemporary()
        => Words[RandomNumberGenerator.GetInt32(Words.Length)] + "-"
            + Words[RandomNumberGenerator.GetInt32(Words.Length)].ToLowerInvariant()
            + RandomNumberGenerator.GetInt32(10, 100)
            + Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];

    public async Task<bool> VerifyAsync(string accountId, string password, CancellationToken cancellationToken = default)
    {
        var credential = await _repository.GetAsync(accountId, cancellationToken);
        if (credential is null) return false;

        if (credential.PasswordHash is null)
        {
            // Never changed since seeding: the buildSeed() demo password applies.
            var demoDefault = credential.AccountType == "User" ? "TopCashback1!"
                : credential.AgentRole == "Administrator" ? "AdminDemo1!"
                : "ServiceDesk1!";
            return password == demoDefault;
        }

        return VerifyHash(password, credential.PasswordHash);
    }

    public Task SetAsync(string accountId, string password, CancellationToken cancellationToken = default)
        => _repository.SetPasswordHashAsync(accountId, Hash(password), cancellationToken);

    // Format: pbkdf2-sha256$<iterations>$<base64 salt>$<base64 hash>
    private static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyHash(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
