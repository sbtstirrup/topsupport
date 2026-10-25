/*
    07_Admin_Accounts.sql
    Password storage shared by the admin screens (create user/agent, reset password) and My
    Profile (change own password). The demo keeps plaintext u.password / a.password; here only a
    PBKDF2 hash is stored (see AccountPasswordService). A NULL PasswordHash means no password
    has been set, so the account can't sign in; 99_SeedData.sql sets hashes of the development
    passwords for the seeded accounts.
*/
USE TopSupport;
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(400) NULL;
GO

IF COL_LENGTH(N'dbo.Agents', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Agents ADD PasswordHash NVARCHAR(400) NULL;
GO

-- One row: which table the account lives in, its stored hash (NULL = no password set) and, for
-- agents, their role.
CREATE OR ALTER PROCEDURE dbo.usp_Account_GetPasswordHash
    @AccountId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'User' AS AccountType, PasswordHash, CAST(NULL AS NVARCHAR(20)) AS AgentRole
    FROM dbo.Users WHERE Id = @AccountId
    UNION ALL
    SELECT N'Agent', PasswordHash, Role
    FROM dbo.Agents WHERE Id = @AccountId;
END
GO

-- Sets the hash. @Unlock = 1 for an admin reset, matching ACT['user-reset'] (u.locked = false);
-- a user changing their own password passes 0 so they can't lift an admin's lock.
CREATE OR ALTER PROCEDURE dbo.usp_Account_SetPasswordHash
    @AccountId    NVARCHAR(50),
    @PasswordHash NVARCHAR(400),
    @Unlock       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users  SET PasswordHash = @PasswordHash, Locked = IIF(@Unlock = 1, 0, Locked) WHERE Id = @AccountId;
    UPDATE dbo.Agents SET PasswordHash = @PasswordHash, Locked = IIF(@Unlock = 1, 0, Locked) WHERE Id = @AccountId;
END
GO
