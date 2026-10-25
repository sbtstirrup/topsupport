using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// My Profile: the signed-in account's contact details and password change. Any signed-in account,
/// always for itself (the account comes from the token, never the request). Invalid input is a 400
/// with field messages, like the demo's inline F.errs.
/// </summary>
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profiles;

    public ProfileController(ProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetAsync(User.AccountId(), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDetails(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await _profiles.UpdateDetailsAsync(User.AccountId(), request, cancellationToken);
        return Ok();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _profiles.ChangePasswordAsync(User.AccountId(), request, cancellationToken);
        return Ok();
    }
}
