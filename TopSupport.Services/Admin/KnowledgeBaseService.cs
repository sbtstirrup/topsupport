using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>
/// Knowledge base reads and article save/delete -- port of PAGES.kb / ACT['ka-save'] /
/// ACT['ka-del'] and the landing page's popular-articles query in topsupport-app.js.
/// </summary>
public class KnowledgeBaseService
{
    private const int PopularCount = 3;

    private readonly IKnowledgeBaseRepository _repository;
    private readonly AuditService _audit;

    public KnowledgeBaseService(IKnowledgeBaseRepository repository, AuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public Task<KnowledgeBaseDto> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <summary>The landing page's "Popular Help Articles": top 3 published by views.</summary>
    public Task<IReadOnlyList<KbArticleDto>> GetPopularAsync(CancellationToken cancellationToken = default)
        => _repository.GetPopularAsync(PopularCount, cancellationToken);

    /// <summary>
    /// Creates (<paramref name="id"/> null) or updates an article. Returns null when updating an
    /// id that doesn't exist. Throws <see cref="ValidationException"/> on invalid input.
    /// </summary>
    public async Task<KbArticleDto?> SaveArticleAsync(string? id, SaveKbArticleRequest request, CancellationToken cancellationToken = default)
    {
        var kb = await _repository.GetAllAsync(cancellationToken);

        // Same messages as ACT['ka-save']; category/status can't be wrong from the UI (fixed
        // selects) but are checked here so the Api never hits the FK/CHECK constraints.
        var errors = new Dictionary<string, string>();
        // The length checks are additions: SQL Server would otherwise truncate the title silently.
        if (string.IsNullOrWhiteSpace(request.Title)) errors["title"] = "Enter a title.";
        else if (FieldLimits.TooLong(request.Title, FieldLimits.KbTitle) is { } titleTooLong) errors["title"] = titleTooLong;
        if (string.IsNullOrWhiteSpace(request.Body)) errors["body"] = "Write the article text.";
        else if (FieldLimits.TooLong(request.Body, FieldLimits.KbBody) is { } bodyTooLong) errors["body"] = bodyTooLong;
        if (!kb.Categories.Any(c => c.Id == request.CategoryId)) errors["categoryId"] = "Choose a category.";
        if (!KbArticleStatuses.All.Contains(request.Status)) errors["status"] = "Choose a status.";
        if (errors.Count > 0) throw new ValidationException(errors);

        if (id is not null && !kb.Articles.Any(a => a.Id == id)) return null;

        var saved = await _repository.SaveArticleAsync(new KbArticleDto
        {
            // Demo: uid('ka') for a new article.
            Id = id ?? "ka" + Guid.NewGuid().ToString("n"),
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Status = request.Status,
            Body = request.Body.Trim()
        }, cancellationToken);

        await _audit.LogAsync(request.ActorId, "Saved knowledge base article (" + saved.Status + ")", saved.Title, AuditCategories.Permission, cancellationToken);
        return saved;
    }

    /// <summary>Returns false when there was no such article.</summary>
    public async Task<bool> DeleteArticleAsync(string id, string actorId, CancellationToken cancellationToken = default)
    {
        var article = (await _repository.GetAllAsync(cancellationToken)).Articles.FirstOrDefault(a => a.Id == id);
        if (article is null) return false;

        // The demo's ACT['ka-del'] writes no audit entry; a delete should leave a trail, so this does.
        await _repository.DeleteArticleAsync(id, cancellationToken);
        await _audit.LogAsync(actorId, "Deleted knowledge base article", article.Title, AuditCategories.Permission, cancellationToken);
        return true;
    }
}
