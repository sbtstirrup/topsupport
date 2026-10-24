/*
    09_Admin_AgentsRoles.sql
    Admin "Agents and Teams" and "Roles and Permissions" screens (PAGES.agents / PAGES.roles in
    topsupport-app.js): agent and team create/edit/delete procs, active-ticket counts for the
    agents table and team cards, and the per-department role permission grid (DB.perms), seeded
    exactly from buildSeed()'s perms object. Safe to re-run.
*/
USE TopSupport;
GO

-- Creation order, so the admin agents table lists agents the way the demo's DB.agents array
-- does (seed agents a1..a5 first, then new ones in the order they were created).
IF COL_LENGTH(N'dbo.Agents', N'Created') IS NULL
    ALTER TABLE dbo.Agents ADD Created DATETIMEOFFSET NOT NULL
        CONSTRAINT DF_Agents_Created DEFAULT SYSDATETIMEOFFSET();
GO

IF COL_LENGTH(N'dbo.Teams', N'Created') IS NULL
    ALTER TABLE dbo.Teams ADD Created DATETIMEOFFSET NOT NULL
        CONSTRAINT DF_Teams_Created DEFAULT SYSDATETIMEOFFSET();
GO

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    -- DB.perms[department][role][permission] flattened to one row per cell.
    CREATE TABLE dbo.RolePermissions
    (
        Department NVARCHAR(100) NOT NULL,
        Role       NVARCHAR(20)  NOT NULL,   -- 'Agent' | 'Supervisor' | 'Administrator'
        Permission NVARCHAR(20)  NOT NULL,   -- 'assign' | 'transfer' | 'close' | 'delete' | 'edit'
        Allowed    BIT           NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY (Department, Role, Permission),
        CONSTRAINT CK_RolePermissions_Role CHECK (Role IN (N'Agent', N'Supervisor', N'Administrator')),
        CONSTRAINT CK_RolePermissions_Permission CHECK (Permission IN (N'assign', N'transfer', N'close', N'delete', N'edit'))
    );
END
GO

/* ===================== Agents ===================== */

-- Every agent, active or not (the admin table shows inactive agents too, unlike
-- usp_Agent_GetAllActive). Same 3-result-set shape as usp_Agent_GetAllActive.
CREATE OR ALTER PROCEDURE dbo.usp_AdminAgent_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Username, Email, Phone, Role, JobTitle, Active, Locked
    FROM dbo.Agents
    ORDER BY Created ASC, Id ASC;

    SELECT AgentId, Department
    FROM dbo.AgentDepartments;

    SELECT TeamId, AgentId
    FROM dbo.TeamMembers;
END
GO

-- validateUser(F, true): email and username must be unique across users and agents.
CREATE OR ALTER PROCEDURE dbo.usp_AdminAgent_FindConflicts
    @Id       NVARCHAR(50),
    @Email    NVARCHAR(256),
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Id <> @Id AND LOWER(Email) = LOWER(@Email))
                    OR EXISTS (SELECT 1 FROM dbo.Agents WHERE Id <> @Id AND LOWER(Email) = LOWER(@Email))
                  THEN 1 ELSE 0 END AS BIT) AS EmailTaken,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Id <> @Id AND Username = @Username)
                    OR EXISTS (SELECT 1 FROM dbo.Agents WHERE Id <> @Id AND Username = @Username)
                  THEN 1 ELSE 0 END AS BIT) AS UsernameTaken;
END
GO

-- Insert or update one agent, replacing its departments and team memberships (ACT['agent-save']
-- keeps team.members in step with agent.teams). @DepartmentsJson / @TeamIdsJson are JSON string
-- arrays, e.g. '["IT Business Support","Infrastructure"]'.
CREATE OR ALTER PROCEDURE dbo.usp_AdminAgent_Save
    @Id              NVARCHAR(50),
    @Name            NVARCHAR(200),
    @Username        NVARCHAR(100),
    @Email           NVARCHAR(256),
    @Phone           NVARCHAR(50),
    @Role            NVARCHAR(20),
    @JobTitle        NVARCHAR(150),
    @Active          BIT,
    @DepartmentsJson NVARCHAR(MAX),
    @TeamIdsJson     NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Agents WHERE Id = @Id)
        UPDATE dbo.Agents
        SET Name = @Name, Username = @Username, Email = @Email, Phone = @Phone,
            Role = @Role, JobTitle = @JobTitle, Active = @Active
        WHERE Id = @Id;
    ELSE
        INSERT INTO dbo.Agents (Id, Name, Username, Email, Phone, Role, JobTitle, Active, Locked)
        VALUES (@Id, @Name, @Username, @Email, @Phone, @Role, @JobTitle, @Active, 0);

    DELETE FROM dbo.AgentDepartments WHERE AgentId = @Id;
    INSERT INTO dbo.AgentDepartments (AgentId, Department)
    SELECT DISTINCT @Id, [value] FROM OPENJSON(@DepartmentsJson);

    DELETE FROM dbo.TeamMembers WHERE AgentId = @Id;
    INSERT INTO dbo.TeamMembers (TeamId, AgentId)
    SELECT DISTINCT j.[value], @Id
    FROM OPENJSON(@TeamIdsJson) j
    INNER JOIN dbo.Teams t ON t.Id = j.[value];

    COMMIT TRANSACTION;
END
GO

