/*
    10_Admin_SecuritySiteProfile.sql
    Security settings (DB.security / PAGES.security), site page text (DB.site / PAGES.site) and
    My Profile (PAGES.profile) in topsupport-app.js.

    Both settings objects live in one key/value table, dbo.AppSettings, with keys prefixed
    "security." or "site." and the demo's property names after the dot (e.g. "security.minLength",
    "site.landingTitle"). Values are stored as text: numbers as digits, booleans as 'true'/'false'
    (JavaScript's String(v), which is also what the audit wording shows). Seeded from buildSeed().

    Re-runnable: the table is created only if missing and seed rows are inserted only if missing,
    so saved settings survive a re-run.
*/
USE TopSupport;
GO

IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppSettings
    (
        [Key]   NVARCHAR(100)  NOT NULL PRIMARY KEY,
        [Value] NVARCHAR(MAX)  NOT NULL
    );
END
GO

-- buildSeed() site + security objects.
INSERT INTO dbo.AppSettings ([Key], [Value])
SELECT s.[Key], s.[Value]
FROM (VALUES
    (N'site.landingTitle',          N'Need help with IT?'),
    (N'site.landingBody',           N'Raise a ticket and the TopCashback IT team will pick it up. You can follow progress, reply to the team and see every update in one place.'),
    (N'site.signinTitle',           N'Sign in to TopSupport'),
    (N'site.signinBody',            N'Use your TopCashback username and password.'),
    (N'security.minLength',         N'10'),
    (N'security.requireUpper',      N'true'),
    (N'security.requireLower',      N'true'),
    (N'security.requireNumber',     N'true'),
    (N'security.requireSymbol',     N'true'),
    (N'security.expiryDays',        N'90'),
    (N'security.history',           N'5'),
    (N'security.sessionTimeout',    N'30'),
    (N'security.lockoutAttempts',   N'5'),
    (N'security.twoFactorAgents',   N'Required'),
    (N'security.twoFactorUsers',    N'Optional')
) AS s ([Key], [Value])
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppSettings a WHERE a.[Key] = s.[Key]);
GO

-- All settings whose key starts with @Prefix (e.g. N'security.').
CREATE OR ALTER PROCEDURE dbo.usp_AppSettings_GetByPrefix
    @Prefix NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Key], [Value]
    FROM dbo.AppSettings
    WHERE LEFT([Key], LEN(@Prefix)) = @Prefix
    ORDER BY [Key];
END
GO

-- Upsert one setting.
CREATE OR ALTER PROCEDURE dbo.usp_AppSetting_Set
    @Key   NVARCHAR(100),
    @Value NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AppSettings SET [Value] = @Value WHERE [Key] = @Key;
    IF @@ROWCOUNT = 0
        INSERT INTO dbo.AppSettings ([Key], [Value]) VALUES (@Key, @Value);
END
GO

-- The signed-in account's contact details for My Profile: a Users row (u1) or an Agents row
-- (a1/a5). Departments is the agent's AgentDepartments in DEPTS order joined with ', ' (the demo's
-- m.departments.join(', ')); Department/Office are the user's.
CREATE OR ALTER PROCEDURE dbo.usp_Profile_Get
    @AccountId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(0 AS BIT) AS IsAgent, Id, Name, Email, Phone, JobTitle, Department, Office,
           CAST(NULL AS NVARCHAR(400)) AS Departments
    FROM dbo.Users WHERE Id = @AccountId
    UNION ALL
    SELECT CAST(1 AS BIT), a.Id, a.Name, a.Email, a.Phone, a.JobTitle, NULL, NULL,
           (SELECT STRING_AGG(d.Department, N', ') WITHIN GROUP (ORDER BY CASE d.Department WHEN N'IT Business Support' THEN 0 ELSE 1 END, d.Department)
            FROM dbo.AgentDepartments d WHERE d.AgentId = a.Id)
    FROM dbo.Agents a WHERE a.Id = @AccountId;
END
GO

-- ACT['pf-save']: name, email and phone on whichever table holds the account.
CREATE OR ALTER PROCEDURE dbo.usp_Profile_Update
    @AccountId NVARCHAR(50),
    @Name      NVARCHAR(200),
    @Email     NVARCHAR(256),
    @Phone     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users  SET Name = @Name, Email = @Email, Phone = @Phone WHERE Id = @AccountId;
    UPDATE dbo.Agents SET Name = @Name, Email = @Email, Phone = @Phone WHERE Id = @AccountId;
END
GO
