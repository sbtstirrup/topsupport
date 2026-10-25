namespace TopSupport.Services.Admin;

/// <summary>
/// Thrown by UserAdminService/OrgAdminService when a create/edit form fails validation. Errors
/// maps the demo's errs keys ("name", "email", ...) to its exact messages; the Api returns them
/// as an AdminValidationErrorDto so the modal can show them under each field.
/// </summary>
public class UserOrgValidationException : Exception
{
    public UserOrgValidationException(IReadOnlyDictionary<string, string> errors)
        : base("Validation failed: " + string.Join(" ", errors.Values))
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string> Errors { get; }
}
