using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin "Roles and Permissions" grid: read every cell, set one cell. Admins only.</summary>
[ApiController]
[Route("api/admin/permissions")]
[Authorize(Policy = Policies.Admin)]
public class RolePermissionsController : ControllerBase
{
    private readonly RolePermissionService _service;

    public RolePermissionsController(RolePermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RolePermissionDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Set(SetRolePermissionRequest request, CancellationToken cancellationToken)
    {
        await _service.SetAsync(request, User.AccountId(), cancellationToken);
        return Ok();
    }
}
