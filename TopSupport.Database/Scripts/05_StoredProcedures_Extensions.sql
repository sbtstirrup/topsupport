/*
    05_StoredProcedures_Extensions.sql
    Tier 1 stored procedure additions/replacements on top of 03_StoredProcedures_Core.sql.
    All business rules (SLA/overdue, queue membership, number formatting, etc.) live in
    TopSupport.Services -- these procs are dumb CRUD/query only.
*/
USE TopSupport;
GO

/* ===================== Tickets ===================== */

-- Rename of usp_Ticket_Insert -> usp_Ticket_Create (same shape as the original proc).
-- The old usp_Ticket_Insert is left in place in 03_StoredProcedures_Core.sql for
-- backward compatibility but should be considered superseded by this proc.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_Create
    @Id              NVARCHAR(50),
    @Number          NVARCHAR(10),
    @Subject         NVARCHAR(300),
    @Topic           NVARCHAR(50),
    @Department      NVARCHAR(100),
    @Status          NVARCHAR(20),
    @Priority        NVARCHAR(20),
    @RequesterUserId NVARCHAR(50),
    @AffectedUserId  NVARCHAR(50),
    @CreatedByType   NVARCHAR(10),
    @CreatedById     NVARCHAR(50) = NULL,
    @Created         DATETIMEOFFSET,
    @Updated         DATETIMEOFFSET,
    @AssignedAgentId NVARCHAR(50) = NULL,
    @AssignedTeamId  NVARCHAR(50) = NULL,
    @FieldsJson      NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tickets
        (Id, Number, Subject, Topic, Department, Status, Priority,
         RequesterUserId, AffectedUserId, CreatedByType, CreatedById,
         Created, Updated, AssignedAgentId, AssignedTeamId, FieldsJson)
    VALUES
        (@Id, @Number, @Subject, @Topic, @Department, @Status, @Priority,
         @RequesterUserId, @AffectedUserId, @CreatedByType, @CreatedById,
         @Created, @Updated, @AssignedAgentId, @AssignedTeamId, @FieldsJson);
END
GO

-- Extends usp_Ticket_GetById (03_StoredProcedures_Core.sql) with a 5th result set:
-- attachment metadata only, no Content column. Replaces the original proc definition.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_GetById
    @TicketId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Number, Subject, Topic, Department, Status, Priority,
           RequesterUserId, AffectedUserId, CreatedByType, CreatedById,
           Created, Updated, AssignedAgentId, AssignedTeamId, FieldsJson
    FROM dbo.Tickets
    WHERE Id = @TicketId;

    SELECT Id, TicketId, Kind, At, ByType, ById, Text, Html
    FROM dbo.TicketTimelineEntries
    WHERE TicketId = @TicketId
    ORDER BY At ASC;

    SELECT Id, TicketId, Text, AssigneeAgentId, Due, Done
    FROM dbo.TicketTasks
    WHERE TicketId = @TicketId;

    SELECT AgentId
    FROM dbo.TicketViewers
    WHERE TicketId = @TicketId;

    SELECT Id, TicketId, TimelineEntryId, FileName, ContentType, SizeBytes,
           UploadedByType, UploadedById, UploadedAt
    FROM dbo.TicketAttachments
    WHERE TicketId = @TicketId
    ORDER BY UploadedAt ASC;
END
GO

-- Replaces usp_Ticket_GetByRequester for "My Tickets": requester OR affected.
-- The old usp_Ticket_GetByRequester is left in place in 03_StoredProcedures_Core.sql.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_GetByRequesterOrAffected
    @UserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Number, Subject, Topic, Department, Status, Priority,
           RequesterUserId, AffectedUserId, CreatedByType, CreatedById,
           Created, Updated, AssignedAgentId, AssignedTeamId, FieldsJson
    FROM dbo.Tickets
    WHERE RequesterUserId = @UserId OR AffectedUserId = @UserId
    ORDER BY Created DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_UpdateStatus
    @TicketId NVARCHAR(50),
    @Status   NVARCHAR(20),
    @Updated  DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tickets
    SET Status = @Status, Updated = @Updated
    WHERE Id = @TicketId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_UpdatePriority
    @TicketId NVARCHAR(50),
    @Priority NVARCHAR(20),
    @Updated  DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tickets
    SET Priority = @Priority, Updated = @Updated
    WHERE Id = @TicketId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_UpdateTopic
    @TicketId   NVARCHAR(50),
    @Topic      NVARCHAR(50),
    @Department NVARCHAR(100),
    @Updated    DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tickets
    SET Topic = @Topic, Department = @Department, Updated = @Updated
    WHERE Id = @TicketId;
