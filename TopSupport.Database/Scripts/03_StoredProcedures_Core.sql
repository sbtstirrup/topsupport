/*
    03_StoredProcedures_Core.sql
    First representative set of stored procedures, enough to prove the ADO.NET repository
    pattern end to end (TopSupport.Api/Data/TicketRepository.cs). Full CRUD coverage for
    every entity comes later, once the ticket-raise and queue flows are built out.
*/
USE TopSupport;
GO

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
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_GetByRequester
    @RequesterUserId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Number, Subject, Topic, Department, Status, Priority,
           RequesterUserId, AffectedUserId, CreatedByType, CreatedById,
           Created, Updated, AssignedAgentId, AssignedTeamId, FieldsJson
    FROM dbo.Tickets
    WHERE RequesterUserId = @RequesterUserId
    ORDER BY Created DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Ticket_Insert
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
