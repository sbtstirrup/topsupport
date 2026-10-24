using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>My Profile: the signed-in account's contact details and password change. Field errors come back in ProfileSaveResult.Errors (200), like the demo's inline F.errs.</summary>
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfileController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet("{accountId}")]
    public async Task<ActionResult<ProfileDto>> Get(string accountId, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetAsync(accountId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileSaveResult>> UpdateDetails(UpdateProfileRequest request, CancellationToken cancellationToken)
        => Ok(await _profiles.UpdateDetailsAsync(request, cancellationToken));

    [HttpPost("password")]
    public async Task<ActionResult<ProfileSaveResult>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        => Ok(await _profiles.ChangePasswordAsync(request, cancellationToken));
}
