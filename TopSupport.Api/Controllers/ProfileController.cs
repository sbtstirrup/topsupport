using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// My Profile: the signed-in account's contact details and password change. Any signed-in account,
/// always for itself (the account comes from the token, never the request). Field errors come back
/// in ProfileSaveResult.Errors (200), like the demo's inline F.errs.
/// </summary>
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfileController(IProfileService profiles)
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
        request.ActorId = User.AccountId();
        await _profiles.UpdateDetailsAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        request.ActorId = User.AccountId();
        await _profiles.ChangePasswordAsync(request, cancellationToken);
        return Ok();
    }
}
