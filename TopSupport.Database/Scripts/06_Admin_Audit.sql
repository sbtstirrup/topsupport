/*
    06_Admin_Audit.sql
    Audit trail (DB.audit / audit(action, target, cat) in topsupport-app.js): table, stored
    procedures and the 16 seed entries from buildSeed()'s auditSeed array. Seed timestamps are
    relative to SYSDATETIMEOFFSET() at run time, matching the demo's "hours ago" pattern.
    Every admin feature writes here through IAuditService.LogAsync.
*/
USE TopSupport;
GO

IF OBJECT_ID(N'dbo.AuditEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditEntries
    (
        Id       NVARCHAR(50)   NOT NULL PRIMARY KEY,
        At       DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        ActorId  NVARCHAR(50)   NOT NULL,   -- Users.Id or Agents.Id
        Action   NVARCHAR(400)  NOT NULL,
        Target   NVARCHAR(400)  NOT NULL,
        Category NVARCHAR(30)   NOT NULL,   -- 'Sign-in' | 'Ticket' | 'User' | 'Organisation' | 'Permission'
        CONSTRAINT CK_AuditEntries_Category CHECK (Category IN (N'Sign-in', N'Ticket', N'User', N'Organisation', N'Permission'))
    );
    CREATE INDEX IX_AuditEntries_At ON dbo.AuditEntries(At DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Audit_Insert
    @Id       NVARCHAR(50),
    @ActorId  NVARCHAR(50),
    @Action   NVARCHAR(400),
    @Target   NVARCHAR(400),
    @Category NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditEntries (Id, At, ActorId, Action, Target, Category)
    VALUES (@Id, SYSDATETIMEOFFSET(), @ActorId, @Action, @Target, @Category);
END
GO

-- Newest first. The demo filters the whole DB.audit array client-side; the Blazor page does the
-- same over this result, capped so a long-lived dev database can't return an unbounded set.
CREATE OR ALTER PROCEDURE dbo.usp_Audit_GetRecent
    @Top INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top) Id, At, ActorId, Action, Target, Category
    FROM dbo.AuditEntries
    ORDER BY At DESC;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AuditEntries WHERE Id = 'au0')
BEGIN
    DECLARE @now DATETIMEOFFSET = SYSDATETIMEOFFSET();
    INSERT INTO dbo.AuditEntries (Id, At, ActorId, Action, Target, Category) VALUES
        ('au0',  DATEADD(MINUTE, -24,   @now), 'a1', N'Signed in', N'Agent portal', N'Sign-in'),
        ('au1',  DATEADD(MINUTE, -48,   @now), 'u1', N'Signed in', N'User portal', N'Sign-in'),
        ('au2',  DATEADD(MINUTE, -90,   @now), 'a2', N'Opened ticket', N'#060026', N'Ticket'),
        ('au3',  DATEADD(MINUTE, -180,  @now), 'a5', N'Changed permission "Delete" for Agent role', N'IT Business Support', N'Permission'),
        ('au4',  DATEADD(MINUTE, -300,  @now), 'a3', N'Assigned ticket to E. Shaw', N'#060022', N'Ticket'),
        ('au5',  DATEADD(MINUTE, -420,  @now), 'a1', N'Changed status to In Progress', N'#060014', N'Ticket'),
        ('au6',  DATEADD(MINUTE, -540,  @now), 'a5', N'Unlocked user account', N'Marcus Webb', N'User'),
        ('au7',  DATEADD(MINUTE, -600,  @now), 'a5', N'Reset password', N'Marcus Webb', N'User'),
        ('au8',  DATEADD(MINUTE, -1320, @now), 'u7', N'Signed in', N'User portal', N'Sign-in'),
        ('au9',  DATEADD(MINUTE, -1560, @now), 'a4', N'Failed sign-in (wrong password)', N'Agent portal', N'Sign-in'),
        ('au10', DATEADD(MINUTE, -1566, @now), 'a4', N'Signed in', N'Agent portal', N'Sign-in'),
        ('au11', DATEADD(MINUTE, -1800, @now), 'a5', N'Updated security setting "Session timeout" to 30 minutes', N'Security', N'Permission'),
        ('au12', DATEADD(MINUTE, -2880, @now), 'a5', N'Created user', N'Owen Pryce', N'User'),
        ('au13', DATEADD(MINUTE, -3120, @now), 'a3', N'Merged ticket into #060011', N'#060009', N'Ticket'),
        ('au14', DATEADD(MINUTE, -4200, @now), 'a5', N'Added Ellie Shaw to department Infrastructure', N'Ellie Shaw', N'Permission'),
        ('au15', DATEADD(MINUTE, -5760, @now), 'a2', N'Closed ticket with status Closed', N'#060004', N'Ticket');
END
GO
