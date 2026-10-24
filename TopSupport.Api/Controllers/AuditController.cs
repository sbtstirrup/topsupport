using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Audit trail read + append (append is for UI-only events such as sign-in/sign-out).</summary>
[ApiController]
[Route("api/[controller]")]
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

    public record LogRequest(string ActorId, string Action, string Target, string Category);

    [HttpPost]
    public async Task<IActionResult> Log(LogRequest request, CancellationToken cancellationToken)
    {
        if (!AuditCategories.All.Contains(request.Category)) return BadRequest($"Unknown category \"{request.Category}\".");
        await _audit.LogAsync(request.ActorId, request.Action, request.Target, request.Category, cancellationToken);
        return Ok();
    }
}
