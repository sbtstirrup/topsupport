using System.Net;
using System.Net.Http.Json;
using TopSupport.Models.Admin;

namespace TopSupport.Blazor.Services;

/// <summary>Result of saving an article: the saved row, or the Api's field errors (400) if it rejected the form.</summary>
public record KbArticleSaveResult(KbArticleDto? Article, IReadOnlyDictionary<string, string> Errors);

public partial class TopSupportApiClient
{
    // ---------------- Knowledge base (KnowledgeBaseController) ----------------

    public async Task<KnowledgeBaseDto> GetKnowledgeBaseAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<KnowledgeBaseDto>("api/kb", cancellationToken) ?? new();

    public async Task<IReadOnlyList<KbArticleDto>> GetPopularKbArticlesAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<KbArticleDto>>("api/kb/popular", cancellationToken) ?? new();

    /// <summary>Creates the article when <paramref name="id"/> is null, otherwise updates it.</summary>
    public async Task<KbArticleSaveResult> SaveKbArticleAsync(string? id, SaveKbArticleRequest request, CancellationToken cancellationToken = default)
    {
        var response = id is null
            ? await _httpClient.PostAsJsonAsync("api/kb/articles", request, cancellationToken)
            : await _httpClient.PutAsJsonAsync($"api/kb/articles/{Uri.EscapeDataString(id)}", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new KbArticleSaveResult(null, await ReadValidationErrorsAsync(response, cancellationToken));
        }

        await EnsureSuccessWithMessageAsync(response, cancellationToken);
        var saved = await response.Content.ReadFromJsonAsync<KbArticleDto>(cancellationToken);
        return new KbArticleSaveResult(saved, new Dictionary<string, string>());
    }

    public async Task DeleteKbArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/kb/articles/{Uri.EscapeDataString(id)}", cancellationToken);
        await EnsureSuccessWithMessageAsync(response, cancellationToken);
    }
}
