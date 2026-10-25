/*
    17_Admin_WorkerMonitor.sql
    The admin "Background Jobs" page: what TopSupport.Worker is doing and what it has emailed.
    Read-only views over Hangfire's tables (the "jobs" schema, created by the worker on its first
    start) and worker.NotificationLog (15_Worker.sql, which must be applied first).

    Hangfire stores recurring-job times as Unix milliseconds in jobs.Hash and its own timestamps
    as UTC DATETIME; both are returned as-is and converted by the Api's repository. Until the
    worker has run once the jobs tables don't exist, so the procs return empty result sets.
*/
USE TopSupport;
GO

-- Three result sets: the worker servers (with heartbeats), the recurring jobs (schedule, last
-- and next run, outcome of the last run) and the most recent @Top job runs, newest first.
CREATE OR ALTER PROCEDURE worker.usp_Monitor_GetJobs
    @Top INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'jobs.Hash', N'U') IS NULL OR OBJECT_ID(N'jobs.Job', N'U') IS NULL
    BEGIN
        SELECT CAST(NULL AS NVARCHAR(200)) AS ServerId, CAST(NULL AS DATETIME) AS LastHeartbeat,
               CAST(NULL AS NVARCHAR(50)) AS StartedAt
        WHERE 1 = 0;
        SELECT CAST(NULL AS NVARCHAR(256)) AS RecurringJobId, CAST(NULL AS NVARCHAR(100)) AS Cron,
               CAST(NULL AS NVARCHAR(100)) AS TimeZoneId, CAST(NULL AS BIGINT) AS LastExecutionMs,
               CAST(NULL AS BIGINT) AS NextExecutionMs, CAST(NULL AS BIGINT) AS LastJobId,
               CAST(NULL AS NVARCHAR(20)) AS LastState, CAST(NULL AS NVARCHAR(MAX)) AS LastError,
               CAST(NULL AS NVARCHAR(MAX)) AS ScheduleError
        WHERE 1 = 0;
        SELECT CAST(NULL AS BIGINT) AS JobId, CAST(NULL AS NVARCHAR(256)) AS RecurringJobId,
               CAST(NULL AS NVARCHAR(512)) AS JobType, CAST(NULL AS NVARCHAR(20)) AS State,
               CAST(NULL AS DATETIME) AS CreatedAt, CAST(NULL AS DATETIME) AS StateAt,
               CAST(NULL AS NVARCHAR(100)) AS Reason, CAST(NULL AS NVARCHAR(MAX)) AS Error,
               CAST(NULL AS BIGINT) AS DurationMs
        WHERE 1 = 0;
        RETURN;
    END

    SELECT s.Id AS ServerId, s.LastHeartbeat, JSON_VALUE(s.Data, N'$.StartedAt') AS StartedAt
    FROM jobs.Server s
    ORDER BY s.LastHeartbeat DESC;

    WITH recurring AS (
        SELECT rs.Value AS RecurringJobId,
               MAX(CASE WHEN h.Field = N'Cron' THEN h.Value END) AS Cron,
               MAX(CASE WHEN h.Field = N'TimeZoneId' THEN h.Value END) AS TimeZoneId,
               TRY_CAST(MAX(CASE WHEN h.Field = N'LastExecution' THEN h.Value END) AS BIGINT) AS LastExecutionMs,
               TRY_CAST(MAX(CASE WHEN h.Field = N'NextExecution' THEN h.Value END) AS BIGINT) AS NextExecutionMs,
               TRY_CAST(MAX(CASE WHEN h.Field = N'LastJobId' THEN h.Value END) AS BIGINT) AS LastJobId,
               MAX(CASE WHEN h.Field = N'Error' THEN h.Value END) AS ScheduleError
        FROM jobs.[Set] rs
        LEFT JOIN jobs.Hash h ON h.[Key] = N'recurring-job:' + rs.Value
        WHERE rs.[Key] = N'recurring-jobs'
        GROUP BY rs.Value
    )
    SELECT r.RecurringJobId, r.Cron, r.TimeZoneId, r.LastExecutionMs, r.NextExecutionMs, r.LastJobId,
           j.StateName AS LastState,
           CASE WHEN j.StateName = N'Failed' THEN JSON_VALUE(st.Data, N'$.ExceptionMessage')
                WHEN j.StateName = N'Scheduled' THEN st.Reason END AS LastError,
           r.ScheduleError
    FROM recurring r
    LEFT JOIN jobs.Job j ON j.Id = r.LastJobId
    LEFT JOIN jobs.State st ON st.Id = j.StateId
    ORDER BY r.RecurringJobId;

    SELECT TOP (@Top)
           j.Id AS JobId,
           REPLACE(p.Value, N'"', N'') AS RecurringJobId,
           JSON_VALUE(j.InvocationData, N'$.t') AS JobType,
           j.StateName AS State,
           j.CreatedAt,
           st.CreatedAt AS StateAt,
           st.Reason,
           COALESCE(JSON_VALUE(st.Data, N'$.ExceptionMessage'), CASE WHEN j.StateName = N'Scheduled' THEN st.Reason END) AS Error,
           TRY_CAST(JSON_VALUE(st.Data, N'$.PerformanceDuration') AS BIGINT) AS DurationMs
    FROM jobs.Job j
    LEFT JOIN jobs.JobParameter p ON p.JobId = j.Id AND p.Name = N'RecurringJobId'
    LEFT JOIN jobs.State st ON st.Id = j.StateId
    ORDER BY j.Id DESC;
