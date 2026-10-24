namespace TopSupport.Blazor.Services;

/// <summary>
/// Lets a page set the title/description shown in the shell's topbar, mirroring how
/// pg.title/pg.desc drive shell()'s header in topsupport-app.js. Pages call SetHeading in
/// OnInitialized/OnParametersSet; MainLayout subscribes to Changed to re-render the topbar.
/// </summary>
public class PageHeadingState
{
    public string Title { get; private set; } = "TopSupport";
    public string? Description { get; private set; }

    public event Action? Changed;

    public void SetHeading(string title, string? description = null)
    {
        Title = title;
        Description = description;
        Changed?.Invoke();
    }
}
