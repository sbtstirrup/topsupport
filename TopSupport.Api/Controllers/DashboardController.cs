using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin dashboard figures (PAGES.dashboard), computed by IDashboardService. Admins only.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Admin)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
        => Ok(await _dashboard.GetDashboardAsync(cancellationToken));
}
