/*
    16_Dev_TestAccounts.sql
    The test-account switcher (development only): every account you can switch to, for the
    switcher's list. The Api only exposes it when running in Development with
    Auth:TestAccountSwitcher:Enabled = true (see TopSupport.Api/Controllers/AuthController.cs);
    it reads, never writes, and shows nothing secret (no password hashes).
*/
USE TopSupport;
GO

-- End users (not deleted) and agents, with what the switcher shows: role, department(s), and
-- whether the account can sign in right now (locked or inactive accounts are listed, disabled).
CREATE OR ALTER PROCEDURE dbo.usp_Account_ListForTesting
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'User' AS AccountType, u.Id, u.Name, u.Username, u.Email,
           CAST(NULL AS NVARCHAR(20)) AS AgentRole, u.Department AS Detail,
           u.Locked, CAST(1 AS BIT) AS Active
    FROM dbo.Users u
    WHERE u.Deleted = 0

    UNION ALL
    SELECT N'Agent', a.Id, a.Name, a.Username, a.Email, a.Role,
           (SELECT STRING_AGG(d.Department, N', ') FROM dbo.AgentDepartments d WHERE d.AgentId = a.Id),
           a.Locked, a.Active
    FROM dbo.Agents a

    ORDER BY AccountType, Name;
END
GO
