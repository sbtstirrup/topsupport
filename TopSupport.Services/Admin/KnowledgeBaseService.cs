using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Services.Admin;

/// <summary>Thrown by IKnowledgeBaseService.SaveArticleAsync when the form is invalid. Mapped to 400 by the Api.</summary>
public class KbArticleValidationException : Exception
{
    /// <summary>Field key ("title" / "body" / "categoryId" / "status") -> message, like the demo's F.errs.</summary>
    public IReadOnlyDictionary<string, string> Errors { get; }

    public KbArticleValidationException(IReadOnlyDictionary<string, string> errors)
        : base("The article has validation errors.")
    {
        Errors = errors;
    }
}

/// <summary>
/// Knowledge base reads and article save/delete -- port of PAGES.kb / ACT['ka-save'] /
/// ACT['ka-del'] and the landing page's popular-articles query in topsupport-app.js.
/// </summary>
public interface IKnowledgeBaseService
{
    Task<KnowledgeBaseDto> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The landing page's "Popular Help Articles": top 3 published by views.</summary>
    Task<IReadOnlyList<KbArticleDto>> GetPopularAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates (<paramref name="id"/> null) or updates an article. Returns null when updating an
    /// id that doesn't exist. Throws <see cref="KbArticleValidationException"/> on invalid input.
    /// </summary>
    Task<KbArticleDto?> SaveArticleAsync(string? id, SaveKbArticleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns false when there was no such article.</summary>
    Task<bool> DeleteArticleAsync(string id, CancellationToken cancellationToken = default);
}

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private const int PopularCount = 3;

    private readonly IKnowledgeBaseRepository _repository;
    private readonly IAuditService _audit;

    public KnowledgeBaseService(IKnowledgeBaseRepository repository, IAuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public Task<KnowledgeBaseDto> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<KbArticleDto>> GetPopularAsync(CancellationToken cancellationToken = default)
        => _repository.GetPopularAsync(PopularCount, cancellationToken);

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
        if (errors.Count > 0) throw new KbArticleValidationException(errors);

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

    public async Task<bool> DeleteArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        var kb = await _repository.GetAllAsync(cancellationToken);
        if (!kb.Articles.Any(a => a.Id == id)) return false;

        // ACT['ka-del'] writes no audit entry, so neither does this.
        await _repository.DeleteArticleAsync(id, cancellationToken);
        return true;
    }
}
