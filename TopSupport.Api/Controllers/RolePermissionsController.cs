using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin "Roles and Permissions" grid: read every cell, set one cell.</summary>
[ApiController]
[Route("api/admin/permissions")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _service;

    public RolePermissionsController(IRolePermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RolePermissionDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Set(SetRolePermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _service.SetAsync(request, cancellationToken);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
