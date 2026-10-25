using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TopSupport.Api.Auth;
using TopSupport.Services.Auth;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Sign-in (ACT.signin) and sign-out (ACT.signout). A successful sign-in returns a bearer token
/// that every other endpoint requires; the Blazor app keeps it in its auth cookie.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtTokenService _tokens;
    private readonly bool _testSwitcherEnabled;

    public AuthController(AuthService authService, JwtTokenService tokens, IOptions<AuthOptions> options, IWebHostEnvironment environment)
    {
        _authService = authService;
        _tokens = tokens;
        // Development AND explicitly enabled: there is no way to switch it on in Production.
        _testSwitcherEnabled = environment.IsDevelopment() && options.Value.TestAccountSwitcher.Enabled;
    }

    public record SignInRequest(string? Username, string? Password);

    public record SignInResponse(string Token, DateTimeOffset ExpiresAt, string AccountId, string Role, string DisplayName);

    public record SignInError(string Error);

    [AllowAnonymous]
    [HttpPost("sign-in")]
    public async Task<ActionResult<SignInResponse>> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _authService.SignInAsync(request.Username, request.Password, cancellationToken);
        if (outcome.Account is not { } account) return BadRequest(new SignInError(outcome.Error!));

        var (token, expiresAt) = _tokens.Issue(account);
        return Ok(new SignInResponse(token, expiresAt, account.AccountId, account.Role.ToString(), account.DisplayName));
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await _authService.SignOutAsync(User.AccountId(), User.AppRole(), cancellationToken);
        return Ok();
    }

    // ---------------- Test-account switcher (Development only) ----------------

    public record TestSignInRequest(string? AccountId);

    /// <summary>Every account the switcher can sign in as. 404 unless the switcher is enabled.</summary>
    [AllowAnonymous]
    [HttpGet("test-accounts")]
    public async Task<ActionResult<IReadOnlyList<TestAccountItem>>> GetTestAccounts(CancellationToken cancellationToken)
        => _testSwitcherEnabled ? Ok(await _authService.ListTestAccountsAsync(cancellationToken)) : NotFound();

    /// <summary>Signs in as any listed account without its password. 404 unless the switcher is enabled.</summary>
    [AllowAnonymous]
    [HttpPost("test-sign-in")]
    public async Task<ActionResult<SignInResponse>> TestSignIn(TestSignInRequest request, CancellationToken cancellationToken)
    {
        if (!_testSwitcherEnabled) return NotFound();

        var outcome = await _authService.TestSignInAsync(request.AccountId ?? string.Empty, cancellationToken);
        if (outcome.Account is not { } account) return BadRequest(new SignInError(outcome.Error!));

        var (token, expiresAt) = _tokens.Issue(account);
        return Ok(new SignInResponse(token, expiresAt, account.AccountId, account.Role.ToString(), account.DisplayName));
    }
}
