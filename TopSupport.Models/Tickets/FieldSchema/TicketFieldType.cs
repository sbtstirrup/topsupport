namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// Mirrors the per-field "type" discriminator used by renderTopicField/topicFields in
/// topsupport-app.js (the demo's values are "select"/"radio"/"checks"/"area"/"files"/"date"/
/// "banner"/plain-text-input): which widget a DynamicTopicField renders.
/// </summary>
public enum TicketFieldType
{
    Text,
    TextArea,
    Select,
    Radio,
    Checks,
    Date,
    Files,
    Banner
}
