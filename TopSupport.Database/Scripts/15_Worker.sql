/*
    15_Worker.sql
    Tables and procs owned by TopSupport.Worker, the Hangfire background job runner. Everything
    here lives in the "worker" schema; the worker only READS the dbo tables, apart from the
    auto-close job, which makes the same writes the Api makes when an agent closes a ticket.
    (Hangfire creates its own tables in the "jobs" schema on first start-up.)

    - worker.NotificationLog: one row per email sent (or being sent). The unique index on
      (TicketId, EventKey, Recipient) is what stops a retry or an overlapping run sending the
      same email twice. TicketId is '' for emails that aren't about one ticket (the digest).
    - worker.JobCursor: when each polling job last completed, so the next run knows where to
      pick up (and the very first run only looks back an hour instead of at all history).
    - Read procs that shape the dbo data each job needs, and worker.usp_AutoClose_CloseTicket.

    Event detection relies on the timeline text the Api writes (TicketService):
      "Assigned to <J. Moore | team name>" and "Closed by <who> with status Resolved|Closed".
*/
USE TopSupport;
GO

IF SCHEMA_ID(N'worker') IS NULL
    EXEC(N'CREATE SCHEMA worker AUTHORIZATION dbo;');
GO

IF OBJECT_ID(N'worker.NotificationLog', N'U') IS NULL
BEGIN
    CREATE TABLE worker.NotificationLog
    (
        Id          BIGINT          IDENTITY(1, 1) NOT NULL CONSTRAINT PK_NotificationLog PRIMARY KEY,
        TicketId    NVARCHAR(50)    NOT NULL,
        EventKey    NVARCHAR(100)   NOT NULL,
        Recipient   NVARCHAR(256)   NOT NULL,
        SentAtUtc   DATETIMEOFFSET  NOT NULL
    );
    CREATE UNIQUE INDEX UX_NotificationLog_Ticket_Event_Recipient ON worker.NotificationLog(TicketId, EventKey, Recipient);
END
GO

IF OBJECT_ID(N'worker.JobCursor', N'U') IS NULL
BEGIN
    CREATE TABLE worker.JobCursor
    (
        JobName          NVARCHAR(100)   NOT NULL CONSTRAINT PK_JobCursor PRIMARY KEY,
        LastCompletedUtc DATETIMEOFFSET  NOT NULL
    );
END
GO

/* ===================== NotificationLog ===================== */

-- Records the email before it is sent. Claimed = 1 means this caller won and should send it;
-- 0 means it was already sent (or another run is sending it right now).
CREATE OR ALTER PROCEDURE worker.usp_NotificationLog_TryClaim
    @TicketId  NVARCHAR(50),
    @EventKey  NVARCHAR(100),
    @Recipient NVARCHAR(256),
    @SentAtUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO worker.NotificationLog (TicketId, EventKey, Recipient, SentAtUtc)
        SELECT @TicketId, @EventKey, @Recipient, @SentAtUtc
        WHERE NOT EXISTS (
            SELECT 1 FROM worker.NotificationLog WITH (UPDLOCK, HOLDLOCK)
            WHERE TicketId = @TicketId AND EventKey = @EventKey AND Recipient = @Recipient);

        SELECT CAST(@@ROWCOUNT AS BIT) AS Claimed;
    END TRY
    BEGIN CATCH
        -- 2601/2627: a concurrent run inserted the same key between the check and the insert.
        IF ERROR_NUMBER() IN (2601, 2627)
            SELECT CAST(0 AS BIT) AS Claimed;
        ELSE
            THROW;
    END CATCH
END
GO

-- Undoes a claim when the send failed, so the job's retry sends it again.
CREATE OR ALTER PROCEDURE worker.usp_NotificationLog_Release
    @TicketId  NVARCHAR(50),
    @EventKey  NVARCHAR(100),
    @Recipient NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM worker.NotificationLog
    WHERE TicketId = @TicketId AND EventKey = @EventKey AND Recipient = @Recipient;
END
GO

/* ===================== JobCursor ===================== */

CREATE OR ALTER PROCEDURE worker.usp_JobCursor_Get
    @JobName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT LastCompletedUtc FROM worker.JobCursor WHERE JobName = @JobName;
