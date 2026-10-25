using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TopSupport.Services.Auth;

namespace TopSupport.Api.Auth;

/// <summary>Issues the HMAC-SHA256 signed bearer token returned by a successful sign-in.</summary>
public sealed class JwtTokenService
{
    public const string AccountIdClaim = "sub";
    public const string NameClaim = "name";
    public const string RoleClaim = "role";

    private readonly AuthOptions _options;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeProvider _time;

    public JwtTokenService(IOptions<AuthOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
        _key = CreateKey(_options);
    }

    public (string Token, DateTimeOffset ExpiresAt) Issue(SignInResult account)
    {
        var expiresAt = _time.GetUtcNow().AddMinutes(_options.TokenLifetimeMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AccountIdClaim, account.AccountId),
                new Claim(NameClaim, account.DisplayName),
                new Claim(RoleClaim, account.Role.ToString())
            }),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        });
        return (token, expiresAt);
    }

    /// <summary>Validation settings for the JwtBearer handler: same issuer, audience and key as <see cref="Issue"/>.</summary>
    public static TokenValidationParameters ValidationParameters(AuthOptions options) => new()
    {
        ValidIssuer = options.Issuer,
        ValidAudience = options.Audience,
        IssuerSigningKey = CreateKey(options),
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        NameClaimType = NameClaim,
        RoleClaimType = RoleClaim,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    private static SymmetricSecurityKey CreateKey(AuthOptions options)
    {
        var bytes = Encoding.UTF8.GetBytes(options.SigningKey);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be configured with at least 32 bytes (see TopSupport.Api/Auth/AuthOptions.cs).");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