-- Active (Open / In Progress / Awaiting User) tickets per assigned agent and per assigned team,
-- for the "Active tickets" column and the "N active tickets assigned to this team" card line.
CREATE OR ALTER PROCEDURE dbo.usp_AdminAgent_GetActiveTicketCounts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT AssignedAgentId AS Id, COUNT(*) AS ActiveTickets
    FROM dbo.Tickets
    WHERE AssignedAgentId IS NOT NULL AND Status IN (N'Open', N'InProgress', N'AwaitingUser')
    GROUP BY AssignedAgentId;

    SELECT AssignedTeamId AS Id, COUNT(*) AS ActiveTickets
    FROM dbo.Tickets
    WHERE AssignedTeamId IS NOT NULL AND Status IN (N'Open', N'InProgress', N'AwaitingUser')
    GROUP BY AssignedTeamId;
END
GO

/* ===================== Teams ===================== */

-- Same 2-result-set shape as usp_Team_GetAll, but in creation order (DB.teams array order).
CREATE OR ALTER PROCEDURE dbo.usp_AdminTeam_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, LeadAgentId, Department
    FROM dbo.Teams
    ORDER BY Created ASC, Id ASC;

    SELECT TeamId, AgentId
    FROM dbo.TeamMembers;
END
GO

-- Insert or update one team, replacing its members (ACT['team-save'] keeps agent.teams in step
-- with team.members). @MemberIdsJson is a JSON string array of Agents.Id values.
CREATE OR ALTER PROCEDURE dbo.usp_AdminTeam_Save
    @Id            NVARCHAR(50),
    @Name          NVARCHAR(200),
    @Department    NVARCHAR(100),
    @LeadAgentId   NVARCHAR(50),
    @MemberIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Teams WHERE Id = @Id)
        UPDATE dbo.Teams
        SET Name = @Name, Department = @Department, LeadAgentId = NULLIF(@LeadAgentId, N'')
        WHERE Id = @Id;
    ELSE
        INSERT INTO dbo.Teams (Id, Name, LeadAgentId, Department)
        VALUES (@Id, @Name, NULLIF(@LeadAgentId, N''), @Department);

    DELETE FROM dbo.TeamMembers WHERE TeamId = @Id;
    INSERT INTO dbo.TeamMembers (TeamId, AgentId)
    SELECT DISTINCT @Id, j.[value]
    FROM OPENJSON(@MemberIdsJson) j
    INNER JOIN dbo.Agents a ON a.Id = j.[value];

    COMMIT TRANSACTION;
END
GO

-- Every ticket (any status) still pointing at the team; ACT['team-del'] refuses to delete while
-- this is non-zero.
CREATE OR ALTER PROCEDURE dbo.usp_AdminTeam_CountTickets
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TicketCount FROM dbo.Tickets WHERE AssignedTeamId = @Id;
END
GO

-- Members stay as agents but leave the team (TeamMembers rows cascade).
CREATE OR ALTER PROCEDURE dbo.usp_AdminTeam_Delete
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Teams WHERE Id = @Id;
END
GO

/* ===================== Role permissions ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_RolePermission_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Department, Role, Permission, Allowed
    FROM dbo.RolePermissions;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RolePermission_Set
    @Department NVARCHAR(100),
    @Role       NVARCHAR(20),
    @Permission NVARCHAR(20),
    @Allowed    BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RolePermissions
    SET Allowed = @Allowed
    WHERE Department = @Department AND Role = @Role AND Permission = @Permission;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.RolePermissions (Department, Role, Permission, Allowed)
        VALUES (@Department, @Role, @Permission, @Allowed);
END
GO

-- buildSeed() perms: identical for both departments except Agent "transfer", which is only
-- allowed in IT Business Support.
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions)
BEGIN
    INSERT INTO dbo.RolePermissions (Department, Role, Permission, Allowed) VALUES
        (N'IT Business Support', N'Agent',         N'assign',   1),
        (N'IT Business Support', N'Agent',         N'transfer', 1),
        (N'IT Business Support', N'Agent',         N'close',    1),
        (N'IT Business Support', N'Agent',         N'delete',   0),
        (N'IT Business Support', N'Agent',         N'edit',     1),
        (N'IT Business Support', N'Supervisor',    N'assign',   1),
        (N'IT Business Support', N'Supervisor',    N'transfer', 1),
        (N'IT Business Support', N'Supervisor',    N'close',    1),
        (N'IT Business Support', N'Supervisor',    N'delete',   0),
        (N'IT Business Support', N'Supervisor',    N'edit',     1),
        (N'IT Business Support', N'Administrator', N'assign',   1),
        (N'IT Business Support', N'Administrator', N'transfer', 1),
        (N'IT Business Support', N'Administrator', N'close',    1),
        (N'IT Business Support', N'Administrator', N'delete',   1),
        (N'IT Business Support', N'Administrator', N'edit',     1),
        (N'Infrastructure',      N'Agent',         N'assign',   1),
        (N'Infrastructure',      N'Agent',         N'transfer', 0),
        (N'Infrastructure',      N'Agent',         N'close',    1),
        (N'Infrastructure',      N'Agent',         N'delete',   0),
        (N'Infrastructure',      N'Agent',         N'edit',     1),
        (N'Infrastructure',      N'Supervisor',    N'assign',   1),
        (N'Infrastructure',      N'Supervisor',    N'transfer', 1),
        (N'Infrastructure',      N'Supervisor',    N'close',    1),
        (N'Infrastructure',      N'Supervisor',    N'delete',   0),
        (N'Infrastructure',      N'Supervisor',    N'edit',     1),
        (N'Infrastructure',      N'Administrator', N'assign',   1),
        (N'Infrastructure',      N'Administrator', N'transfer', 1),
        (N'Infrastructure',      N'Administrator', N'close',    1),
        (N'Infrastructure',      N'Administrator', N'delete',   1),
        (N'Infrastructure',      N'Administrator', N'edit',     1);
END
GO
