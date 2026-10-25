/*
    13_Auth_SignIn.sql
    Real sign-in (ACT.signin in topsupport-app.js) against the Users/Agents rows and their
    PasswordHash from 07_Admin_Accounts.sql. The Api verifies the password and issues a signed
    token; the hand-rolled AspNetUsers tables from 01_Schema_Identity.sql are not used.

    - FailedSignIns on Users and Agents: consecutive failed sign-ins, so the account locks after
      the security setting "lockoutAttempts" (stored since 10_ but never enforced until now).
    - usp_Account_GetByUsername: the account a username signs in as. Like the demo, a
      non-deleted user wins over an agent with the same username.
    - usp_Account_RecordFailedSignIn / usp_Account_ResetFailedSignIns.
*/
USE TopSupport;
GO

IF COL_LENGTH(N'dbo.Users', N'FailedSignIns') IS NULL
    ALTER TABLE dbo.Users ADD FailedSignIns INT NOT NULL CONSTRAINT DF_Users_FailedSignIns DEFAULT 0;
GO

IF COL_LENGTH(N'dbo.Agents', N'FailedSignIns') IS NULL
    ALTER TABLE dbo.Agents ADD FailedSignIns INT NOT NULL CONSTRAINT DF_Agents_FailedSignIns DEFAULT 0;
GO

-- At most one row. Active is always 1 for users; Deleted is always 0 for agents.
CREATE OR ALTER PROCEDURE dbo.usp_Account_GetByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) AccountType, Id, Name, AgentRole, PasswordHash, Locked, Active
    FROM (
        SELECT 1 AS Pref, N'User' AS AccountType, Id, Name, CAST(NULL AS NVARCHAR(20)) AS AgentRole,
               PasswordHash, Locked, CAST(1 AS BIT) AS Active
        FROM dbo.Users WHERE Username = @Username AND Deleted = 0
        UNION ALL
        SELECT 2, N'Agent', Id, Name, Role, PasswordHash, Locked, Active
        FROM dbo.Agents WHERE Username = @Username
    ) a
    ORDER BY Pref;
END
GO

-- Counts a failed sign-in and locks the account once it reaches @LockAfter (0 = never lock).
-- Returns 1 in JustLocked when this call locked it, so the caller can audit the lock once.
CREATE OR ALTER PROCEDURE dbo.usp_Account_RecordFailedSignIn
    @AccountId NVARCHAR(50),
    @LockAfter INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result TABLE (FailedSignIns INT, WasLocked BIT, Locked BIT);

    UPDATE dbo.Users
    SET FailedSignIns = FailedSignIns + 1,
        Locked = IIF(@LockAfter > 0 AND FailedSignIns + 1 >= @LockAfter, 1, Locked)
    OUTPUT inserted.FailedSignIns, deleted.Locked, inserted.Locked INTO @Result
    WHERE Id = @AccountId;

    UPDATE dbo.Agents
    SET FailedSignIns = FailedSignIns + 1,
        Locked = IIF(@LockAfter > 0 AND FailedSignIns + 1 >= @LockAfter, 1, Locked)
    OUTPUT inserted.FailedSignIns, deleted.Locked, inserted.Locked INTO @Result
    WHERE Id = @AccountId;

    SELECT FailedSignIns, CAST(IIF(WasLocked = 0 AND Locked = 1, 1, 0) AS BIT) AS JustLocked FROM @Result;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Account_ResetFailedSignIns
    @AccountId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users  SET FailedSignIns = 0 WHERE Id = @AccountId AND FailedSignIns <> 0;
    UPDATE dbo.Agents SET FailedSignIns = 0 WHERE Id = @AccountId AND FailedSignIns <> 0;
END
GO

-- An admin unlock or password reset also clears the failed-sign-in count, so the account
-- doesn't re-lock on the next mistake.
CREATE OR ALTER PROCEDURE dbo.usp_Account_SetPasswordHash
    @AccountId    NVARCHAR(50),
    @PasswordHash NVARCHAR(400),
    @Unlock       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET PasswordHash = @PasswordHash, Locked = IIF(@Unlock = 1, 0, Locked), FailedSignIns = IIF(@Unlock = 1, 0, FailedSignIns)
    WHERE Id = @AccountId;
    UPDATE dbo.Agents
    SET PasswordHash = @PasswordHash, Locked = IIF(@Unlock = 1, 0, Locked), FailedSignIns = IIF(@Unlock = 1, 0, FailedSignIns)
    WHERE Id = @AccountId;
END
GO

-- Redefined from 08_Admin_UsersOrgs.sql: unlocking also clears the failed-sign-in count.
CREATE OR ALTER PROCEDURE dbo.usp_User_SetLocked
    @Id     NVARCHAR(50),
    @Locked BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET Locked = @Locked, FailedSignIns = IIF(@Locked = 0, 0, FailedSignIns)
    WHERE Id = @Id AND Deleted = 0;
END
GO

-- Checked on every Api request with a bearer token, so a token stops working as soon as its
-- account is locked, deleted, deactivated or given a different role, not only when it expires.
CREATE OR ALTER PROCEDURE dbo.usp_Account_GetStatus
    @AccountId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'User' AS AccountType, CAST(NULL AS NVARCHAR(20)) AS AgentRole, Locked, CAST(IIF(Deleted = 0, 1, 0) AS BIT) AS Active
    FROM dbo.Users WHERE Id = @AccountId
    UNION ALL
    SELECT N'Agent', Role, Locked, Active
    FROM dbo.Agents WHERE Id = @AccountId;
END
GO
