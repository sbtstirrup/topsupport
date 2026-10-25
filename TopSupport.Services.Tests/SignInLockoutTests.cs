using TopSupport.Models.Admin;
using TopSupport.Services.Admin;
using TopSupport.Services.Auth;
using TopSupport.Services.Tests.Fakes;

namespace TopSupport.Services.Tests;

public class SignInLockoutTests
{
    private const string WrongCredentials = "The username or password is not right. Check them and try again.";
    private const string Locked = "This account is locked. Email itsupport@topcashback.example to unlock it.";
    private const string Password = "ServiceDesk1!";

    private readonly FakeCredentialRepository _credentials = new();
    private readonly FakeAppSettingsRepository _settings = new();
    private readonly FakeAuditRepository _audit = new();
    private readonly FakeAccount _account = new() { Id = "a5", Username = "nhussain" };
    private readonly AuthService _auth;

    public SignInLockoutTests()
    {
        _credentials.Accounts.Add(_account);
        var passwords = new AccountPasswordService(_credentials);
        // PBKDF2 at 600,000 iterations: hashed once per test, through the real code.
        passwords.SetAsync(_account.Id, Password).GetAwaiter().GetResult();

        var audit = new AuditService(_audit);
        _auth = new AuthService(_credentials, passwords, new SettingsService(_settings, audit), audit);
        LockAfter(3);
    }

    private void LockAfter(int attempts) => _settings.Values["security.lockoutAttempts"] = attempts.ToString();

    private Task<SignInOutcome> SignIn(string username, string password)
        => _auth.SignInAsync(username, password, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Correct_password_signs_in_and_clears_the_failure_count()
    {
        _account.FailedSignIns = 2;

        var outcome = await SignIn("  NHussain ", Password);

        Assert.Null(outcome.Error);
        Assert.Equal("a5", outcome.Account!.AccountId);
        Assert.Equal(0, _account.FailedSignIns);
        Assert.Contains(_audit.Entries, e => e.Action == "Signed in" && e.Category == AuditCategories.SignIn);
    }

    [Fact]
    public async Task Account_locks_on_the_configured_failure_and_is_audited_once()
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(WrongCredentials, (await SignIn("nhussain", "wrong-" + i)).Error);
        }

        Assert.True(_account.Locked);
        Assert.Single(_audit.Entries, e => e.Action == "Locked account after 3 failed sign-ins");
        // The fourth attempt found it already locked and wasn't counted.
        Assert.Equal(3, _credentials.FailedSignInsRecorded.Count);
    }

    [Fact]
    public async Task Locked_account_is_only_revealed_to_someone_with_the_password()
    {
        _account.Locked = true;

        Assert.Equal(WrongCredentials, (await SignIn("nhussain", "wrong")).Error);
        Assert.Equal(Locked, (await SignIn("nhussain", Password)).Error);
    }

    [Fact]
    public async Task Zero_attempts_turns_lockout_off()
    {
        LockAfter(0);

        for (var i = 0; i < 6; i++) await SignIn("nhussain", "wrong");

        Assert.False(_account.Locked);
        Assert.Null((await SignIn("nhussain", Password)).Error);
    }

    [Fact]
    public async Task Unknown_username_gets_the_same_message_and_counts_against_nobody()
    {
        var outcome = await SignIn("nobody", Password);

        Assert.Equal(WrongCredentials, outcome.Error);
        Assert.Empty(_credentials.FailedSignInsRecorded);
        Assert.Contains(_audit.Entries, e => e.ActorId == "unknown" && e.Action == "Failed sign-in (nobody)");
    }

    [Fact]
    public async Task Inactive_agent_is_treated_as_unknown()
    {
        _account.Active = false;

        Assert.Equal(WrongCredentials, (await SignIn("nhussain", Password)).Error);
    }

    [Fact]
    public async Task Account_without_a_password_cannot_sign_in()
    {
        _account.PasswordHash = null;

        Assert.Equal(WrongCredentials, (await SignIn("nhussain", Password)).Error);
    }
}
