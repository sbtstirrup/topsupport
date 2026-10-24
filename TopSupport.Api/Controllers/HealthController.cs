using Microsoft.AspNetCore.Mvc;

namespace TopSupport.Api.Controllers;

/// <summary>Simple liveness check, used by the Blazor app's diagnostics page to prove the
/// Blazor -> HTTP -> API path works end to end.</summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", utc = DateTimeOffset.UtcNow });
}
