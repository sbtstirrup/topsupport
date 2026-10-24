namespace TopSupport.Blazor.Components.Shared;

/// <summary>Which field ChangeStatusOrPriorityModal is editing -- mirrors the d.field ("status" |
/// "priority") discriminator MODALS.setfield uses in topsupport-app.js.</summary>
public enum StatusOrPriority
{
    Status,
    Priority
}
