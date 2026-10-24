/*
    02_Schema_Core.sql
    Core domain schema, reproducing the shape of DB in topsupport-app.js's buildSeed().
*/
USE TopSupport;
GO

IF OBJECT_ID(N'dbo.Orgs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orgs
    (
        Id      NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Name    NVARCHAR(200)   NOT NULL,
        Address NVARCHAR(400)   NOT NULL,
        Phone   NVARCHAR(50)    NOT NULL,
        Note    NVARCHAR(400)   NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id          NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Name        NVARCHAR(200)   NOT NULL,
        Username    NVARCHAR(100)   NOT NULL,
        Email       NVARCHAR(256)   NOT NULL,
        Phone       NVARCHAR(50)    NULL,
        OrgId       NVARCHAR(50)    NOT NULL,
        Office      NVARCHAR(200)   NULL,
        Department  NVARCHAR(100)   NOT NULL,
        JobTitle    NVARCHAR(150)   NULL,
        Locked      BIT             NOT NULL DEFAULT 0,
        Created     DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT FK_Users_Orgs FOREIGN KEY (OrgId) REFERENCES dbo.Orgs(Id)
    );
    CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
END
GO

IF OBJECT_ID(N'dbo.Teams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Teams
    (
        Id          NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Name        NVARCHAR(200)   NOT NULL,
        LeadAgentId NVARCHAR(50)    NULL,
        Department  NVARCHAR(100)   NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Agents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Agents
    (
        Id          NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Name        NVARCHAR(200)   NOT NULL,
        Username    NVARCHAR(100)   NOT NULL,
        Email       NVARCHAR(256)   NOT NULL,
        Phone       NVARCHAR(50)    NULL,
        Role        NVARCHAR(20)    NOT NULL,   -- 'Agent' | 'Supervisor' | 'Administrator'
        JobTitle    NVARCHAR(150)   NULL,
        Active      BIT             NOT NULL DEFAULT 1,
        Locked      BIT             NOT NULL DEFAULT 0
    );
    CREATE UNIQUE INDEX IX_Agents_Username ON dbo.Agents(Username);

    ALTER TABLE dbo.Teams ADD CONSTRAINT FK_Teams_LeadAgent FOREIGN KEY (LeadAgentId) REFERENCES dbo.Agents(Id);
END
GO

IF OBJECT_ID(N'dbo.AgentDepartments', N'U') IS NULL
BEGIN
    -- One row per (agent, department) the agent is permitted to work in.
    CREATE TABLE dbo.AgentDepartments
    (
        AgentId    NVARCHAR(50)  NOT NULL,
        Department NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_AgentDepartments PRIMARY KEY (AgentId, Department),
        CONSTRAINT FK_AgentDepartments_Agents FOREIGN KEY (AgentId) REFERENCES dbo.Agents(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.TeamMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeamMembers
    (
        TeamId  NVARCHAR(50) NOT NULL,
        AgentId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_TeamMembers PRIMARY KEY (TeamId, AgentId),
        CONSTRAINT FK_TeamMembers_Teams FOREIGN KEY (TeamId) REFERENCES dbo.Teams(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TeamMembers_Agents FOREIGN KEY (AgentId) REFERENCES dbo.Agents(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Assets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Assets
    (
        Tag     NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Type    NVARCHAR(50)    NOT NULL,
        Model   NVARCHAR(200)   NOT NULL,
        UserId  NVARCHAR(50)    NULL,
        CONSTRAINT FK_Assets_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Tickets', N'U') IS NULL
BEGIN
    -- FieldsJson holds the per-topic dynamic form answers as a JSON object, matching the
    -- "fields" blob on each ticket in topsupport-app.js (topicFields/renderTopicField render
    -- a different field set per TOPICS entry; a fixed-column schema would need a migration
    -- every time a topic's fields change).
    CREATE TABLE dbo.Tickets
    (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        Number          NVARCHAR(10)    NOT NULL,
        Subject         NVARCHAR(300)   NOT NULL,
        Topic           NVARCHAR(50)    NOT NULL,
        Department      NVARCHAR(100)   NOT NULL,
        Status          NVARCHAR(20)    NOT NULL,
        Priority        NVARCHAR(20)    NOT NULL,
        RequesterUserId NVARCHAR(50)    NOT NULL,
        AffectedUserId  NVARCHAR(50)    NOT NULL,
        CreatedByType   NVARCHAR(10)    NOT NULL,  -- 'user' | 'agent' | 'system'
        CreatedById     NVARCHAR(50)    NULL,
        Created         DATETIMEOFFSET  NOT NULL,
        Updated         DATETIMEOFFSET  NOT NULL,
        AssignedAgentId NVARCHAR(50)    NULL,
        AssignedTeamId  NVARCHAR(50)    NULL,
        FieldsJson      NVARCHAR(MAX)   NOT NULL DEFAULT N'{}',
        CONSTRAINT FK_Tickets_Requester FOREIGN KEY (RequesterUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Tickets_Affected FOREIGN KEY (AffectedUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Tickets_Agent FOREIGN KEY (AssignedAgentId) REFERENCES dbo.Agents(Id),
        CONSTRAINT FK_Tickets_Team FOREIGN KEY (AssignedTeamId) REFERENCES dbo.Teams(Id),
        CONSTRAINT CK_Tickets_FieldsJson CHECK (ISJSON(FieldsJson) = 1)
    );
    CREATE UNIQUE INDEX IX_Tickets_Number ON dbo.Tickets(Number);
    CREATE INDEX IX_Tickets_Requester ON dbo.Tickets(RequesterUserId);
    CREATE INDEX IX_Tickets_Status ON dbo.Tickets(Status);
END
GO

IF OBJECT_ID(N'dbo.TicketTimelineEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketTimelineEntries
    (
        Id          NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TicketId    NVARCHAR(50)    NOT NULL,
        Kind        NVARCHAR(10)    NOT NULL,  -- 'event' | 'note' | 'reply'
        At          DATETIMEOFFSET  NOT NULL,
        ByType      NVARCHAR(10)    NOT NULL,  -- 'user' | 'agent' | 'system'
        ById        NVARCHAR(50)    NULL,
        Text        NVARCHAR(MAX)   NULL,      -- plain text, used when Kind = 'event'
        Html        NVARCHAR(MAX)   NULL,      -- sanitized rich text, used when Kind in ('note','reply')
        CONSTRAINT FK_TicketTimelineEntries_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TicketTimelineEntries_TicketId ON dbo.TicketTimelineEntries(TicketId, At);
END
GO

IF OBJECT_ID(N'dbo.TicketTasks', N'U') IS NULL
BEGIN
    -- The leaver-ticket checklist (LEAVER_TASKS in topsupport-app.js) and any future
    -- per-ticket task list.
    CREATE TABLE dbo.TicketTasks
    (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TicketId        NVARCHAR(50)    NOT NULL,
        Text            NVARCHAR(300)   NOT NULL,
        AssigneeAgentId NVARCHAR(50)    NOT NULL,
        Due             DATE            NULL,
        Done            BIT             NOT NULL DEFAULT 0,
        CONSTRAINT FK_TicketTasks_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TicketTasks_Agents FOREIGN KEY (AssigneeAgentId) REFERENCES dbo.Agents(Id)
    );
    CREATE INDEX IX_TicketTasks_TicketId ON dbo.TicketTasks(TicketId);
END
GO

IF OBJECT_ID(N'dbo.TicketViewers', N'U') IS NULL
BEGIN
    -- Agents watching a ticket without being the assignee (demo's ticket.viewers).
    CREATE TABLE dbo.TicketViewers
    (
        TicketId NVARCHAR(50) NOT NULL,
        AgentId  NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_TicketViewers PRIMARY KEY (TicketId, AgentId),
        CONSTRAINT FK_TicketViewers_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TicketViewers_Agents FOREIGN KEY (AgentId) REFERENCES dbo.Agents(Id)
    );
END
GO
