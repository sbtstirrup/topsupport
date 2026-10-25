using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Admin Users screens: list, detail, create/edit, lock/unlock, reset password, delete. Admins
/// only, except GET api/users/all, which every signed-in account needs to name people on tickets.
/// The acting admin is always the signed-in account.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserAdminService _users;
    private readonly IUserRepository _userRepository;

    public UsersController(UserAdminService users, IUserRepository userRepository)
    {
        _users = users;
        _userRepository = userRepository;
    }

    /// <summary>Non-deleted users with open-ticket counts (PAGES.users).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetList(CancellationToken cancellationToken)
        => Ok(await _users.GetListAsync(cancellationToken));

    /// <summary>
    /// Every user including deleted ones, so ticket screens can still name them. Any signed-in
    /// account: the Blazor LookupDataService loads it for the end-user Raise/My Tickets/ticket pages too,
    /// so end users get names only (PeopleRedaction).
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(User.ForCaller(await _users.GetAllIncludingDeletedAsync(cancellationToken)));

    /// <summary>One user, deleted or not (the page shows "User not found" for deleted users).</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<UserDto>> Get(string id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<SaveUserResult>> Create(SaveUserRequest request, CancellationToken cancellationToken)
        => Ok(await _users.CreateAsync(request, User.AccountId(), cancellationToken));

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<SaveUserResult>> Update(string id, SaveUserRequest request, CancellationToken cancellationToken)
        => Ok(await _users.UpdateAsync(id, request, User.AccountId(), cancellationToken));

    [HttpPost("{id}/toggle-lock")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<UserDto>> ToggleLock(string id, CancellationToken cancellationToken)
        => Ok(await _users.ToggleLockAsync(id, User.AccountId(), cancellationToken));

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<TemporaryPasswordDto>> ResetPassword(string id, CancellationToken cancellationToken)
        => Ok(await _users.ResetPasswordAsync(id, User.AccountId(), cancellationToken));

    [HttpPost("{id}/delete")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _users.DeleteAsync(id, User.AccountId(), cancellationToken);
        return Ok();
    }
}
