using TopSupport.Models.Enums;

namespace TopSupport.Api.Identity;

/// <summary>
/// Identity's user model for TopSupport. Maps to dbo.AspNetUsers (see
/// TopSupport.Database/Scripts/01_Schema_Identity.sql), with two TopSupport-specific
/// properties added: which portal the account signs into, and which domain record
/// (Users.Id or Agents.Id) it represents.
/// </summary>
public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    /// <summary>Which portal (User/Agent/Admin) this account lands in — matches S.role in the demo.</summary>
    public AppRole AppRole { get; set; }

    /// <summary>The Users.Id or Agents.Id row this login represents, depending on AppRole.</summary>
    public string DomainPersonId { get; set; } = string.Empty;
}
