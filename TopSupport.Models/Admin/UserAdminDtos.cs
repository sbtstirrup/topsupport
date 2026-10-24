using TopSupport.Models.People;

namespace TopSupport.Models.Admin;

/// <summary>A row in the admin Users list (PAGES.users): the user plus their active-ticket count.</summary>
public class UserListItemDto : UserDto
{
    /// <summary>Open/In Progress/Awaiting User tickets where they're requester or affected user.</summary>
    public int OpenTicketCount { get; set; }
}

/// <summary>Create/edit user form (MODALS.useredit / S.forms.ue). ActorId is the acting admin, for the audit trail.</summary>
public class SaveUserRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string OrgId { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
}

/// <summary>Body for admin actions that need nothing but the acting account (lock, reset, delete).</summary>
public class AdminActionRequest
{
    public string ActorId { get; set; } = string.Empty;
}

/// <summary>What MODALS.temppw shows once: who it's for and the generated password.</summary>
public class TemporaryPasswordDto
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Result of saving a user. TemporaryPassword is only set when the user was just created.</summary>
public class SaveUserResult
{
    public UserDto User { get; set; } = new();
    public TemporaryPasswordDto? TemporaryPassword { get; set; }
}

/// <summary>
/// 400 body for admin form validation: field key (the demo's errs keys, e.g. "email") to the
/// demo's exact message.
/// </summary>
public class AdminValidationErrorDto
{
    public Dictionary<string, string> Errors { get; set; } = new();
}

/// <summary>ORG_DEPTS in topsupport-app.js: the departments an end user can belong to.</summary>
public static class UserDepartments
{
    public static readonly IReadOnlyList<string> All = new[] { "Customer Services", "Finance", "HR", "Legal", "Marketing", "Operations", "Product", "Sales" };
}
