namespace TopSupport.Models.Admin;

/// <summary>Mirrors an entry in the demo's DB.kb.cats (kbCats in buildSeed()).</summary>
public class KbCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>Mirrors an entry in the demo's DB.kb.articles (kbArticles in buildSeed()).</summary>
public class KbArticleDto
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Joined from dbo.KbCategories, so list views and the reader don't need a second lookup.</summary>
    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>One of <see cref="KbArticleStatuses"/>.</summary>
    public string Status { get; set; } = KbArticleStatuses.Draft;

    public string Body { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public int Views { get; set; }
}

/// <summary>The whole knowledge base (the demo's DB.kb): categories in display order + every article.</summary>
public class KnowledgeBaseDto
{
    public List<KbCategoryDto> Categories { get; set; } = new();
    public List<KbArticleDto> Articles { get; set; } = new();
}

/// <summary>Body of POST/PUT api/kb/articles -- the S.forms.ka fields plus the acting account for the audit.</summary>
public class SaveKbArticleRequest
{
    public string ActorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Status { get; set; } = KbArticleStatuses.Draft;
    public string Body { get; set; } = string.Empty;
}

/// <summary>The article statuses offered by PAGES.kbedit (dbo.KbArticles has a CHECK constraint on these).</summary>
public static class KbArticleStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";

    public static readonly IReadOnlyList<string> All = new[] { Draft, Published };
}
