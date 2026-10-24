using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin Users screens: list, detail, create/edit, lock/unlock, reset password, delete.</summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;
    private readonly IUserRepository _userRepository;

    public UsersController(IUserAdminService users, IUserRepository userRepository)
    {
        _users = users;
        _userRepository = userRepository;
    }

    /// <summary>Non-deleted users with open-ticket counts (PAGES.users).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetList(CancellationToken cancellationToken)
        => Ok(await _users.GetListAsync(cancellationToken));

    /// <summary>Every user including deleted ones, so ticket screens can still name them.</summary>
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _users.GetAllIncludingDeletedAsync(cancellationToken));

    /// <summary>One user, deleted or not (the page shows "User not found" for deleted users).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(string id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public Task<ActionResult<SaveUserResult>> Create(SaveUserRequest request, CancellationToken cancellationToken)
        => Run(() => _users.CreateAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public Task<ActionResult<SaveUserResult>> Update(string id, SaveUserRequest request, CancellationToken cancellationToken)
        => Run(() => _users.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id}/toggle-lock")]
    public Task<ActionResult<UserDto>> ToggleLock(string id, AdminActionRequest request, CancellationToken cancellationToken)
        => Run(() => _users.ToggleLockAsync(id, request.ActorId, cancellationToken));

    [HttpPost("{id}/reset-password")]
    public Task<ActionResult<TemporaryPasswordDto>> ResetPassword(string id, AdminActionRequest request, CancellationToken cancellationToken)
        => Run(() => _users.ResetPasswordAsync(id, request.ActorId, cancellationToken));

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(string id, AdminActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _users.DeleteAsync(id, request.ActorId, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (UserOrgValidationException ex)
        {
            return BadRequest(new AdminValidationErrorDto { Errors = new Dictionary<string, string>(ex.Errors) });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