END
GO

CREATE OR ALTER PROCEDURE worker.usp_JobCursor_Set
    @JobName          NVARCHAR(100),
    @LastCompletedUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE worker.JobCursor SET LastCompletedUtc = @LastCompletedUtc WHERE JobName = @JobName;
    IF @@ROWCOUNT = 0
        INSERT INTO worker.JobCursor (JobName, LastCompletedUtc) VALUES (@JobName, @LastCompletedUtc);
END
GO

/* ===================== ticket-events ===================== */

-- Every emailable event with SinceUtc < At <= UntilUtc, with the people it concerns:
--   created  : the ticket row itself (Tickets.Created)
--   reply    : a public reply by an agent (Kind = 'Reply'; internal notes are Kind = 'Note')
--   assigned : an "Assigned to ..." event; the ticket's CURRENT agent is returned and the worker
--              only emails them when the event names them (team-only assignments are skipped)
--   resolved : a "Closed by ... with status Resolved" event on a ticket that is still Resolved
CREATE OR ALTER PROCEDURE worker.usp_TicketEvents_GetSince
    @SinceUtc DATETIMEOFFSET,
    @UntilUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.EventType, e.EventId, e.At, e.EventText, e.ReplyHtml,
           t.Id AS TicketId, t.Number, t.Subject, t.Status,
           u.Id AS RequesterId, u.Name AS RequesterName, u.Email AS RequesterEmail,
           a.Id AS AgentId, a.Name AS AgentName, a.Email AS AgentEmail
    FROM (
        SELECT N'created' AS EventType, t.Id AS EventId, t.Id AS TicketId, t.Created AS At,
               CAST(NULL AS NVARCHAR(MAX)) AS EventText, CAST(NULL AS NVARCHAR(MAX)) AS ReplyHtml
        FROM dbo.Tickets t
        WHERE t.Created > @SinceUtc AND t.Created <= @UntilUtc

        UNION ALL
        SELECT N'reply', te.Id, te.TicketId, te.At, NULL, te.Html
        FROM dbo.TicketTimelineEntries te
        WHERE te.Kind = N'Reply' AND te.ByType = N'agent' AND te.At > @SinceUtc AND te.At <= @UntilUtc

        UNION ALL
        SELECT N'assigned', te.Id, te.TicketId, te.At, te.Text, NULL
        FROM dbo.TicketTimelineEntries te
        WHERE te.Kind = N'Event' AND te.Text LIKE N'Assigned to %' AND te.At > @SinceUtc AND te.At <= @UntilUtc

        UNION ALL
        SELECT N'resolved', te.Id, te.TicketId, te.At, te.Text, NULL
        FROM dbo.TicketTimelineEntries te
        WHERE te.Kind = N'Event' AND te.Text LIKE N'Closed by % with status Resolved' AND te.At > @SinceUtc AND te.At <= @UntilUtc
    ) e
    JOIN dbo.Tickets t ON t.Id = e.TicketId
    JOIN dbo.Users u ON u.Id = t.RequesterUserId
    LEFT JOIN dbo.Agents a ON a.Id = t.AssignedAgentId AND a.Active = 1
    ORDER BY e.At, e.EventId;
END
GO

/* ===================== sla-sweep ===================== */