END
GO

-- What the worker has emailed (worker.NotificationLog), filtered and paged, newest first.
--   Result 1: counts per event type (the part of EventKey before ':') for the last 24 hours,
--             the last 7 days and all time, regardless of the filters.
--   Result 2: how many rows match the filters.
--   Result 3: one page of matching rows, with the ticket's number and subject when it still exists.
-- @Search is matched with LIKE against recipient, ticket number and subject; the Api escapes
-- LIKE wildcards before calling.
CREATE OR ALTER PROCEDURE worker.usp_Monitor_GetNotifications
    @EventType NVARCHAR(50)  = NULL,
    @Search    NVARCHAR(400) = NULL,
    @Page      INT = 1,
    @PageSize  INT = 25,
    @NowUtc    DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    WITH typed AS (
        SELECT LEFT(EventKey, CHARINDEX(N':', EventKey + N':') - 1) AS EventType, SentAtUtc
        FROM worker.NotificationLog
    )
    SELECT EventType,
           SUM(CASE WHEN SentAtUtc >= DATEADD(HOUR, -24, @NowUtc) THEN 1 ELSE 0 END) AS Last24Hours,
           SUM(CASE WHEN SentAtUtc >= DATEADD(DAY, -7, @NowUtc) THEN 1 ELSE 0 END) AS Last7Days,
           COUNT(*) AS AllTime
    FROM typed
    GROUP BY EventType;

    WITH rows AS (
        SELECT n.Id, n.SentAtUtc, n.TicketId, t.Number AS TicketNumber, t.Subject,
               LEFT(n.EventKey, CHARINDEX(N':', n.EventKey + N':') - 1) AS EventType,
               n.EventKey, n.Recipient
        FROM worker.NotificationLog n
        LEFT JOIN dbo.Tickets t ON t.Id = n.TicketId
    )
    SELECT * INTO #matches
    FROM rows
    WHERE (@EventType IS NULL OR EventType = @EventType)
      AND (@Search IS NULL
           OR Recipient LIKE N'%' + @Search + N'%'
           OR TicketNumber LIKE N'%' + @Search + N'%'
           OR Subject LIKE N'%' + @Search + N'%');

    SELECT COUNT(*) AS Total FROM #matches;

    SELECT Id, SentAtUtc, TicketId, TicketNumber, Subject, EventType, EventKey, Recipient
    FROM #matches
    ORDER BY SentAtUtc DESC, Id DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
