using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin Background Jobs page: TopSupport.Worker's jobs and sent emails. Admin only, read-only.</summary>
[ApiController]
[Route("api/admin/worker")]
[Authorize(Policy = Policies.Admin)]
public class WorkerMonitorController : ControllerBase
{
    private readonly WorkerMonitorService _service;

    public WorkerMonitorController(WorkerMonitorService service)
    {
        _service = service;
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<WorkerJobsDto>> GetJobs(CancellationToken cancellationToken)
        => Ok(await _service.GetJobsAsync(cancellationToken));

    [HttpGet("notifications")]
    public async Task<ActionResult<WorkerNotificationsDto>> GetNotifications(
        [FromQuery] string? type, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => Ok(await _service.GetNotificationsAsync(type, search, page, pageSize, cancellationToken));
}
