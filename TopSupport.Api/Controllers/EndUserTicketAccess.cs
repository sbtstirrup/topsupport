using System.Security.Claims;
using TopSupport.Api.Auth;
using TopSupport.Models.Enums;
using TopSupport.Models.Tickets;

namespace TopSupport.Api.Controllers;

/// <summary>
/// What an end user may see of a ticket (TicketsController/AttachmentsController): only tickets
/// where they are the requester or affected user, and never the agent-only parts -- internal
/// notes, files attached to those notes (or agent files not anchored to a public reply), and
/// the task list.
/// </summary>
internal static class EndUserTicketAccess
{
    public static bool IsOwnTicket(this ClaimsPrincipal user, TicketDto ticket)
    {
        var accountId = user.AccountId();
        return ticket.RequesterUserId == accountId || ticket.AffectedUserId == accountId;
    }

    /// <summary>Strips the agent-only parts in place for an end user; agents get the ticket unchanged.</summary>
    public static TicketDto ForCaller(this ClaimsPrincipal user, TicketDto ticket)
    {
        if (!user.IsEndUser()) return ticket;

        // Work out the visible attachments before the notes leave the timeline.
        var visible = VisibleAttachments(ticket, ticket.Attachments);
        ticket.Timeline.RemoveAll(e => e.Kind == TicketTimelineEntryKind.Note);
        ticket.Attachments.RemoveAll(a => !visible.Contains(a));
        ticket.Tasks.Clear();
        return ticket;
    }

    /// <summary>
    /// The attachments of <paramref name="ticket"/> an end user may see: never files on internal
    /// notes, and -- defence in depth -- a file an agent uploaded only when it is anchored to a
    /// public Reply, or to the ticket's creation event when the agent raised the ticket on the
    /// user's behalf. An agent file with no anchor (or any other anchor) is hidden.
    /// </summary>
    public static IReadOnlyList<TicketAttachmentDto> VisibleAttachments(TicketDto ticket, IEnumerable<TicketAttachmentDto> attachments)
    {
        var noteIds = ticket.Timeline.Where(e => e.Kind == TicketTimelineEntryKind.Note).Select(e => e.Id).ToHashSet();
        var publicAnchorIds = PublicAgentAnchorIds(ticket);
        return attachments
            .Where(a => !IsOn(a, noteIds))
            .Where(a => a.UploadedByType != "agent" || IsOn(a, publicAnchorIds))
            .ToList();
    }

    /// <summary>A file attached to a single ticket the end user can see, under the same rules as VisibleAttachments.</summary>
    public static bool CanSeeAttachment(this ClaimsPrincipal user, TicketDto ticket, string attachmentId)
        => !user.IsEndUser()
            || (user.IsOwnTicket(ticket) && VisibleAttachments(ticket, ticket.Attachments).Any(a => a.Id == attachmentId));

    /// <summary>Public replies, plus the agent creator's "Created by ... on behalf of ..." event (their earliest event on the ticket).</summary>
    private static HashSet<string> PublicAgentAnchorIds(TicketDto ticket)
    {
        var ids = ticket.Timeline.Where(e => e.Kind == TicketTimelineEntryKind.Reply).Select(e => e.Id).ToHashSet();
        if (ticket.CreatedBy.Type == "agent" &&
            ticket.Timeline
                .Where(e => e.Kind == TicketTimelineEntryKind.Event && e.By.Type == "agent" && e.By.Id == ticket.CreatedBy.Id)
                .MinBy(e => e.At) is { } created)
        {
            ids.Add(created.Id);
        }

        return ids;
    }

    private static bool IsOn(TicketAttachmentDto attachment, HashSet<string> entryIds)
        => attachment.TimelineEntryId is not null && entryIds.Contains(attachment.TimelineEntryId);
}
