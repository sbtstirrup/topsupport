/*
    07_Admin_Accounts.sql
    Password storage shared by the admin screens (create user/agent, reset password) and My
    Profile (change own password). The demo keeps plaintext u.password / a.password; here only a
    PBKDF2 hash is stored (see AccountPasswordService). A NULL PasswordHash means the account still
    has its buildSeed() demo password ('TopCashback1!' for users, 'AdminDemo1!' for Administrator
    agents, 'ServiceDesk1!' for other agents), so the seed scripts don't need precomputed hashes.
*/
USE TopSupport;
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(400) NULL;
GO

IF COL_LENGTH(N'dbo.Agents', N'PasswordHash') IS NULL
    ALTER TABLE dbo.Agents ADD PasswordHash NVARCHAR(400) NULL;
GO

-- One row: which table the account lives in, its stored hash (NULL = demo default) and, for
-- agents, their role (which decides the demo default password).
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

-- Sets the hash and, matching ACT['user-reset'] (u.locked = false), unlocks the account.
CREATE OR ALTER PROCEDURE dbo.usp_Account_SetPasswordHash
    @AccountId    NVARCHAR(50),
    @PasswordHash NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users  SET PasswordHash = @PasswordHash, Locked = 0 WHERE Id = @AccountId;
    UPDATE dbo.Agents SET PasswordHash = @PasswordHash, Locked = 0 WHERE Id = @AccountId;
END
GO
