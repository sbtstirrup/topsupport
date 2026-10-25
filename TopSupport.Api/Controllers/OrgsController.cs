using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Models.People;
using TopSupport.Models.Tickets;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Admin Organisations screens: list, detail, open tickets, create/edit. Admins only.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.Admin)]
public class OrgsController : ControllerBase
{
    private readonly IOrgAdminService _orgs;

    public OrgsController(IOrgAdminService orgs)
    {
        _orgs = orgs;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrgListItemDto>>> GetList(CancellationToken cancellationToken)
        => Ok(await _orgs.GetListAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<OrgDto>> Get(string id, CancellationToken cancellationToken)
    {
        var org = await _orgs.GetAsync(id, cancellationToken);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpGet("{id}/open-tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetOpenTickets(string id, CancellationToken cancellationToken)
        => Ok(await _orgs.GetOpenTicketsAsync(id, cancellationToken));

    [HttpPost]
    public Task<ActionResult<OrgDto>> Create(SaveOrgRequest request, CancellationToken cancellationToken)
    {
        request.ActorId = User.AccountId();
        return Run(() => _orgs.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id}")]
    public Task<ActionResult<OrgDto>> Update(string id, SaveOrgRequest request, CancellationToken cancellationToken)
    {
        request.ActorId = User.AccountId();
        return Run(() => _orgs.UpdateAsync(id, request, cancellationToken));
    }

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (UserOrgValidationException ex)
        {
            return BadRequest(new AdminValidationErrorDto { Errors = new Dictionary<string, string>(ex.Errors) });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
