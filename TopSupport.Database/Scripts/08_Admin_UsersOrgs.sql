/*
    08_Admin_UsersOrgs.sql
    Admin Users / User detail / Organisations / Organisation detail screens (PAGES.users,
    PAGES.user, PAGES.orgs, PAGES.org in topsupport-app.js).

    - Users.Deleted: the demo's soft delete (u.deleted). Deleted users drop out of the user list
      and pickers (activeUsers() in the demo) but their row stays so historical tickets still
      show their name. A deleted user keeps their username and IX_Users_Username stays unique
      across every row, so usp_Account_CheckIdentityTaken still treats a deleted user's username
      as taken (the demo only checks non-deleted users; reuse would violate the index here).
    - usp_User_GetAllActive is redefined to match activeUsers() (Deleted = 0, locked users
      included) instead of "Locked = 0"; usp_User_GetAll includes deleted users for name lookups.
    - Admin list/detail/mutation procs for users and organisations.
*/
USE TopSupport;
GO

IF COL_LENGTH(N'dbo.Users', N'Deleted') IS NULL
    ALTER TABLE dbo.Users ADD Deleted BIT NOT NULL CONSTRAINT DF_Users_Deleted DEFAULT 0;
GO

-- Unchanged from 02_Schema_Core.sql; re-created here only if missing (an early draft of this
-- script dropped it). It deliberately stays unfiltered: a filtered index would make every
-- sqlcmd-created proc that writes dbo.Users (QUOTED_IDENTIFIER OFF) fail.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_Username')
    CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
GO

/* ===================== Lookup: Users (redefined) ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Deleted, Created
    FROM dbo.Users
    WHERE Id = @Id;
END
GO

-- activeUsers() in the demo: everyone not deleted (locked users still appear in pickers).
CREATE OR ALTER PROCEDURE dbo.usp_User_GetAllActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Deleted, Created
    FROM dbo.Users
    WHERE Deleted = 0
    ORDER BY Name ASC;
END
GO

-- Every user including deleted ones, so ticket screens can still name a deleted requester.
CREATE OR ALTER PROCEDURE dbo.usp_User_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Deleted, Created
    FROM dbo.Users
    ORDER BY Name ASC;
END
GO

/* ===================== Admin: Users ===================== */

-- PAGES.users rows: non-deleted users with their active-ticket count (requester OR affected,
-- matching userTickets(u.id).filter(isActive)).
CREATE OR ALTER PROCEDURE dbo.usp_AdminUser_GetList
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.Id, u.Name, u.Username, u.Email, u.Phone, u.OrgId, u.Office, u.Department, u.JobTitle,
           u.Locked, u.Deleted, u.Created,
           (SELECT COUNT(*) FROM dbo.Tickets t
            WHERE (t.RequesterUserId = u.Id OR t.AffectedUserId = u.Id)
              AND t.Status IN ('Open', 'InProgress', 'AwaitingUser')) AS OpenTicketCount
    FROM dbo.Users u
    WHERE u.Deleted = 0
    ORDER BY u.Name ASC;
END
GO