END
GO

-- Writes whatever nullable (AssignedAgentId, AssignedTeamId) pair it's given -- the
-- caller/service pre-computes the correct pair (e.g. team-assign clears the agent,
-- agent-assign defaults the team) per the JS demo's assignTicket behavior.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_Assign
    @TicketId        NVARCHAR(50),
    @AssignedAgentId NVARCHAR(50) = NULL,
    @AssignedTeamId  NVARCHAR(50) = NULL,
    @Updated         DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tickets
    SET AssignedAgentId = @AssignedAgentId, AssignedTeamId = @AssignedTeamId, Updated = @Updated
    WHERE Id = @TicketId;
END
GO

-- Claim (ACT['t-claim']) as a compare-and-set: only assigns while the ticket's agent is still the
-- one the caller saw (@ExpectedAgentId, NULL = unassigned), so of two simultaneous claims exactly
-- one updates a row. Returns Affected = 0 when someone else got there first.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_ClaimIfUnchanged
    @TicketId        NVARCHAR(50),
    @AgentId         NVARCHAR(50),
    @AssignedTeamId  NVARCHAR(50) = NULL,
    @ExpectedAgentId NVARCHAR(50) = NULL,
    @Updated         DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Tickets
    SET AssignedAgentId = @AgentId, AssignedTeamId = @AssignedTeamId, Updated = @Updated
    WHERE Id = @TicketId
      AND ((@ExpectedAgentId IS NULL AND AssignedAgentId IS NULL) OR AssignedAgentId = @ExpectedAgentId);

    SELECT @@ROWCOUNT AS Affected;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_NextNumber
AS
BEGIN
    SET NOCOUNT ON;

    SELECT NEXT VALUE FOR dbo.TicketNumberSeq AS NextNumber;
END
GO

/* ===================== Timeline / Tasks ===================== */

-- Also bumps dbo.Tickets.Updated, since every addEntry call in the JS demo does this.
CREATE OR ALTER PROCEDURE dbo.usp_TicketTimelineEntry_Insert
    @Id       NVARCHAR(50),
    @TicketId NVARCHAR(50),
    @Kind     NVARCHAR(10),
    @At       DATETIMEOFFSET,
    @ByType   NVARCHAR(10),
    @ById     NVARCHAR(50) = NULL,
    @Text     NVARCHAR(MAX) = NULL,
    @Html     NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TicketTimelineEntries (Id, TicketId, Kind, At, ByType, ById, Text, Html)
    VALUES (@Id, @TicketId, @Kind, @At, @ByType, @ById, @Text, @Html);

    UPDATE dbo.Tickets
    SET Updated = @At
    WHERE Id = @TicketId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TicketTask_Insert
    @Id              NVARCHAR(50),
    @TicketId        NVARCHAR(50),
    @Text            NVARCHAR(300),
    @AssigneeAgentId NVARCHAR(50),
    @Due             DATE = NULL,
    @Done            BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TicketTasks (Id, TicketId, Text, AssigneeAgentId, Due, Done)
    VALUES (@Id, @TicketId, @Text, @AssigneeAgentId, @Due, @Done);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TicketTask_SetDone
    @TaskId NVARCHAR(50),
    @Done   BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TicketTasks
    SET Done = @Done
    WHERE Id = @TaskId;
END
GO

