using TopSupport.Models.Enums;

namespace TopSupport.Services.Auth;

/// <summary>Result of a successful sign-in: which account signed in and which portal/role it lands in.</summary>
public record SignInResult(string AccountId, AppRole Role, string DisplayName);

/// <summary>Either <see cref="Account"/> (signed in) or <see cref="Error"/> (the message to show on the sign-in page).</summary>
public record SignInOutcome(SignInResult? Account, string? Error);

/// <summary>
/// One account in the development-only test switcher. <see cref="Detail"/> is the user's
/// department or the agent's departments; <see cref="CanSignIn"/> is false when locked or inactive.
/// </summary>
public record TestAccountItem(string AccountId, AppRole Role, string Name, string Username, string? AgentRole, string? Detail, bool CanSignIn, string? Status);
