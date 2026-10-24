/*
    11_Admin_KnowledgeBase.sql
    Knowledge base (DB.kb in topsupport-app.js): FAQ categories + help articles, the stored
    procedures behind PAGES.kb / PAGES.kbedit / the landing page's "Popular Help Articles", and
    the 4 categories + 8 articles from buildSeed()'s kbCats/kbArticles arrays. Article UpdatedAt
    seed values are relative to SYSDATETIMEOFFSET() at run time, matching the demo's
    iso(nowMs - N * 24 * H) pattern.
*/
USE TopSupport;
GO

IF OBJECT_ID(N'dbo.KbCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KbCategories
    (
        Id          NVARCHAR(50)  NOT NULL PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Description NVARCHAR(400) NOT NULL,
        SortOrder   INT           NOT NULL DEFAULT 0   -- the demo lists DB.kb.cats in array order
    );
END
GO

IF OBJECT_ID(N'dbo.KbArticles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KbArticles
    (
        Id         NVARCHAR(50)   NOT NULL PRIMARY KEY,
        CategoryId NVARCHAR(50)   NOT NULL CONSTRAINT FK_KbArticles_Category REFERENCES dbo.KbCategories(Id),
        Title      NVARCHAR(400)  NOT NULL,
        Status     NVARCHAR(20)   NOT NULL,   -- 'Draft' | 'Published'
        Body       NVARCHAR(MAX)  NOT NULL,
        UpdatedAt  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        Views      INT            NOT NULL DEFAULT 0,
        CONSTRAINT CK_KbArticles_Status CHECK (Status IN (N'Draft', N'Published'))
    );
    CREATE INDEX IX_KbArticles_CategoryId ON dbo.KbArticles(CategoryId);
END
GO

-- Two result sets: categories (in display order), then every article with its category name.
-- PAGES.kb filters/sorts DB.kb client-side; the Blazor page does the same over this result.
CREATE OR ALTER PROCEDURE dbo.usp_Kb_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, SortOrder
    FROM dbo.KbCategories
    ORDER BY SortOrder, Name;

    SELECT a.Id, a.CategoryId, c.Name AS CategoryName, a.Title, a.Status, a.Body, a.UpdatedAt, a.Views
    FROM dbo.KbArticles a
    JOIN dbo.KbCategories c ON c.Id = a.CategoryId
    ORDER BY a.UpdatedAt DESC;
END
GO

-- Landing page: published articles, most viewed first (DB.kb.articles.filter(Published)
-- .sort(views desc).slice(0, 3)).
CREATE OR ALTER PROCEDURE dbo.usp_KbArticle_GetPopular
    @Top INT = 3
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top) a.Id, a.CategoryId, c.Name AS CategoryName, a.Title, a.Status, a.Body, a.UpdatedAt, a.Views
    FROM dbo.KbArticles a
    JOIN dbo.KbCategories c ON c.Id = a.CategoryId
    WHERE a.Status = N'Published'
    ORDER BY a.Views DESC, a.UpdatedAt DESC;
END
GO

-- Insert-or-update, matching ACT['ka-save']: a new article starts with 0 views, and either way
-- the title/category/status/body are replaced and UpdatedAt is stamped now. Returns the saved row.
CREATE OR ALTER PROCEDURE dbo.usp_KbArticle_Save
    @Id         NVARCHAR(50),
    @CategoryId NVARCHAR(50),
    @Title      NVARCHAR(400),
    @Status     NVARCHAR(20),
    @Body       NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = @Id)
        UPDATE dbo.KbArticles
        SET CategoryId = @CategoryId, Title = @Title, Status = @Status, Body = @Body, UpdatedAt = SYSDATETIMEOFFSET()
        WHERE Id = @Id;
    ELSE
        INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, Body, UpdatedAt, Views)
        VALUES (@Id, @CategoryId, @Title, @Status, @Body, SYSDATETIMEOFFSET(), 0);

    SELECT a.Id, a.CategoryId, c.Name AS CategoryName, a.Title, a.Status, a.Body, a.UpdatedAt, a.Views
    FROM dbo.KbArticles a
    JOIN dbo.KbCategories c ON c.Id = a.CategoryId
    WHERE a.Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_KbArticle_Delete
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.KbArticles WHERE Id = @Id;
END
GO

-- Seed: buildSeed() kbCats / kbArticles, ported exactly.
IF NOT EXISTS (SELECT 1 FROM dbo.KbCategories WHERE Id = 'k1')
    INSERT INTO dbo.KbCategories (Id, Name, Description, SortOrder) VALUES ('k1', N'Getting started', N'Accounts, passwords and your first day.', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.KbCategories WHERE Id = 'k2')
    INSERT INTO dbo.KbCategories (Id, Name, Description, SortOrder) VALUES ('k2', N'Email and Teams', N'Outlook, shared mailboxes and calls.', 2);
IF NOT EXISTS (SELECT 1 FROM dbo.KbCategories WHERE Id = 'k3')
    INSERT INTO dbo.KbCategories (Id, Name, Description, SortOrder) VALUES ('k3', N'Hardware', N'Laptops, docks, monitors and headsets.', 3);
IF NOT EXISTS (SELECT 1 FROM dbo.KbCategories WHERE Id = 'k4')
    INSERT INTO dbo.KbCategories (Id, Name, Description, SortOrder) VALUES ('k4', N'Remote working', N'VPN, home set-up and security.', 4);
GO

DECLARE @now DATETIMEOFFSET = SYSDATETIMEOFFSET();

IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka1')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka1', 'k1', N'Reset a forgotten password', N'Published', DATEADD(DAY, -20, @now), 412, N'If you have forgotten your password, email itsupport@topcashback.example from a colleague’s account or call the Service Desk on ext. 4400. We will verify your identity and issue a temporary password.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka2')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka2', 'k1', N'Your first day: signing in', N'Published', DATEADD(DAY, -45, @now), 133, N'Your line manager receives your temporary password. Sign in, then choose a new password that meets the rules shown on screen.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka3')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka3', 'k2', N'Set up an out-of-office reply', N'Published', DATEADD(DAY, -12, @now), 298, N'In Outlook, go to File › Automatic Replies, choose Send automatic replies and set the dates you are away.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka4')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka4', 'k2', N'Open a shared mailbox', N'Published', DATEADD(DAY, -30, @now), 187, N'Once access is granted, the shared mailbox appears in Outlook within an hour. Restart Outlook if it does not.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka5')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka5', 'k3', N'Dock troubleshooting checklist', N'Published', DATEADD(DAY, -8, @now), 95, N'Unplug the dock’s power for 30 seconds, reconnect the USB-C cable firmly and check the monitor input is set to DisplayPort.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka6')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka6', 'k3', N'Report damaged equipment', N'Draft', DATEADD(DAY, -2, @now), 0, N'Raise a Hardware ticket, choose Physical damage and attach a photo.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka7')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka7', 'k4', N'Remote working security checklist', N'Published', DATEADD(DAY, -60, @now), 76, N'Lock your screen when away, use the VPN for file shares and never save company files to personal storage.');
IF NOT EXISTS (SELECT 1 FROM dbo.KbArticles WHERE Id = 'ka8')
    INSERT INTO dbo.KbArticles (Id, CategoryId, Title, Status, UpdatedAt, Views, Body) VALUES
        ('ka8', 'k4', N'Connect to the VPN', N'Published', DATEADD(DAY, -15, @now), 341, N'Open the VPN client from the system tray, choose TopCashback and sign in with your normal account.');
GO