/* ===================== Attachments ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_TicketAttachment_Insert
    @Id              NVARCHAR(50),
    @TicketId        NVARCHAR(50),
    @TimelineEntryId NVARCHAR(50) = NULL,
    @FileName        NVARCHAR(260),
    @ContentType     NVARCHAR(200),
    @SizeBytes       BIGINT,
    @Content         VARBINARY(MAX),
    @UploadedByType  NVARCHAR(10),
    @UploadedById    NVARCHAR(50) = NULL,
    @UploadedAt      DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TicketAttachments
        (Id, TicketId, TimelineEntryId, FileName, ContentType, SizeBytes, Content,
         UploadedByType, UploadedById, UploadedAt)
    VALUES
        (@Id, @TicketId, @TimelineEntryId, @FileName, @ContentType, @SizeBytes, @Content,
         @UploadedByType, @UploadedById, @UploadedAt);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TicketAttachment_GetMetadataByTicket
    @TicketId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, TicketId, TimelineEntryId, FileName, ContentType, SizeBytes,
           UploadedByType, UploadedById, UploadedAt
    FROM dbo.TicketAttachments
    WHERE TicketId = @TicketId
    ORDER BY UploadedAt ASC;
END
GO

-- The ONLY proc allowed to select the Content column.
CREATE OR ALTER PROCEDURE dbo.usp_TicketAttachment_GetContent
    @AttachmentId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT FileName, ContentType, SizeBytes, Content
    FROM dbo.TicketAttachments
    WHERE Id = @AttachmentId;
END
GO

/* ===================== Canned Responses ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_CannedResponse_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Body, SortOrder
    FROM dbo.CannedResponses
    ORDER BY SortOrder ASC, Title ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CannedResponse_Insert
    @Id        NVARCHAR(50),
    @Title     NVARCHAR(200),
    @Body      NVARCHAR(MAX),
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.CannedResponses (Id, Title, Body, SortOrder)
    VALUES (@Id, @Title, @Body, @SortOrder);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CannedResponse_Update
    @Id        NVARCHAR(50),
    @Title     NVARCHAR(200),
    @Body      NVARCHAR(MAX),
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CannedResponses
    SET Title = @Title, Body = @Body, SortOrder = @SortOrder
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CannedResponse_Delete
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.CannedResponses WHERE Id = @Id;
END
GO

/* ===================== Queue ===================== */

