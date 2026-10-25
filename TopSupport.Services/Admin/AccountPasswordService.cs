using System.Security.Cryptography;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Password handling shared by the admin screens and My Profile. Passwords are stored as PBKDF2
/// hashes instead of the demo's plaintext, and temporary passwords are random rather than the
/// demo's guessable genPassword() word pattern. Policy checks (length, character classes, history)
/// belong to the security-settings feature, not here.
/// </summary>
public interface IAccountPasswordService
{
    /// <summary>A random 16-character temporary password with an upper, lower, digit and symbol.</summary>
    string GenerateTemporary();

    /// <summary>False if the account doesn't exist or the password is wrong.</summary>
    Task<bool> VerifyAsync(string accountId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Does the same PBKDF2 work as a real check and returns false, so a sign-in for an unknown
    /// username takes as long as one for a real account (no username enumeration by timing).
    /// </summary>
    bool VerifyUnknownAccount(string password);

    /// <summary>Admin reset: hashes and stores the password, and unlocks the account (ACT['user-reset']).</summary>
    Task SetAsync(string accountId, string password, CancellationToken cancellationToken = default);

    /// <summary>The account holder's own change: hashes and stores the password, leaving a lock in place.</summary>
    Task ChangeOwnAsync(string accountId, string password, CancellationToken cancellationToken = default);
}

public class AccountPasswordService : IAccountPasswordService
{
    // No look-alike characters (0/O, 1/l/I), since the admin reads the password out to the user.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!#$%&?";
    private const int TemporaryLength = 16;

    // OWASP Password Storage Cheat Sheet figure for PBKDF2-HMAC-SHA256. The count is stored in each
    // hash, so hashes made with the earlier 100,000 still verify.
    private const int Iterations = 600_000;

    // Any well-formed hash at the current iteration count (its password is irrelevant).
    private static readonly string UnknownAccountHash = Hash(Guid.NewGuid().ToString());

    private readonly IAccountCredentialRepository _repository;

    public AccountPasswordService(IAccountCredentialRepository repository)
    {
        _repository = repository;
    }

    public string GenerateTemporary()
    {
        // One of each class (so the password policy's character rules pass), the rest from all
        // classes, then shuffled so the classes aren't in fixed positions.
        const string all = Upper + Lower + Digits + Symbols;
        var chars = new char[TemporaryLength];
        chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        chars[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
        for (var i = 4; i < chars.Length; i++) chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    public async Task<bool> VerifyAsync(string accountId, string password, CancellationToken cancellationToken = default)
    {
        var credential = await _repository.GetAsync(accountId, cancellationToken);
        if (credential is null) return false;

        // No hash means no password has been set, so nothing can match (never a default password).
        return credential.PasswordHash is not null && VerifyHash(password, credential.PasswordHash);
    }

    public bool VerifyUnknownAccount(string password)
    {
        VerifyHash(password, UnknownAccountHash);
        return false;
    }

    public Task SetAsync(string accountId, string password, CancellationToken cancellationToken = default)
        => _repository.SetPasswordHashAsync(accountId, Hash(password), unlock: true, cancellationToken);

    public Task ChangeOwnAsync(string accountId, string password, CancellationToken cancellationToken = default)
        => _repository.SetPasswordHashAsync(accountId, Hash(password), unlock: false, cancellationToken);

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
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;

        // A corrupt stored hash fails verification instead of throwing (a 500).
        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expected.Length == 0) return false;
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