-- validateUser(): which of the email/username are already used by someone else. The email pool
-- is non-deleted users plus every agent, exactly as in the demo; usernames are also checked
-- against deleted users because IX_Users_Username is unique across every row.
CREATE OR ALTER PROCEDURE dbo.usp_Account_CheckIdentityTaken
    @ExcludeId NVARCHAR(50),
    @Email     NVARCHAR(256),
    @Username  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Pool AS
    (
        SELECT Id, Email, Username FROM dbo.Users WHERE Deleted = 0
        UNION ALL
        SELECT Id, Email, Username FROM dbo.Agents
    )
    SELECT
        CAST(CASE WHEN EXISTS (SELECT 1 FROM Pool WHERE Id <> @ExcludeId AND LOWER(Email) = LOWER(@Email)) THEN 1 ELSE 0 END AS BIT) AS EmailTaken,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM Pool WHERE Id <> @ExcludeId AND Username = @Username)
                    OR EXISTS (SELECT 1 FROM dbo.Users WHERE Id <> @ExcludeId AND Deleted = 1 AND Username = @Username)
                  THEN 1 ELSE 0 END AS BIT) AS UsernameTaken;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Insert
    @Id         NVARCHAR(50),
    @Name       NVARCHAR(200),
    @Username   NVARCHAR(100),
    @Email      NVARCHAR(256),
    @Phone      NVARCHAR(50),
    @OrgId      NVARCHAR(50),
    @Office     NVARCHAR(200),
    @Department NVARCHAR(100),
    @JobTitle   NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked, Deleted)
    VALUES (@Id, @Name, @Username, @Email, @Phone, @OrgId, @Office, @Department, @JobTitle, 0, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Update
    @Id         NVARCHAR(50),
    @Name       NVARCHAR(200),
    @Username   NVARCHAR(100),
    @Email      NVARCHAR(256),
    @Phone      NVARCHAR(50),
    @OrgId      NVARCHAR(50),
    @Office     NVARCHAR(200),
    @Department NVARCHAR(100),
    @JobTitle   NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET Name = @Name, Username = @Username, Email = @Email, Phone = @Phone, OrgId = @OrgId,
        Office = @Office, Department = @Department, JobTitle = @JobTitle
    WHERE Id = @Id AND Deleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_SetLocked
    @Id     NVARCHAR(50),
    @Locked BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users SET Locked = @Locked WHERE Id = @Id AND Deleted = 0;
END
GO

-- ACT['user-del']: u.deleted = true; u.locked = true; their assets become unassigned.
CREATE OR ALTER PROCEDURE dbo.usp_User_SoftDelete
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        UPDATE dbo.Assets SET UserId = NULL WHERE UserId = @Id;
        UPDATE dbo.Users SET Deleted = 1, Locked = 1 WHERE Id = @Id;
    COMMIT TRANSACTION;
END
GO

/* ===================== Admin: Organisations ===================== */

-- PAGES.orgs cards: member count (non-deleted users) and active tickets raised by a member
-- (requester only, as in the demo's org card).
CREATE OR ALTER PROCEDURE dbo.usp_Org_GetList
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.Name, o.Address, o.Phone, o.Note,
           (SELECT COUNT(*) FROM dbo.Users u WHERE u.OrgId = o.Id AND u.Deleted = 0) AS MemberCount,
           (SELECT COUNT(*) FROM dbo.Tickets t
            JOIN dbo.Users u ON u.Id = t.RequesterUserId
            WHERE u.OrgId = o.Id AND u.Deleted = 0
              AND t.Status IN ('Open', 'InProgress', 'AwaitingUser')) AS OpenTicketCount
    FROM dbo.Orgs o
    ORDER BY o.Name ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Org_GetById
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Address, Phone, Note FROM dbo.Orgs WHERE Id = @Id;
END
GO

-- PAGES.org "Open Tickets": active tickets where a non-deleted member is requester or affected.
CREATE OR ALTER PROCEDURE dbo.usp_Org_GetOpenTicketIds
    @OrgId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id
    FROM dbo.Tickets t
    WHERE t.Status IN ('Open', 'InProgress', 'AwaitingUser')
      AND EXISTS (SELECT 1 FROM dbo.Users u
                  WHERE u.OrgId = @OrgId AND u.Deleted = 0
                    AND (u.Id = t.RequesterUserId OR u.Id = t.AffectedUserId))
    ORDER BY t.Created DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Org_Insert
    @Id      NVARCHAR(50),
    @Name    NVARCHAR(200),
    @Address NVARCHAR(400),
    @Phone   NVARCHAR(50),
    @Note    NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Orgs (Id, Name, Address, Phone, Note)
    VALUES (@Id, @Name, @Address, @Phone, @Note);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Org_Update
    @Id      NVARCHAR(50),
    @Name    NVARCHAR(200),
    @Address NVARCHAR(400),
    @Phone   NVARCHAR(50),
    @Note    NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Orgs
    SET Name = @Name, Address = @Address, Phone = @Phone, Note = @Note
    WHERE Id = @Id;
END
GO