-- All active tickets (Open/InProgress/AwaitingUser) plus two computed columns needed
-- for overdue/answered logic in QueueService (SLA_HOURS and queue-membership rules
-- stay entirely in C#, not duplicated here):
--   FirstAgentReplyAt : earliest Reply-kind entry posted by an agent (first response time)
--   LastReplyByType   : ByType of the most recent Reply-kind entry (NULL if none yet)
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_GetActiveForQueue
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id, t.Number, t.Subject, t.Topic, t.Department, t.Status, t.Priority,
           t.RequesterUserId, t.AffectedUserId, t.CreatedByType, t.CreatedById,
           t.Created, t.Updated, t.AssignedAgentId, t.AssignedTeamId, t.FieldsJson,
           (
               SELECT MIN(te.At)
               FROM dbo.TicketTimelineEntries te
               WHERE te.TicketId = t.Id AND te.Kind = 'Reply' AND te.ByType = 'agent'
           ) AS FirstAgentReplyAt,
           (
               SELECT TOP (1) te2.ByType
               FROM dbo.TicketTimelineEntries te2
               WHERE te2.TicketId = t.Id AND te2.Kind = 'Reply'
               ORDER BY te2.At DESC
           ) AS LastReplyByType
    FROM dbo.Tickets t
    WHERE t.Status IN ('Open', 'InProgress', 'AwaitingUser')
    ORDER BY t.Created DESC;
END
GO

-- Equality filters where each parameter is NULL-or-match, plus an optional free-text
-- search across Number/Subject/requester-or-affected user Name/FieldsJson. No paging
-- or sorting in SQL -- QueueService does that in C# over the returned row set.
CREATE OR ALTER PROCEDURE dbo.usp_Ticket_SearchForQueue
    @Status          NVARCHAR(20)  = NULL,
    @Priority        NVARCHAR(20)  = NULL,
    @Topic           NVARCHAR(50)  = NULL,
    @Department      NVARCHAR(100) = NULL,
    @AssignedAgentId NVARCHAR(50)  = NULL,
    @AssignedTeamId  NVARCHAR(50)  = NULL,
    -- 200 searchable characters (FieldLimits.SearchText), LIKE-escaped by TicketRepository:
    -- each %, _ or [ grows to 3 characters, so allow 3x before SQL Server would truncate.
    @SearchText      NVARCHAR(600) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id, t.Number, t.Subject, t.Topic, t.Department, t.Status, t.Priority,
           t.RequesterUserId, t.AffectedUserId, t.CreatedByType, t.CreatedById,
           t.Created, t.Updated, t.AssignedAgentId, t.AssignedTeamId, t.FieldsJson,
           (
               SELECT MIN(te.At)
               FROM dbo.TicketTimelineEntries te
               WHERE te.TicketId = t.Id AND te.Kind = 'Reply' AND te.ByType = 'agent'
           ) AS FirstAgentReplyAt,
           (
               SELECT TOP (1) te2.ByType
               FROM dbo.TicketTimelineEntries te2
               WHERE te2.TicketId = t.Id AND te2.Kind = 'Reply'
               ORDER BY te2.At DESC
           ) AS LastReplyByType
    FROM dbo.Tickets t
    WHERE (@Status IS NULL OR t.Status = @Status)
      AND (@Priority IS NULL OR t.Priority = @Priority)
      AND (@Topic IS NULL OR t.Topic = @Topic)
      AND (@Department IS NULL OR t.Department = @Department)
      AND (@AssignedAgentId IS NULL OR t.AssignedAgentId = @AssignedAgentId)
      AND (@AssignedTeamId IS NULL OR t.AssignedTeamId = @AssignedTeamId)
      AND (
            @SearchText IS NULL
            OR t.Number LIKE '%' + @SearchText + '%'
            OR t.Subject LIKE '%' + @SearchText + '%'
            OR EXISTS (
                SELECT 1 FROM dbo.Users ru
                WHERE ru.Id IN (t.RequesterUserId, t.AffectedUserId)
                  AND ru.Name LIKE '%' + @SearchText + '%'
            )
            OR t.FieldsJson LIKE '%' + @SearchText + '%'
          )
    ORDER BY t.Created DESC;
END
GO

/* ===================== Lookup: Users ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Created
    FROM dbo.Users
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetAllActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Created
    FROM dbo.Users
    WHERE Locked = 0
    ORDER BY Name ASC;
END
GO

/* ===================== Lookup: Agents ===================== */

-- Multi-result-set shape (same pattern as usp_Ticket_GetById): core agent row, then a
-- second set of (AgentId, Department) rows from AgentDepartments, then a third of
-- (TeamId, AgentId) rows from TeamMembers restricted to this agent.
CREATE OR ALTER PROCEDURE dbo.usp_Agent_GetById
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, Role, JobTitle, Active, Locked
    FROM dbo.Agents
    WHERE Id = @Id;

    SELECT AgentId, Department
    FROM dbo.AgentDepartments
    WHERE AgentId = @Id;

    SELECT TeamId, AgentId
    FROM dbo.TeamMembers
    WHERE AgentId = @Id;
END
GO

-- Same multi-set pattern as usp_Agent_GetById, but for all active agents at once: the
-- caller groups the 2nd/3rd result sets by AgentId in C#.
CREATE OR ALTER PROCEDURE dbo.usp_Agent_GetAllActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, Role, JobTitle, Active, Locked
    FROM dbo.Agents
    WHERE Active = 1
    ORDER BY Name ASC;

    SELECT ad.AgentId, ad.Department
    FROM dbo.AgentDepartments ad
    INNER JOIN dbo.Agents a ON a.Id = ad.AgentId
    WHERE a.Active = 1;

    SELECT tm.TeamId, tm.AgentId
    FROM dbo.TeamMembers tm
    INNER JOIN dbo.Agents a ON a.Id = tm.AgentId
    WHERE a.Active = 1;
END
GO

/* ===================== Lookup: Teams ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Team_GetById
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, LeadAgentId, Department
    FROM dbo.Teams
    WHERE Id = @Id;

    SELECT TeamId, AgentId
    FROM dbo.TeamMembers
    WHERE TeamId = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Team_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, LeadAgentId, Department
    FROM dbo.Teams
    ORDER BY Name ASC;

    SELECT TeamId, AgentId
    FROM dbo.TeamMembers;
END
GO

/* ===================== Lookup: Assets ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Asset_GetByTag
    @Tag NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Tag, Type, Model, UserId
    FROM dbo.Assets
    WHERE Tag = @Tag;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Asset_GetByUserId
    @UserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Tag, Type, Model, UserId
    FROM dbo.Assets
    WHERE UserId = @UserId
    ORDER BY Tag ASC;
END
GO
