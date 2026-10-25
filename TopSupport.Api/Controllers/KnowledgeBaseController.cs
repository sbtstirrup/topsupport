using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Api.Auth;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Knowledge base: admin list + article save/delete (admins only), and the public
/// popular-articles feed (anonymous; usp_KbArticle_GetPopular returns Published articles only).
/// </summary>
[ApiController]
[Route("api/kb")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _kb;

    public KnowledgeBaseController(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    /// <summary>Every category and article, drafts included.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<KnowledgeBaseDto>> GetAll(CancellationToken cancellationToken)
        => Ok(await _kb.GetAllAsync(cancellationToken));

    /// <summary>Published articles for the public landing page (no drafts).</summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<KbArticleDto>>> GetPopular(CancellationToken cancellationToken)
        => Ok(await _kb.GetPopularAsync(cancellationToken));

    [HttpPost("articles")]
    [Authorize(Policy = Policies.Admin)]
    public Task<ActionResult<KbArticleDto>> Create(SaveKbArticleRequest request, CancellationToken cancellationToken)
        => SaveAsync(null, request, cancellationToken);

    [HttpPut("articles/{id}")]
    [Authorize(Policy = Policies.Admin)]
    public Task<ActionResult<KbArticleDto>> Update(string id, SaveKbArticleRequest request, CancellationToken cancellationToken)
        => SaveAsync(id, request, cancellationToken);

    [HttpDelete("articles/{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        => await _kb.DeleteArticleAsync(id, cancellationToken) ? Ok() : NotFound();

    private async Task<ActionResult<KbArticleDto>> SaveAsync(string? id, SaveKbArticleRequest request, CancellationToken cancellationToken)
    {
        request.ActorId = User.AccountId();
        var saved = await _kb.SaveArticleAsync(id, request, cancellationToken);
        return saved is null ? NotFound() : Ok(saved);
    }
}
