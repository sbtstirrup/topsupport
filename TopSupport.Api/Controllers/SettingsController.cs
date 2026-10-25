using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Security settings (password rules) and site page text. Site text is anonymous (it drives the
/// landing/sign-in pages); the password rules need any signed-in account (My Profile); saves are admins only.
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;

    public SettingsController(ISettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet("security")]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecurity(CancellationToken cancellationToken)
        => Ok(await _settings.GetSecurityAsync(cancellationToken));

    [HttpPut("security")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<SaveSecuritySettingsResult>> SaveSecurity(SaveSecuritySettingsRequest request, CancellationToken cancellationToken)
    {
        var changed = await _settings.SaveSecurityAsync(User.AccountId(), request.Settings, cancellationToken);
        return Ok(new SaveSecuritySettingsResult { Changed = changed });
    }

    [HttpGet("site")]
    [AllowAnonymous]
    public async Task<ActionResult<SiteSettingsDto>> GetSite(CancellationToken cancellationToken)
        => Ok(await _settings.GetSiteAsync(cancellationToken));

    [HttpPut("site")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> SaveSite(SaveSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        await _settings.SaveSiteAsync(User.AccountId(), request.Settings, cancellationToken);
        return Ok();
    }
}
