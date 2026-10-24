namespace TopSupport.Blazor.Components.Shared;

/// <summary>Which column layout QueueTable renders -- see QueueTable.razor's parameter contract doc.</summary>
public enum QueueTableColumnSet
{
    /// <summary>MyTickets.razor: Ticket #, Subject, Topic, Priority, Status, Created. No checkboxes/assignee.</summary>
    User,

    /// <summary>Queue.razor: checkbox, Ticket #, Subject, Requester, Topic, Priority, SLA due, Assignee, Status.</summary>
    Agent
}
