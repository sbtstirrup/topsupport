using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Audit trail read (PAGES.audit). Admins only; sign-in/sign-out entries are written by AuthController.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Admin)]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> GetRecent(CancellationToken cancellationToken)
        => Ok(await _audit.GetRecentAsync(cancellationToken));
}
