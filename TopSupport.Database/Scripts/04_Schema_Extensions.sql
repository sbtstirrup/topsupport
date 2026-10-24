/*
    04_Schema_Extensions.sql
    Tier 1 schema additions on top of 02_Schema_Core.sql: ticket attachments, canned
    responses, and the ticket-numbering sequence (replacing the JS demo's in-memory
    nextNum counter with an atomic, race-free SQL Server SEQUENCE).

    Ticket merge is explicitly out of scope for this project -- no MergedInto column is
    added anywhere.
*/
USE TopSupport;
GO

IF OBJECT_ID(N'dbo.TicketAttachments', N'U') IS NULL
BEGIN
    -- TimelineEntryId IS NULL means the file was attached at ticket-creation time
    -- (before any timeline entry existed to anchor it to); otherwise it was attached
    -- alongside a reply/note and is anchored to that entry.
    CREATE TABLE dbo.TicketAttachments
    (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TicketId        NVARCHAR(50)    NOT NULL,
        TimelineEntryId NVARCHAR(50)    NULL,
        FileName        NVARCHAR(260)   NOT NULL,
        ContentType     NVARCHAR(200)   NOT NULL,
        SizeBytes       BIGINT          NOT NULL,
        Content         VARBINARY(MAX)  NOT NULL,
        UploadedByType  NVARCHAR(10)    NOT NULL,  -- 'user' | 'agent' | 'system'
        UploadedById    NVARCHAR(50)    NULL,
        UploadedAt      DATETIMEOFFSET  NOT NULL,
        CONSTRAINT FK_TicketAttachments_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TicketAttachments_TimelineEntries FOREIGN KEY (TimelineEntryId) REFERENCES dbo.TicketTimelineEntries(Id)
    );
    CREATE INDEX IX_TicketAttachments_TicketId ON dbo.TicketAttachments(TicketId, UploadedAt);
END
GO

IF OBJECT_ID(N'dbo.CannedResponses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CannedResponses
    (
        Id        NVARCHAR(50)  NOT NULL PRIMARY KEY,
        Title     NVARCHAR(200) NOT NULL,
        Body      NVARCHAR(MAX) NOT NULL,
        SortOrder INT           NOT NULL DEFAULT 0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'TicketNumberSeq' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    EXEC('CREATE SEQUENCE dbo.TicketNumberSeq AS INT START WITH 60001 INCREMENT BY 1;');
END
GO
