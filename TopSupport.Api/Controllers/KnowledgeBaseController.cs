using Microsoft.AspNetCore.Mvc;
using TopSupport.Models.Admin;
using TopSupport.Services.Admin;

namespace TopSupport.Api.Controllers;

/// <summary>Knowledge base: admin list + article save/delete, and the public popular-articles feed.</summary>
[ApiController]
[Route("api/kb")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _kb;

    public KnowledgeBaseController(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    [HttpGet]
    public async Task<ActionResult<KnowledgeBaseDto>> GetAll(CancellationToken cancellationToken)
        => Ok(await _kb.GetAllAsync(cancellationToken));

    /// <summary>Published articles for the public landing page (no drafts).</summary>
    [HttpGet("popular")]
    public async Task<ActionResult<IReadOnlyList<KbArticleDto>>> GetPopular(CancellationToken cancellationToken)
        => Ok(await _kb.GetPopularAsync(cancellationToken));

    [HttpPost("articles")]
    public Task<ActionResult<KbArticleDto>> Create(SaveKbArticleRequest request, CancellationToken cancellationToken)
        => SaveAsync(null, request, cancellationToken);

    [HttpPut("articles/{id}")]
    public Task<ActionResult<KbArticleDto>> Update(string id, SaveKbArticleRequest request, CancellationToken cancellationToken)
        => SaveAsync(id, request, cancellationToken);

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        => await _kb.DeleteArticleAsync(id, cancellationToken) ? Ok() : NotFound();

    private async Task<ActionResult<KbArticleDto>> SaveAsync(string? id, SaveKbArticleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var saved = await _kb.SaveArticleAsync(id, request, cancellationToken);
            return saved is null ? NotFound() : Ok(saved);
        }
        catch (KbArticleValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }
}
