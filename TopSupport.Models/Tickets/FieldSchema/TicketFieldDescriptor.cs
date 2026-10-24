namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// One dynamic field in a topic's form, mirroring one entry in the arrays returned by
/// topicFields() in topsupport-app.js. Field list/labels/types/required-ness/show-conditions
/// are all static data; only StaticOptions-vs-OptionsSource varies by whether the option list
/// needs runtime data (see TicketFieldOptionsSource).
/// </summary>
public class TicketFieldDescriptor
{
    /// <summary>The key this field is stored under in TicketDto.Fields (the demo's "k").</summary>
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public TicketFieldType Type { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? Hint { get; set; }
    public int? Rows { get; set; }

    /// <summary>Shown instead of the Checks/Select option list when it's empty at resolve
    /// time (e.g. "{name} has no equipment in the asset list.").</summary>
    public string? EmptyMessage { get; set; }

    public List<TicketFieldOption>? StaticOptions { get; set; }
    public TicketFieldOptionsSource OptionsSource { get; set; } = TicketFieldOptionsSource.Static;

    public TicketFieldShowCondition? ShowWhen { get; set; }
}
