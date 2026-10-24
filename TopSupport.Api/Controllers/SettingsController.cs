using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Security settings (password rules) and site page text. Reads are public: the rules drive My Profile and the text drives the landing/sign-in pages.</summary>
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
    public async Task<ActionResult<SaveSecuritySettingsResult>> SaveSecurity(SaveSecuritySettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var changed = await _settings.SaveSecurityAsync(request.ActorId, request.Settings, cancellationToken);
            return Ok(new SaveSecuritySettingsResult { Changed = changed });
        }
        catch (SettingsValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("site")]
    public async Task<ActionResult<SiteSettingsDto>> GetSite(CancellationToken cancellationToken)
        => Ok(await _settings.GetSiteAsync(cancellationToken));

    [HttpPut("site")]
    public async Task<IActionResult> SaveSite(SaveSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _settings.SaveSiteAsync(request.ActorId, request.Settings, cancellationToken);
            return Ok();
        }
        catch (SettingsValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