-- Active tickets with no public agent reply yet (the SLA is a first-response target; the worker
-- works out the due time from Created + the priority's target in TicketSla), then the active
-- members of every team so a ticket with no agent can go to its team.
CREATE OR ALTER PROCEDURE worker.usp_SlaSweep_GetCandidates
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id AS TicketId, t.Number, t.Subject, t.Status, t.Priority, t.Created,
           (SELECT MIN(te.At) FROM dbo.TicketTimelineEntries te
            WHERE te.TicketId = t.Id AND te.Kind = N'Reply' AND te.ByType = N'agent') AS FirstAgentReplyAt,
           a.Id AS AgentId, a.Name AS AgentName, a.Email AS AgentEmail,
           tm.Id AS TeamId, tm.Name AS TeamName
    FROM dbo.Tickets t
    LEFT JOIN dbo.Agents a ON a.Id = t.AssignedAgentId AND a.Active = 1
    LEFT JOIN dbo.Teams tm ON tm.Id = t.AssignedTeamId
    WHERE t.Status IN (N'Open', N'InProgress', N'AwaitingUser')
      AND NOT EXISTS (SELECT 1 FROM dbo.TicketTimelineEntries te
                      WHERE te.TicketId = t.Id AND te.Kind = N'Reply' AND te.ByType = N'agent');

    SELECT m.TeamId, a.Id AS AgentId, a.Name, a.Email
    FROM dbo.TeamMembers m
    JOIN dbo.Agents a ON a.Id = m.AgentId AND a.Active = 1;
END
GO

/* ===================== auto-close ===================== */

-- Resolved tickets and when they were resolved: the latest "Closed by ..." event, as
-- usp_Dashboard_GetTicketClosedAt does, falling back to Updated.
CREATE OR ALTER PROCEDURE worker.usp_AutoClose_GetResolved
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id AS TicketId, t.Number,
           COALESCE(
               (SELECT TOP (1) te.At FROM dbo.TicketTimelineEntries te
                WHERE te.TicketId = t.Id AND te.Kind = N'Event' AND te.Text LIKE N'Closed by%'
                ORDER BY te.At DESC),
               t.Updated) AS ResolvedAt
    FROM dbo.Tickets t
    WHERE t.Status = N'Resolved';
END
GO

-- Closes one Resolved ticket the way the Api's changeStatus does for a system actor: status,
-- the "Closed by System with status Closed" timeline event (via usp_TicketTimelineEntry_Insert)
-- and a "Changed status to Closed" audit entry by "system". Only if it's still Resolved, so a
-- ticket reopened since the candidates were read is left alone. Closed = 1 when it closed it.
CREATE OR ALTER PROCEDURE worker.usp_AutoClose_CloseTicket
    @TicketId    NVARCHAR(50),
    @AtUtc       DATETIMEOFFSET,
    @EntryId     NVARCHAR(50),
    @AuditId     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.Tickets
    SET Status = N'Closed', Updated = @AtUtc
    WHERE Id = @TicketId AND Status = N'Resolved';

    IF @@ROWCOUNT = 0
    BEGIN
        COMMIT TRANSACTION;
        SELECT CAST(0 AS BIT) AS Closed;
        RETURN;
    END

    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = @EntryId, @TicketId = @TicketId, @Kind = N'Event', @At = @AtUtc,
        @ByType = N'system', @ById = NULL, @Text = N'Closed by System with status Closed';

    DECLARE @Target NVARCHAR(400) = N'#' + (SELECT Number FROM dbo.Tickets WHERE Id = @TicketId);
    EXEC dbo.usp_Audit_Insert
        @Id = @AuditId, @ActorId = N'system', @Action = N'Changed status to Closed',
        @Target = @Target, @Category = N'Ticket';

    COMMIT TRANSACTION;
    SELECT CAST(1 AS BIT) AS Closed;
END
GO

/* ===================== daily-digest ===================== */

-- Teams whose lead is an active agent with an email, then every active ticket assigned to a
-- team, with what the digest needs to show it as open or overdue.
CREATE OR ALTER PROCEDURE worker.usp_Digest_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tm.Id AS TeamId, tm.Name AS TeamName, a.Id AS LeadAgentId, a.Name AS LeadName, a.Email AS LeadEmail
    FROM dbo.Teams tm
    JOIN dbo.Agents a ON a.Id = tm.LeadAgentId AND a.Active = 1
    WHERE a.Email <> N'';

    SELECT t.Id AS TicketId, t.Number, t.Subject, t.Status, t.Priority, t.Created, t.AssignedTeamId AS TeamId,
           (SELECT MIN(te.At) FROM dbo.TicketTimelineEntries te
            WHERE te.TicketId = t.Id AND te.Kind = N'Reply' AND te.ByType = N'agent') AS FirstAgentReplyAt,
           a.Name AS AgentName
    FROM dbo.Tickets t
    LEFT JOIN dbo.Agents a ON a.Id = t.AssignedAgentId
    WHERE t.Status IN (N'Open', N'InProgress', N'AwaitingUser') AND t.AssignedTeamId IS NOT NULL
    ORDER BY t.Created;
END
GO
