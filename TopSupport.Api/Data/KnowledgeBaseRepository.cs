using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>
/// ADO.NET implementation of IKnowledgeBaseRepository, calling usp_Kb_GetAll and
/// usp_KbArticle_GetPopular/Save/Delete.
/// </summary>
public class KnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly IDbExecutor _db;

    public KnowledgeBaseRepository(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<KnowledgeBaseDto> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var kb = new KnowledgeBaseDto();

        await _db.ExecuteReaderAsync(
            "dbo.usp_Kb_GetAll",
            _ => { },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    kb.Categories.Add(new KbCategoryDto
                    {
                        Id = (string)reader["Id"],
                        Name = (string)reader["Name"],
                        Description = (string)reader["Description"],
                        SortOrder = (int)reader["SortOrder"]
                    });
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    kb.Articles.Add(ReadArticle(reader));
                }
            });

        return kb;
    }

    public async Task<IReadOnlyList<KbArticleDto>> GetPopularAsync(int top, CancellationToken cancellationToken = default)
    {
        var articles = new List<KbArticleDto>();

        await _db.ExecuteReaderAsync(
            "dbo.usp_KbArticle_GetPopular",
            cmd => cmd.Parameters.AddWithValue("@Top", top),
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    articles.Add(ReadArticle(reader));
                }
            });

        return articles;
    }

    public async Task<KbArticleDto> SaveArticleAsync(KbArticleDto article, CancellationToken cancellationToken = default)
    {
        KbArticleDto? saved = null;

        await _db.ExecuteReaderAsync(
            "dbo.usp_KbArticle_Save",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@Id", article.Id);
                cmd.Parameters.AddWithValue("@CategoryId", article.CategoryId);
                cmd.Parameters.AddWithValue("@Title", article.Title);
                cmd.Parameters.AddWithValue("@Status", article.Status);
                cmd.Parameters.AddWithValue("@Body", article.Body);
            },
            cancellationToken,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    saved = ReadArticle(reader);
                }
            });

        return saved ?? throw new InvalidOperationException($"usp_KbArticle_Save returned no row for article \"{article.Id}\".");
    }

    public Task DeleteArticleAsync(string id, CancellationToken cancellationToken = default)
        => _db.ExecuteNonQueryAsync(
            "dbo.usp_KbArticle_Delete",
            cmd => cmd.Parameters.AddWithValue("@Id", id),
            cancellationToken);

    private static KbArticleDto ReadArticle(SqlDataReader reader) => new()
    {
        Id = (string)reader["Id"],
        CategoryId = (string)reader["CategoryId"],
        CategoryName = (string)reader["CategoryName"],
        Title = (string)reader["Title"],
        Status = (string)reader["Status"],
        Body = (string)reader["Body"],
        UpdatedAt = (DateTimeOffset)reader["UpdatedAt"],
        Views = (int)reader["Views"]
    };
}
