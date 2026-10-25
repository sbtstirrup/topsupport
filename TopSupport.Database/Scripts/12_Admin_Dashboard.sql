/*
    12_Admin_Dashboard.sql
    Admin dashboard (PAGES.dashboard in topsupport-app.js). Everything except "when was this
    ticket closed" is computed in C# by DashboardService over usp_Ticket_SearchForQueue's rows
    (same SLA/overdue logic as QueueService via TicketSla), so this script only adds the one
    proc that needs the timeline: closedAt(t), which is the time of the ticket's most recent
    "Closed by ..." event, falling back to Tickets.Updated when no such event exists.
    Re-runnable (CREATE OR ALTER).
*/
USE TopSupport;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetTicketClosedAt
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id AS TicketId,
           COALESCE(
               (
                   SELECT TOP (1) te.At
                   FROM dbo.TicketTimelineEntries te
                   WHERE te.TicketId = t.Id AND te.Kind = 'Event' AND te.Text LIKE N'Closed by%'
                   ORDER BY te.At DESC
               ),
               t.Updated
           ) AS ClosedAt
    FROM dbo.Tickets t
    WHERE t.Status IN ('Resolved', 'Closed');
END
GO
