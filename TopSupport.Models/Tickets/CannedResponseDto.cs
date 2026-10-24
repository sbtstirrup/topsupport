namespace TopSupport.Models.Tickets;

/// <summary>Mirrors an entry in the demo's DB.canned (agent canned reply templates).</summary>
public class CannedResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
