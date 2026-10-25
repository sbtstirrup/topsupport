using TopSupport.Models.Admin;

namespace TopSupport.Services.Abstractions;

/// <summary>
/// Data-access contract for dbo.KbCategories/dbo.KbArticles, calling usp_Kb_GetAll and
/// usp_KbArticle_GetPopular/Save/Delete.
/// </summary>
public interface IKnowledgeBaseRepository
{
    Task<KnowledgeBaseDto> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KbArticleDto>> GetPopularAsync(int top, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the article (stamping UpdatedAt) and returns the saved row.</summary>
    Task<KbArticleDto> SaveArticleAsync(KbArticleDto article, CancellationToken cancellationToken = default);

    Task DeleteArticleAsync(string id, CancellationToken cancellationToken = default);
}
