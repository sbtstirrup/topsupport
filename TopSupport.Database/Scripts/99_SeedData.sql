/*
    99_SeedData.sql
    Seed data ported field-for-field from buildSeed() in topsupport-app.js: all 3 orgs,
    the full 12 demo users, 5 agents, 2 teams, all 15 assets, 5 canned responses, and the
    full 29-ticket specs array with timelines and (for the 2 leaver tickets) task
    checklists. Relative timestamps (ago/day offsets) are computed from
    SYSDATETIMEOFFSET() at seed-run time rather than hardcoded, matching the JS demo's
    "hours ago" pattern. Idempotent via IF NOT EXISTS guards, consistent with the rest of
    this file.
*/
USE TopSupport;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orgs WHERE Id = 'o1')
BEGIN
    INSERT INTO dbo.Orgs (Id, Name, Address, Phone, Note) VALUES
        ('o1', N'Horwich', N'Unit 4, Middlebrook Business Park, Horwich', N'01632 960100', N'Head office, finance, HR and warehouse.'),
        ('o2', N'Manchester', N'3rd floor, 21 Portland Street, Manchester', N'01632 960200', N'Sales, marketing and customer services hub.'),
        ('o3', N'Remote', N'Home-based staff across the North West', N'—', N'Equipment is couriered to and from home addresses.');
END
GO

-- Extend to the full 12 demo users from buildSeed() (u1-u3,u5,u8,u11 already existed from the skeleton pass).
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u1')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u1', N'Amira Patel', N'apatel', N'amira.patel@topcashback.example', N'01632 960112', 'o1', N'Horwich · Unit 4, desk F-12', N'Finance', N'Accounts Assistant', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u2')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u2', N'Tom Hargreaves', N'thargreaves', N'tom.hargreaves@topcashback.example', N'01632 960131', 'o1', N'Horwich · Warehouse office', N'Operations', N'Warehouse Supervisor', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u3')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u3', N'Leah Brennan', N'lbrennan', N'leah.brennan@topcashback.example', N'01632 960145', 'o1', N'Horwich · Unit 4, desk H-03', N'HR', N'HR Advisor', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u4')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u4', N'Daniel Okafor', N'dokafor', N'daniel.okafor@topcashback.example', N'01632 960117', 'o1', N'Horwich · Unit 4, desk F-07', N'Finance', N'Management Accountant', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u5')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u5', N'Sophie Lumb', N'slumb', N'sophie.lumb@topcashback.example', N'01632 960221', 'o2', N'Manchester · 3rd floor, desk S-21', N'Sales', N'Sales Manager', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u6')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u6', N'Marcus Webb', N'mwebb', N'marcus.webb@topcashback.example', N'01632 960209', 'o2', N'Manchester · 3rd floor, desk S-09', N'Sales', N'Account Manager', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u7')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u7', N'Priya Nair', N'pnair', N'priya.nair@topcashback.example', N'01632 960234', 'o2', N'Manchester · 3rd floor, desk M-04', N'Marketing', N'Marketing Executive', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u8')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u8', N'Grace Whitfield', N'gwhitfield', N'grace.whitfield@topcashback.example', N'01632 960250', 'o2', N'Manchester · 2nd floor, CS team', N'Customer Services', N'Team Leader', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u9')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u9', N'Ethan Clarke', N'eclarke', N'ethan.clarke@topcashback.example', N'07700 900311', 'o3', N'Remote · home (Preston)', N'Sales', N'Field Sales Representative', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u10')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u10', N'Hannah Doyle', N'hdoyle', N'hannah.doyle@topcashback.example', N'07700 900327', 'o3', N'Remote · home (Wigan)', N'Customer Services', N'Support Advisor', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u11')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u11', N'Owen Pryce', N'opryce', N'owen.pryce@topcashback.example', N'07700 900342', 'o3', N'Remote · home (Chester)', N'Product', N'Product Owner', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 'u12')
BEGIN
    INSERT INTO dbo.Users (Id, Name, Username, Email, Phone, OrgId, Office, Department, JobTitle, Locked) VALUES
        ('u12', N'Ruth Adeyemi', N'radeyemi', N'ruth.adeyemi@topcashback.example', N'07700 900358', 'o3', N'Remote · home (Bolton)', N'Legal', N'Contracts Manager', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Agents WHERE Id = 'a1')
BEGIN
    INSERT INTO dbo.Agents (Id, Name, Username, Email, Phone, Role, JobTitle, Active, Locked) VALUES
        ('a1', N'Jamie Moore', N'jmoore', N'jamie.moore@topcashback.example', N'ext. 4401', N'Agent', N'Service Desk Analyst', 1, 0),
        ('a2', N'Ravi Singh', N'rsingh', N'ravi.singh@topcashback.example', N'ext. 4402', N'Agent', N'Service Desk Analyst', 1, 0),
        ('a3', N'Ellie Shaw', N'eshaw', N'ellie.shaw@topcashback.example', N'ext. 4403', N'Supervisor', N'Service Desk Lead', 1, 0),
        ('a4', N'Callum Reid', N'creid', N'callum.reid@topcashback.example', N'ext. 4410', N'Agent', N'Infrastructure Engineer', 1, 0),
        ('a5', N'Nadia Hussain', N'nhussain', N'nadia.hussain@topcashback.example', N'ext. 4400', N'Administrator', N'IT Service Manager', 1, 0);

    INSERT INTO dbo.AgentDepartments (AgentId, Department) VALUES
        ('a1', N'IT Business Support'),
        ('a2', N'IT Business Support'),
        ('a3', N'IT Business Support'), ('a3', N'Infrastructure'),
        ('a4', N'Infrastructure'),
        ('a5', N'IT Business Support'), ('a5', N'Infrastructure');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Teams WHERE Id = 't1')
BEGIN
    INSERT INTO dbo.Teams (Id, Name, LeadAgentId, Department) VALUES
        ('t1', N'Service Desk', 'a3', N'IT Business Support'),
        ('t2', N'Infrastructure', 'a5', N'Infrastructure');

    INSERT INTO dbo.TeamMembers (TeamId, AgentId) VALUES
        ('t1', 'a1'), ('t1', 'a2'), ('t1', 'a3'),
        ('t2', 'a4'), ('t2', 'a5');
END
GO

-- All 15 Assets rows from buildSeed().
IF NOT EXISTS (SELECT 1 FROM dbo.Assets WHERE Tag = 'TCB-LT-0142')
BEGIN
    INSERT INTO dbo.Assets (Tag, Type, Model, UserId) VALUES
        ('TCB-LT-0142', N'laptop', N'Dell Latitude 5440', 'u1'),
        ('TCB-LT-0156', N'laptop', N'Dell Latitude 5440', 'u9'),
        ('TCB-LT-0163', N'laptop', N'Lenovo ThinkPad T14 Gen 4', 'u10'),
        ('TCB-LT-0171', N'laptop', N'Lenovo ThinkPad T14 Gen 4', 'u11'),
        ('TCB-LT-0178', N'laptop', N'Dell Latitude 7440', 'u5'),
        ('TCB-LT-0185', N'laptop', N'Dell Latitude 5440', 'u12'),
        ('TCB-DT-0031', N'desktop', N'Dell OptiPlex 7010', 'u2'),
        ('TCB-DT-0038', N'desktop', N'Dell OptiPlex 7010', 'u4'),
        ('TCB-MN-0207', N'monitor', N'Dell P2423D 24-inch', 'u1'),
        ('TCB-MN-0212', N'monitor', N'Dell P2423D 24-inch', 'u4'),
        ('TCB-MN-0219', N'monitor', N'iiyama ProLite XUB2493', 'u8'),
        ('TCB-DK-0088', N'dock', N'Dell WD19S dock', 'u1'),
        ('TCB-DK-0091', N'dock', N'Lenovo USB-C Universal Dock', 'u10'),
        ('TCB-HS-0054', N'headset', N'Jabra Evolve2 55', 'u8'),
        ('TCB-HS-0061', N'headset', N'Poly Voyager Focus 2', 'u6');
END
GO

-- All 5 CannedResponses rows from buildSeed() (ids ported verbatim as c1-c5).
IF NOT EXISTS (SELECT 1 FROM dbo.CannedResponses WHERE Id = 'c1')
BEGIN
    INSERT INTO dbo.CannedResponses (Id, Title, Body, SortOrder) VALUES
        ('c1', N'Acknowledgement', N'Hi {recipient.first},

Thanks for contacting IT Support. Ticket {ticket.number} ({ticket.subject}) is logged and I am looking into it now.

Kind regards,
{agent.name}
TopSupport', 0),
        ('c2', N'Need more information', N'Hi {recipient.first},

To help with ticket {ticket.number}, could you send a screenshot of the error and tell me when it last worked?

Thanks,
{agent.name}', 1),
        ('c3', N'Remote session', N'Hi {recipient.first},

I would like to connect to your computer to look at this. Please reply with a time today when you are free for 20 minutes.

{agent.name}', 2),
        ('c4', N'Courier collection booked', N'Hi {recipient.first},

A courier will collect the device for ticket {ticket.number} on the next working day between 9am and 5pm. Please leave it in the returns bag provided.

{agent.name}', 3),
        ('c5', N'Resolved, please confirm', N'Hi {recipient.first},

I believe ticket {ticket.number} is now fixed. If anything is still wrong, reply to this message and the ticket will reopen.

Kind regards,
{agent.name}', 4);
END
GO

-- Full ticket set ported from buildSeed() specs (sorted by created-date ascending, numbered from 060001).
IF NOT EXISTS (SELECT 1 FROM dbo.Tickets WHERE Id = 'T060001')
BEGIN

    -- Ticket 1/30: T060001 (leaver) "Leaver: Hannah Doyle"
    DECLARE @t0_Created DATETIMEOFFSET = DATEADD(HOUR, -2040, SYSDATETIMEOFFSET());
    DECLARE @t0_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"leavingDate":"@@DAY9@@","lineManager":"u8","returnAssets":["TCB-LT-0163","TCB-DK-0091"],"mailbox":"Archive","mailboxTarget":"","details":"Hannah has resigned. Her last day is in four weeks."}';
    SET @t0_FieldsJson = REPLACE(@t0_FieldsJson, '@@DAY9@@', FORMAT(DATEADD(DAY, -57, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060001', @Number = '060001', @Subject = N'Leaver: Hannah Doyle', @Topic = 'leaver', @Department = N'IT Business Support',
        @Status = 'Closed', @Priority = 'Normal', @RequesterUserId = 'u3', @AffectedUserId = 'u10',
        @CreatedByType = 'user', @CreatedById = 'u3', @Created = @t0_Created, @Updated = @t0_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t0_FieldsJson;
    DECLARE @ee1_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t0_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e1', @TicketId = 'T060001', @Kind = 'Event', @At = @ee1_At,
        @ByType = 'user', @ById = 'u3', @Text = N'Created by L. Brennan', @Html = NULL;
    DECLARE @ee2_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t0_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e2', @TicketId = 'T060001', @Kind = 'Event', @At = @ee2_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee3_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t0_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e3', @TicketId = 'T060001', @Kind = 'Event', @At = @ee3_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee4_At DATETIMEOFFSET = DATEADD(MINUTE, 4320, @t0_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e4', @TicketId = 'T060001', @Kind = 'Reply', @At = @ee4_At,
        @ByType = 'user', @ById = 'u3', @Text = NULL, @Html = N'<p>Update: Hannah has withdrawn her resignation and is staying. Please cancel this request.</p>';
    DECLARE @ee5_At DATETIMEOFFSET = DATEADD(MINUTE, 4440, @t0_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e5', @TicketId = 'T060001', @Kind = 'Event', @At = @ee5_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by J. Moore with status Closed', @Html = NULL;
    DECLARE @k1_Due DATE = CAST(DATEADD(DAY, -57, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k1', @TicketId = 'T060001', @Text = N'Disable account', @AssigneeAgentId = 'a1',
        @Due = @k1_Due, @Done = 0;
    DECLARE @k2_Due DATE = CAST(DATEADD(DAY, -57, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k2', @TicketId = 'T060001', @Text = N'Recover laptop', @AssigneeAgentId = 'a1',
        @Due = @k2_Due, @Done = 0;
    DECLARE @k3_Due DATE = CAST(DATEADD(DAY, -56, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k3', @TicketId = 'T060001', @Text = N'Remove licences', @AssigneeAgentId = 'a5',
        @Due = @k3_Due, @Done = 0;
    DECLARE @k4_Due DATE = CAST(DATEADD(DAY, -56, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k4', @TicketId = 'T060001', @Text = N'Archive mailbox', @AssigneeAgentId = 'a5',
        @Due = @k4_Due, @Done = 0;

    -- Ticket 2/30: T060002 (newuser) "New starter: HR Assistant, Horwich"
    DECLARE @t1_Created DATETIMEOFFSET = DATEADD(HOUR, -1790, SYSDATETIMEOFFSET());
    DECLARE @t1_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"newName":"Isla Hartley","startDate":"@@DAY8@@","jobTitle":"HR Assistant","department":"HR","lineManager":"u3","software":["Microsoft 365","Adobe Acrobat Pro"],"deviceNeeded":"Standard laptop","details":""}';
    SET @t1_FieldsJson = REPLACE(@t1_FieldsJson, '@@DAY8@@', FORMAT(DATEADD(DAY, -60, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060002', @Number = '060002', @Subject = N'New starter: HR Assistant, Horwich', @Topic = 'newuser', @Department = N'IT Business Support',
        @Status = 'Closed', @Priority = 'Normal', @RequesterUserId = 'u3', @AffectedUserId = 'u3',
        @CreatedByType = 'user', @CreatedById = 'u3', @Created = @t1_Created, @Updated = @t1_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t1_FieldsJson;
    DECLARE @ee6_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t1_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e6', @TicketId = 'T060002', @Kind = 'Event', @At = @ee6_At,
        @ByType = 'user', @ById = 'u3', @Text = N'Created by L. Brennan', @Html = NULL;
    DECLARE @ee7_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t1_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e7', @TicketId = 'T060002', @Kind = 'Event', @At = @ee7_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee8_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t1_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e8', @TicketId = 'T060002', @Kind = 'Event', @At = @ee8_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee9_At DATETIMEOFFSET = DATEADD(MINUTE, 1800, @t1_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e9', @TicketId = 'T060002', @Kind = 'Reply', @At = @ee9_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Hi Leah, Isla''s account and laptop are ready. We''ll give her a temporary password in person on her first day.</p>';
    DECLARE @ee10_At DATETIMEOFFSET = DATEADD(MINUTE, 1860, @t1_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e10', @TicketId = 'T060002', @Kind = 'Event', @At = @ee10_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by E. Shaw with status Closed', @Html = NULL;

    -- Ticket 3/30: T060003 (hardware) "Laptop fan very loud"
    DECLARE @t2_Created DATETIMEOFFSET = DATEADD(HOUR, -1430, SYSDATETIMEOFFSET());
    DECLARE @t2_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Laptop","device":"TCB-LT-0142","problem":"Other","started":"This week","impact":"inconvenience","tried":["Restarted the device"],"details":"The fan is very loud even when I''m only using email.","location":"desk","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060003', @Number = '060003', @Subject = N'Laptop fan very loud', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Closed', @Priority = 'Normal', @RequesterUserId = 'u1', @AffectedUserId = 'u1',
        @CreatedByType = 'user', @CreatedById = 'u1', @Created = @t2_Created, @Updated = @t2_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t2_FieldsJson;
    DECLARE @ee11_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e11', @TicketId = 'T060003', @Kind = 'Event', @At = @ee11_At,
        @ByType = 'user', @ById = 'u1', @Text = N'Created by A. Patel', @Html = NULL;
    DECLARE @ee12_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e12', @TicketId = 'T060003', @Kind = 'Event', @At = @ee12_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee13_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e13', @TicketId = 'T060003', @Kind = 'Event', @At = @ee13_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee14_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e14', @TicketId = 'T060003', @Kind = 'Reply', @At = @ee14_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Amira, I''ve cleaned the vents and updated the BIOS. It should be much quieter now.</p>';
    DECLARE @ee15_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e15', @TicketId = 'T060003', @Kind = 'Event', @At = @ee15_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by J. Moore with status Resolved', @Html = NULL;
    DECLARE @ee16_At DATETIMEOFFSET = DATEADD(MINUTE, 10320, @t2_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e16', @TicketId = 'T060003', @Kind = 'Event', @At = @ee16_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by System with status Closed (no reply for 7 days)', @Html = NULL;

    -- Ticket 4/30: T060004 (software) "DocuSign add-in missing from Word"
    DECLARE @t3_Created DATETIMEOFFSET = DATEADD(HOUR, -1080, SYSDATETIMEOFFSET());
    DECLARE @t3_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Microsoft Word","version":"Microsoft 365","errorMsg":"","impact":"inconvenience","details":"The DocuSign add-in has disappeared from the Word ribbon."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060004', @Number = '060004', @Subject = N'DocuSign add-in missing from Word', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'Closed', @Priority = 'Normal', @RequesterUserId = 'u12', @AffectedUserId = 'u12',
        @CreatedByType = 'user', @CreatedById = 'u12', @Created = @t3_Created, @Updated = @t3_Created,
        @AssignedAgentId = 'a2', @AssignedTeamId = 't1',
        @FieldsJson = @t3_FieldsJson;
    DECLARE @ee17_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e17', @TicketId = 'T060004', @Kind = 'Event', @At = @ee17_At,
        @ByType = 'user', @ById = 'u12', @Text = N'Created by R. Adeyemi', @Html = NULL;
    DECLARE @ee18_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e18', @TicketId = 'T060004', @Kind = 'Event', @At = @ee18_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee19_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e19', @TicketId = 'T060004', @Kind = 'Event', @At = @ee19_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to R. Singh', @Html = NULL;
    DECLARE @ee20_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e20', @TicketId = 'T060004', @Kind = 'Reply', @At = @ee20_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Hi Ruth, I''ve re-enabled the add-in. Restart Word and it will appear under the Insert tab.</p>';
    DECLARE @ee21_At DATETIMEOFFSET = DATEADD(MINUTE, 300, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e21', @TicketId = 'T060004', @Kind = 'Reply', @At = @ee21_At,
        @ByType = 'user', @ById = 'u12', @Text = NULL, @Html = N'<p>It''s back, thanks.</p>';
    DECLARE @ee22_At DATETIMEOFFSET = DATEADD(MINUTE, 360, @t3_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e22', @TicketId = 'T060004', @Kind = 'Event', @At = @ee22_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by R. Singh with status Closed', @Html = NULL;

    -- Ticket 5/30: T060005 (hardware) "Laptop screen cracked"
    DECLARE @t4_Created DATETIMEOFFSET = DATEADD(HOUR, -960, SYSDATETIMEOFFSET());
    DECLARE @t4_FieldsJson NVARCHAR(MAX) = N'{"attachments":[{"name":"cracked-screen.jpg","size":2310000}],"deviceType":"Laptop","device":"TCB-LT-0156","problem":"Physical damage","started":"Today","impact":"cant","tried":["Nothing yet"],"details":"I dropped my laptop at a client site and the screen is cracked across the middle.","location":"remote","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060005', @Number = '060005', @Subject = N'Laptop screen cracked', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Resolved', @Priority = 'Urgent', @RequesterUserId = 'u9', @AffectedUserId = 'u9',
        @CreatedByType = 'user', @CreatedById = 'u9', @Created = @t4_Created, @Updated = @t4_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t4_FieldsJson;
    DECLARE @ee23_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e23', @TicketId = 'T060005', @Kind = 'Event', @At = @ee23_At,
        @ByType = 'user', @ById = 'u9', @Text = N'Created by E. Clarke', @Html = NULL;
    DECLARE @ee24_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e24', @TicketId = 'T060005', @Kind = 'Event', @At = @ee24_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee25_At DATETIMEOFFSET = DATEADD(MINUTE, 30, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e25', @TicketId = 'T060005', @Kind = 'Event', @At = @ee25_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee26_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e26', @TicketId = 'T060005', @Kind = 'Reply', @At = @ee26_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Ethan, I''m sending a replacement laptop by next-day courier with a returns bag for the damaged one.</p>';
    DECLARE @ee27_At DATETIMEOFFSET = DATEADD(MINUTE, 1560, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e27', @TicketId = 'T060005', @Kind = 'Reply', @At = @ee27_At,
        @ByType = 'user', @ById = 'u9', @Text = NULL, @Html = N'<p>Replacement arrived and is working. The damaged laptop is in the bag ready for collection.</p>';
    DECLARE @ee28_At DATETIMEOFFSET = DATEADD(MINUTE, 1800, @t4_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e28', @TicketId = 'T060005', @Kind = 'Event', @At = @ee28_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by J. Moore with status Resolved', @Html = NULL;

    -- Ticket 6/30: T060006 (server) "New development VM for API prototype"
    DECLARE @t5_Created DATETIMEOFFSET = DATEADD(HOUR, -600, SYSDATETIMEOFFSET());
    DECLARE @t5_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"environment":"Development","requestType":"New server","serverName":"TCB-DEV-API03","justification":"Prototype API for the supplier portal. Small Linux VM: 2 vCPU, 4 GB RAM.","neededBy":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060006', @Number = '060006', @Subject = N'New development VM for API prototype', @Topic = 'server', @Department = N'Infrastructure',
        @Status = 'Resolved', @Priority = 'Normal', @RequesterUserId = 'u11', @AffectedUserId = 'u11',
        @CreatedByType = 'user', @CreatedById = 'u11', @Created = @t5_Created, @Updated = @t5_Created,
        @AssignedAgentId = 'a4', @AssignedTeamId = 't2',
        @FieldsJson = @t5_FieldsJson;
    DECLARE @ee29_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t5_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e29', @TicketId = 'T060006', @Kind = 'Event', @At = @ee29_At,
        @ByType = 'user', @ById = 'u11', @Text = N'Created by O. Pryce', @Html = NULL;
    DECLARE @ee30_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t5_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e30', @TicketId = 'T060006', @Kind = 'Event', @At = @ee30_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;
    DECLARE @ee31_At DATETIMEOFFSET = DATEADD(MINUTE, 360, @t5_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e31', @TicketId = 'T060006', @Kind = 'Event', @At = @ee31_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to C. Reid', @Html = NULL;
    DECLARE @ee32_At DATETIMEOFFSET = DATEADD(MINUTE, 1680, @t5_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e32', @TicketId = 'T060006', @Kind = 'Reply', @At = @ee32_At,
        @ByType = 'agent', @ById = 'a4', @Text = NULL, @Html = N'<p>Hi Owen, TCB-DEV-API03 is built. Connection details are in the Infrastructure vault and you have admin rights.</p>';
    DECLARE @ee33_At DATETIMEOFFSET = DATEADD(MINUTE, 3000, @t5_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e33', @TicketId = 'T060006', @Kind = 'Event', @At = @ee33_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by C. Reid with status Resolved', @Html = NULL;

    -- Ticket 7/30: T060007 (software) "Teams video freezing on calls"
    DECLARE @t6_Created DATETIMEOFFSET = DATEADD(HOUR, -480, SYSDATETIMEOFFSET());
    DECLARE @t6_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Microsoft Teams","version":"New Teams","errorMsg":"","impact":"difficulty","details":"Video freezes after about 2 minutes on every call."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060007', @Number = '060007', @Subject = N'Teams video freezing on calls', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'Resolved', @Priority = 'High', @RequesterUserId = 'u5', @AffectedUserId = 'u5',
        @CreatedByType = 'user', @CreatedById = 'u5', @Created = @t6_Created, @Updated = @t6_Created,
        @AssignedAgentId = 'a2', @AssignedTeamId = 't1',
        @FieldsJson = @t6_FieldsJson;
    DECLARE @ee34_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t6_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e34', @TicketId = 'T060007', @Kind = 'Event', @At = @ee34_At,
        @ByType = 'user', @ById = 'u5', @Text = N'Created by S. Lumb', @Html = NULL;
    DECLARE @ee35_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t6_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e35', @TicketId = 'T060007', @Kind = 'Event', @At = @ee35_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee36_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t6_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e36', @TicketId = 'T060007', @Kind = 'Event', @At = @ee36_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to R. Singh', @Html = NULL;
    DECLARE @ee37_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t6_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e37', @TicketId = 'T060007', @Kind = 'Reply', @At = @ee37_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Hi Sophie, I''ve cleared the Teams cache and updated your graphics driver remotely. Please try a test call.</p>';
    DECLARE @ee38_At DATETIMEOFFSET = DATEADD(MINUTE, 1440, @t6_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e38', @TicketId = 'T060007', @Kind = 'Event', @At = @ee38_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by R. Singh with status Resolved', @Html = NULL;

    -- Ticket 8/30: T060008 (hardware) "Monitor shows "No signal" when docked"
    DECLARE @t7_Created DATETIMEOFFSET = DATEADD(HOUR, -360, SYSDATETIMEOFFSET());
    DECLARE @t7_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Monitor","device":"TCB-MN-0207","problem":"Display problem","started":"Today","impact":"difficulty","tried":["Checked cables and power"],"details":"The monitor says \"No signal\" when the laptop is docked.","location":"desk","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060008', @Number = '060008', @Subject = N'Monitor shows "No signal" when docked', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Resolved', @Priority = 'High', @RequesterUserId = 'u1', @AffectedUserId = 'u1',
        @CreatedByType = 'user', @CreatedById = 'u1', @Created = @t7_Created, @Updated = @t7_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t7_FieldsJson;
    DECLARE @ee39_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e39', @TicketId = 'T060008', @Kind = 'Event', @At = @ee39_At,
        @ByType = 'user', @ById = 'u1', @Text = N'Created by A. Patel', @Html = NULL;
    DECLARE @ee40_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e40', @TicketId = 'T060008', @Kind = 'Event', @At = @ee40_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee41_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e41', @TicketId = 'T060008', @Kind = 'Event', @At = @ee41_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee42_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e42', @TicketId = 'T060008', @Kind = 'Reply', @At = @ee42_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Amira, I''ve replaced the damaged USB-C cable from the dock. Can you confirm both screens are working?</p>';
    DECLARE @ee43_At DATETIMEOFFSET = DATEADD(MINUTE, 300, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e43', @TicketId = 'T060008', @Kind = 'Reply', @At = @ee43_At,
        @ByType = 'user', @ById = 'u1', @Text = NULL, @Html = N'<p>Both working now, thank you!</p>';
    DECLARE @ee44_At DATETIMEOFFSET = DATEADD(MINUTE, 360, @t7_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e44', @TicketId = 'T060008', @Kind = 'Event', @At = @ee44_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by J. Moore with status Resolved', @Html = NULL;

    -- Ticket 9/30: T060009 (general) "How do I set up an out-of-office reply?"
    DECLARE @t8_Created DATETIMEOFFSET = DATEADD(HOUR, -290, SYSDATETIMEOFFSET());
    DECLARE @t8_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"details":"How do I set up an automatic out-of-office reply in Outlook? I am away next week."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060009', @Number = '060009', @Subject = N'How do I set up an out-of-office reply?', @Topic = 'general', @Department = N'IT Business Support',
        @Status = 'Resolved', @Priority = 'Normal', @RequesterUserId = 'u4', @AffectedUserId = 'u4',
        @CreatedByType = 'user', @CreatedById = 'u4', @Created = @t8_Created, @Updated = @t8_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t8_FieldsJson;
    DECLARE @ee45_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t8_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e45', @TicketId = 'T060009', @Kind = 'Event', @At = @ee45_At,
        @ByType = 'user', @ById = 'u4', @Text = N'Created by D. Okafor', @Html = NULL;
    DECLARE @ee46_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t8_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e46', @TicketId = 'T060009', @Kind = 'Event', @At = @ee46_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee47_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t8_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e47', @TicketId = 'T060009', @Kind = 'Event', @At = @ee47_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee48_At DATETIMEOFFSET = DATEADD(MINUTE, 90, @t8_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e48', @TicketId = 'T060009', @Kind = 'Reply', @At = @ee48_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Hi Daniel, in Outlook go to <strong>File › Automatic Replies</strong>, choose "Send automatic replies" and set your dates. There is a guide in the knowledge base too.</p>';
    DECLARE @ee49_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t8_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e49', @TicketId = 'T060009', @Kind = 'Event', @At = @ee49_At,
        @ByType = 'system', @ById = NULL, @Text = N'Closed by E. Shaw with status Resolved', @Html = NULL;

    -- Ticket 10/30: T060010 (newuser) "New starter: Customer Services Advisor, Manchester"
    DECLARE @t9_Created DATETIMEOFFSET = DATEADD(HOUR, -240, SYSDATETIMEOFFSET());
    DECLARE @t9_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"newName":"Callie Moss","startDate":"@@DAY5@@","jobTitle":"Customer Services Advisor","department":"Customer Services","lineManager":"u8","software":["Microsoft 365","Teams Phone","Salesforce"],"deviceNeeded":"Standard laptop","details":""}';
    SET @t9_FieldsJson = REPLACE(@t9_FieldsJson, '@@DAY5@@', FORMAT(DATEADD(DAY, 3, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060010', @Number = '060010', @Subject = N'New starter: Customer Services Advisor, Manchester', @Topic = 'newuser', @Department = N'IT Business Support',
        @Status = 'AwaitingUser', @Priority = 'Normal', @RequesterUserId = 'u8', @AffectedUserId = 'u8',
        @CreatedByType = 'user', @CreatedById = 'u8', @Created = @t9_Created, @Updated = @t9_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t9_FieldsJson;
    DECLARE @ee50_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t9_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e50', @TicketId = 'T060010', @Kind = 'Event', @At = @ee50_At,
        @ByType = 'user', @ById = 'u8', @Text = N'Created by G. Whitfield', @Html = NULL;
    DECLARE @ee51_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t9_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e51', @TicketId = 'T060010', @Kind = 'Event', @At = @ee51_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee52_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t9_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e52', @TicketId = 'T060010', @Kind = 'Event', @At = @ee52_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee53_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t9_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e53', @TicketId = 'T060010', @Kind = 'Reply', @At = @ee53_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Grace, the laptop is built. Which phone queue should Callie join so we can set up Teams Phone?</p>';
    DECLARE @ee54_At DATETIMEOFFSET = DATEADD(MINUTE, 246, @t9_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e54', @TicketId = 'T060010', @Kind = 'Event', @At = @ee54_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by J. Moore', @Html = NULL;

    -- Ticket 11/30: T060011 (general) "Request for a second monitor"
    DECLARE @t10_Created DATETIMEOFFSET = DATEADD(HOUR, -216, SYSDATETIMEOFFSET());
    DECLARE @t10_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"details":"Could I have a second monitor at my desk? I spend most of the day comparing campaign reports side by side."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060011', @Number = '060011', @Subject = N'Request for a second monitor', @Topic = 'general', @Department = N'IT Business Support',
        @Status = 'AwaitingUser', @Priority = 'Normal', @RequesterUserId = 'u7', @AffectedUserId = 'u7',
        @CreatedByType = 'user', @CreatedById = 'u7', @Created = @t10_Created, @Updated = @t10_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t10_FieldsJson;
    DECLARE @ee55_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t10_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e55', @TicketId = 'T060011', @Kind = 'Event', @At = @ee55_At,
        @ByType = 'user', @ById = 'u7', @Text = N'Created by P. Nair', @Html = NULL;
    DECLARE @ee56_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t10_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e56', @TicketId = 'T060011', @Kind = 'Event', @At = @ee56_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee57_At DATETIMEOFFSET = DATEADD(MINUTE, 300, @t10_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e57', @TicketId = 'T060011', @Kind = 'Event', @At = @ee57_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee58_At DATETIMEOFFSET = DATEADD(MINUTE, 360, @t10_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e58', @TicketId = 'T060011', @Kind = 'Reply', @At = @ee58_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Hi Priya, second monitors need line manager approval. Could you ask your manager to reply on this ticket?</p>';
    DECLARE @ee59_At DATETIMEOFFSET = DATEADD(MINUTE, 366, @t10_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e59', @TicketId = 'T060011', @Kind = 'Event', @At = @ee59_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by E. Shaw', @Html = NULL;

    -- Ticket 12/30: T060012 (hardware) "No sound in left ear of headset"
    DECLARE @t11_Created DATETIMEOFFSET = DATEADD(HOUR, -190, SYSDATETIMEOFFSET());
    DECLARE @t11_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Headset/Phone","device":"TCB-HS-0061","problem":"Audio or call problem","started":"Longer than a week","impact":"inconvenience","tried":["Restarted the device","Tried another device"],"details":"No sound in the left ear. Same problem on another laptop.","location":"desk","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060012', @Number = '060012', @Subject = N'No sound in left ear of headset', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'AwaitingUser', @Priority = 'Normal', @RequesterUserId = 'u6', @AffectedUserId = 'u6',
        @CreatedByType = 'user', @CreatedById = 'u6', @Created = @t11_Created, @Updated = @t11_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t11_FieldsJson;
    DECLARE @ee60_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e60', @TicketId = 'T060012', @Kind = 'Event', @At = @ee60_At,
        @ByType = 'user', @ById = 'u6', @Text = N'Created by M. Webb', @Html = NULL;
    DECLARE @ee61_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e61', @TicketId = 'T060012', @Kind = 'Event', @At = @ee61_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee62_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e62', @TicketId = 'T060012', @Kind = 'Event', @At = @ee62_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee63_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e63', @TicketId = 'T060012', @Kind = 'Reply', @At = @ee63_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Marcus, that''s a hardware fault and it''s under warranty, so I can swap it. Are you in the Manchester office on Thursday?</p>';
    DECLARE @ee64_At DATETIMEOFFSET = DATEADD(MINUTE, 246, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e64', @TicketId = 'T060012', @Kind = 'Event', @At = @ee64_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by J. Moore', @Html = NULL;
    DECLARE @ee65_At DATETIMEOFFSET = DATEADD(MINUTE, 6000, @t11_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e65', @TicketId = 'T060012', @Kind = 'Note', @At = @ee65_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Chased on Teams, no reply yet. Close after 5 working days if no answer.</p>';

    -- Ticket 13/30: T060013 (leaver) "Leaver: Ethan Clarke"
    DECLARE @t12_Created DATETIMEOFFSET = DATEADD(HOUR, -144, SYSDATETIMEOFFSET());
    DECLARE @t12_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"leavingDate":"@@DAY3@@","lineManager":"u5","returnAssets":["TCB-LT-0156"],"mailbox":"Forward to someone","mailboxTarget":"u5","details":"Ethan works from home. Please arrange courier collection of his laptop."}';
    SET @t12_FieldsJson = REPLACE(@t12_FieldsJson, '@@DAY3@@', FORMAT(DATEADD(DAY, 8, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060013', @Number = '060013', @Subject = N'Leaver: Ethan Clarke', @Topic = 'leaver', @Department = N'IT Business Support',
        @Status = 'InProgress', @Priority = 'Normal', @RequesterUserId = 'u3', @AffectedUserId = 'u9',
        @CreatedByType = 'user', @CreatedById = 'u3', @Created = @t12_Created, @Updated = @t12_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t12_FieldsJson;
    DECLARE @ee66_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t12_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e66', @TicketId = 'T060013', @Kind = 'Event', @At = @ee66_At,
        @ByType = 'user', @ById = 'u3', @Text = N'Created by L. Brennan', @Html = NULL;
    DECLARE @ee67_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t12_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e67', @TicketId = 'T060013', @Kind = 'Event', @At = @ee67_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee68_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t12_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e68', @TicketId = 'T060013', @Kind = 'Event', @At = @ee68_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee69_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t12_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e69', @TicketId = 'T060013', @Kind = 'Reply', @At = @ee69_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Thanks Leah. I''ve booked a courier collection from Ethan''s home on his last day and will disable his account at 17:30 that evening.</p>';
    DECLARE @ee70_At DATETIMEOFFSET = DATEADD(MINUTE, 186, @t12_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e70', @TicketId = 'T060013', @Kind = 'Event', @At = @ee70_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by J. Moore', @Html = NULL;
    INSERT INTO dbo.TicketViewers (TicketId, AgentId) VALUES
        ('T060013', 'a2');
    DECLARE @k5_Due DATE = CAST(DATEADD(DAY, 8, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k5', @TicketId = 'T060013', @Text = N'Disable account', @AssigneeAgentId = 'a1',
        @Due = @k5_Due, @Done = 0;
    DECLARE @k6_Due DATE = CAST(DATEADD(DAY, 8, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k6', @TicketId = 'T060013', @Text = N'Recover laptop', @AssigneeAgentId = 'a1',
        @Due = @k6_Due, @Done = 0;
    DECLARE @k7_Due DATE = CAST(DATEADD(DAY, 9, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k7', @TicketId = 'T060013', @Text = N'Remove licences', @AssigneeAgentId = 'a5',
        @Due = @k7_Due, @Done = 0;
    DECLARE @k8_Due DATE = CAST(DATEADD(DAY, 9, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k8', @TicketId = 'T060013', @Text = N'Archive mailbox', @AssigneeAgentId = 'a5',
        @Due = @k8_Due, @Done = 0;

    -- Ticket 14/30: T060014 (hardware) "Dock not detecting external monitor"
    DECLARE @t13_Created DATETIMEOFFSET = DATEADD(HOUR, -120, SYSDATETIMEOFFSET());
    DECLARE @t13_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Docking station","device":"TCB-DK-0091","problem":"Accessory not working","started":"This week","impact":"difficulty","tried":["Restarted the device","Checked cables and power","Tried a different port or dock"],"details":"The dock charges the laptop but the external monitor shows \"No signal\".","location":"remote","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060014', @Number = '060014', @Subject = N'Dock not detecting external monitor', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'InProgress', @Priority = 'High', @RequesterUserId = 'u10', @AffectedUserId = 'u10',
        @CreatedByType = 'user', @CreatedById = 'u10', @Created = @t13_Created, @Updated = @t13_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t13_FieldsJson;
    DECLARE @ee71_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e71', @TicketId = 'T060014', @Kind = 'Event', @At = @ee71_At,
        @ByType = 'user', @ById = 'u10', @Text = N'Created by H. Doyle', @Html = NULL;
    DECLARE @ee72_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e72', @TicketId = 'T060014', @Kind = 'Event', @At = @ee72_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee73_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e73', @TicketId = 'T060014', @Kind = 'Event', @At = @ee73_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee74_At DATETIMEOFFSET = DATEADD(MINUTE, 300, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e74', @TicketId = 'T060014', @Kind = 'Reply', @At = @ee74_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Hi Hannah, a firmware update should fix this, but I''ve also sent a replacement dock. It should arrive on Thursday.</p>';
    DECLARE @ee75_At DATETIMEOFFSET = DATEADD(MINUTE, 306, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e75', @TicketId = 'T060014', @Kind = 'Event', @At = @ee75_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by E. Shaw', @Html = NULL;
    DECLARE @ee76_At DATETIMEOFFSET = DATEADD(MINUTE, 1800, @t13_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e76', @TicketId = 'T060014', @Kind = 'Note', @At = @ee76_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Courier ref DX-2219-8841. Ask for the old dock back in the same box.</p>';

    -- Ticket 15/30: T060015 (software) "Excel macros blocked after update"
    DECLARE @t14_Created DATETIMEOFFSET = DATEADD(HOUR, -100, SYSDATETIMEOFFSET());
    DECLARE @t14_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Microsoft Excel","version":"Microsoft 365","errorMsg":"Microsoft has blocked macros from running because the source of this file is untrusted.","impact":"difficulty","details":"Our bank reconciliation workbook stopped running its macros after Tuesday''s update."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060015', @Number = '060015', @Subject = N'Excel macros blocked after update', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'AwaitingUser', @Priority = 'High', @RequesterUserId = 'u1', @AffectedUserId = 'u1',
        @CreatedByType = 'user', @CreatedById = 'u1', @Created = @t14_Created, @Updated = @t14_Created,
        @AssignedAgentId = 'a2', @AssignedTeamId = 't1',
        @FieldsJson = @t14_FieldsJson;
    DECLARE @ee77_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t14_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e77', @TicketId = 'T060015', @Kind = 'Event', @At = @ee77_At,
        @ByType = 'user', @ById = 'u1', @Text = N'Created by A. Patel', @Html = NULL;
    DECLARE @ee78_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t14_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e78', @TicketId = 'T060015', @Kind = 'Event', @At = @ee78_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee79_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t14_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e79', @TicketId = 'T060015', @Kind = 'Event', @At = @ee79_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to R. Singh', @Html = NULL;
    DECLARE @ee80_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t14_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e80', @TicketId = 'T060015', @Kind = 'Reply', @At = @ee80_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Hi Amira, this is the new macro security policy. Where is the workbook saved? If it''s in the Finance SharePoint library I can add it as a trusted location.</p>';
    DECLARE @ee81_At DATETIMEOFFSET = DATEADD(MINUTE, 186, @t14_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e81', @TicketId = 'T060015', @Kind = 'Event', @At = @ee81_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by R. Singh', @Html = NULL;

    -- Ticket 16/30: T060016 (server) "Read access to the finance reporting server"
    DECLARE @t15_Created DATETIMEOFFSET = DATEADD(HOUR, -98, SYSDATETIMEOFFSET());
    DECLARE @t15_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"environment":"Production","requestType":"Access to a server","serverName":"TCB-PRD-RPT01","justification":"I need read access to run month-end variance reports myself instead of waiting for exports.","neededBy":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060016', @Number = '060016', @Subject = N'Read access to the finance reporting server', @Topic = 'server', @Department = N'Infrastructure',
        @Status = 'InProgress', @Priority = 'Normal', @RequesterUserId = 'u4', @AffectedUserId = 'u4',
        @CreatedByType = 'user', @CreatedById = 'u4', @Created = @t15_Created, @Updated = @t15_Created,
        @AssignedAgentId = 'a4', @AssignedTeamId = 't2',
        @FieldsJson = @t15_FieldsJson;
    DECLARE @ee82_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e82', @TicketId = 'T060016', @Kind = 'Event', @At = @ee82_At,
        @ByType = 'user', @ById = 'u4', @Text = N'Created by D. Okafor', @Html = NULL;
    DECLARE @ee83_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e83', @TicketId = 'T060016', @Kind = 'Event', @At = @ee83_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;
    DECLARE @ee84_At DATETIMEOFFSET = DATEADD(MINUTE, 300, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e84', @TicketId = 'T060016', @Kind = 'Event', @At = @ee84_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to C. Reid', @Html = NULL;
    DECLARE @ee85_At DATETIMEOFFSET = DATEADD(MINUTE, 360, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e85', @TicketId = 'T060016', @Kind = 'Note', @At = @ee85_At,
        @ByType = 'agent', @ById = 'a4', @Text = NULL, @Html = N'<p>Needs Finance Director approval before production access.</p>';
    DECLARE @ee86_At DATETIMEOFFSET = DATEADD(MINUTE, 1560, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e86', @TicketId = 'T060016', @Kind = 'Reply', @At = @ee86_At,
        @ByType = 'agent', @ById = 'a4', @Text = NULL, @Html = N'<p>Hi Daniel, approval received. I''ve added you to the RPT-Readers group. Access will work after your next sign-in; I''ll keep this open until you confirm.</p>';
    DECLARE @ee87_At DATETIMEOFFSET = DATEADD(MINUTE, 1566, @t15_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e87', @TicketId = 'T060016', @Kind = 'Event', @At = @ee87_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by C. Reid', @Html = NULL;

    -- Ticket 17/30: T060017 (vpn) "VPN for two weeks at the Chester client site"
    DECLARE @t16_Created DATETIMEOFFSET = DATEADD(HOUR, -96, SYSDATETIMEOFFSET());
    DECLARE @t16_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"vpnDevice":"TCB-LT-0171","reason":"Working from the Chester client site for two weeks and need the file shares and Jira.","dateFrom":"@@DAY0@@"}';
    SET @t16_FieldsJson = REPLACE(@t16_FieldsJson, '@@DAY0@@', FORMAT(DATEADD(DAY, 10, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060017', @Number = '060017', @Subject = N'VPN for two weeks at the Chester client site', @Topic = 'vpn', @Department = N'Infrastructure',
        @Status = 'Open', @Priority = 'Low', @RequesterUserId = 'u11', @AffectedUserId = 'u11',
        @CreatedByType = 'user', @CreatedById = 'u11', @Created = @t16_Created, @Updated = @t16_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't2',
        @FieldsJson = @t16_FieldsJson;
    DECLARE @ee88_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t16_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e88', @TicketId = 'T060017', @Kind = 'Event', @At = @ee88_At,
        @ByType = 'user', @ById = 'u11', @Text = N'Created by O. Pryce', @Html = NULL;
    DECLARE @ee89_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t16_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e89', @TicketId = 'T060017', @Kind = 'Event', @At = @ee89_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;
    DECLARE @ee90_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t16_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e90', @TicketId = 'T060017', @Kind = 'Event', @At = @ee90_At,
        @ByType = 'system', @ById = NULL, @Text = N'Priority changed from Normal to Low by C. Reid', @Html = NULL;

    -- Ticket 18/30: T060018 (vpn) "VPN access for contract reviews from home"
    DECLARE @t17_Created DATETIMEOFFSET = DATEADD(HOUR, -75, SYSDATETIMEOFFSET());
    DECLARE @t17_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"vpnDevice":"TCB-LT-0185","reason":"I review contracts from home two days a week and need the legal file share.","dateFrom":"@@DAY6@@"}';
    SET @t17_FieldsJson = REPLACE(@t17_FieldsJson, '@@DAY6@@', FORMAT(DATEADD(DAY, 2, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060018', @Number = '060018', @Subject = N'VPN access for contract reviews from home', @Topic = 'vpn', @Department = N'Infrastructure',
        @Status = 'AwaitingUser', @Priority = 'Normal', @RequesterUserId = 'u12', @AffectedUserId = 'u12',
        @CreatedByType = 'user', @CreatedById = 'u12', @Created = @t17_Created, @Updated = @t17_Created,
        @AssignedAgentId = 'a5', @AssignedTeamId = 't2',
        @FieldsJson = @t17_FieldsJson;
    DECLARE @ee91_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t17_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e91', @TicketId = 'T060018', @Kind = 'Event', @At = @ee91_At,
        @ByType = 'user', @ById = 'u12', @Text = N'Created by R. Adeyemi', @Html = NULL;
    DECLARE @ee92_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t17_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e92', @TicketId = 'T060018', @Kind = 'Event', @At = @ee92_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;
    DECLARE @ee93_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t17_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e93', @TicketId = 'T060018', @Kind = 'Event', @At = @ee93_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to N. Hussain', @Html = NULL;
    DECLARE @ee94_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t17_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e94', @TicketId = 'T060018', @Kind = 'Reply', @At = @ee94_At,
        @ByType = 'agent', @ById = 'a5', @Text = NULL, @Html = N'<p>Hi Ruth, before I enable VPN please complete the remote working security checklist in the knowledge base and reply here when it''s done.</p>';
    DECLARE @ee95_At DATETIMEOFFSET = DATEADD(MINUTE, 186, @t17_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e95', @TicketId = 'T060018', @Kind = 'Event', @At = @ee95_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by N. Hussain', @Html = NULL;

    -- Ticket 19/30: T060019 (hardware) "Warehouse desktop running very slowly"
    DECLARE @t18_Created DATETIMEOFFSET = DATEADD(HOUR, -74, SYSDATETIMEOFFSET());
    DECLARE @t18_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Desktop","device":"TCB-DT-0031","problem":"Running slowly","started":"Longer than a week","impact":"difficulty","tried":["Restarted the device"],"details":"It takes about 10 minutes to start and the stock system freezes when I open more than one tab.","location":"desk","visitTime":"Weekdays 7am to 3pm"}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060019', @Number = '060019', @Subject = N'Warehouse desktop running very slowly', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'InProgress', @Priority = 'High', @RequesterUserId = 'u2', @AffectedUserId = 'u2',
        @CreatedByType = 'user', @CreatedById = 'u2', @Created = @t18_Created, @Updated = @t18_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t18_FieldsJson;
    DECLARE @ee96_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e96', @TicketId = 'T060019', @Kind = 'Event', @At = @ee96_At,
        @ByType = 'user', @ById = 'u2', @Text = N'Created by T. Hargreaves', @Html = NULL;
    DECLARE @ee97_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e97', @TicketId = 'T060019', @Kind = 'Event', @At = @ee97_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee98_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e98', @TicketId = 'T060019', @Kind = 'Event', @At = @ee98_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee99_At DATETIMEOFFSET = DATEADD(MINUTE, 90, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e99', @TicketId = 'T060019', @Kind = 'Reply', @At = @ee99_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Hi Tom, the disk is almost full and the PC is on an old build. I''ll come down tomorrow at 8am to re-image it. It takes about an hour.</p>';
    DECLARE @ee100_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e100', @TicketId = 'T060019', @Kind = 'Event', @At = @ee100_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by J. Moore', @Html = NULL;
    DECLARE @ee101_At DATETIMEOFFSET = DATEADD(MINUTE, 1320, @t18_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e101', @TicketId = 'T060019', @Kind = 'Note', @At = @ee101_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Re-image booked for Thursday 08:00. SSD upgrade ordered on PO 4471.</p>';

    -- Ticket 20/30: T060020 (software) "Adobe Acrobat crashes when combining PDFs"
    DECLARE @t19_Created DATETIMEOFFSET = DATEADD(HOUR, -72, SYSDATETIMEOFFSET());
    DECLARE @t19_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Adobe Acrobat","version":"Acrobat Pro 2024","errorMsg":"\"Adobe Acrobat has stopped working\"","impact":"inconvenience","details":"Acrobat crashes every time I combine more than about 10 PDFs into one file for the campaign pack."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060020', @Number = '060020', @Subject = N'Adobe Acrobat crashes when combining PDFs', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u7', @AffectedUserId = 'u7',
        @CreatedByType = 'user', @CreatedById = 'u7', @Created = @t19_Created, @Updated = @t19_Created,
        @AssignedAgentId = 'a2', @AssignedTeamId = 't1',
        @FieldsJson = @t19_FieldsJson;
    DECLARE @ee102_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e102', @TicketId = 'T060020', @Kind = 'Event', @At = @ee102_At,
        @ByType = 'user', @ById = 'u7', @Text = N'Created by P. Nair', @Html = NULL;
    DECLARE @ee103_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e103', @TicketId = 'T060020', @Kind = 'Event', @At = @ee103_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee104_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e104', @TicketId = 'T060020', @Kind = 'Event', @At = @ee104_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to R. Singh', @Html = NULL;
    DECLARE @ee105_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e105', @TicketId = 'T060020', @Kind = 'Reply', @At = @ee105_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Hi Priya,</p><p>Could you try combining the files in two batches of five and let me know if it still crashes? I''ve also pushed the latest Acrobat update to your laptop.</p>';
    DECLARE @ee106_At DATETIMEOFFSET = DATEADD(MINUTE, 246, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e106', @TicketId = 'T060020', @Kind = 'Event', @At = @ee106_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to Awaiting User by R. Singh', @Html = NULL;
    DECLARE @ee107_At DATETIMEOFFSET = DATEADD(MINUTE, 1800, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e107', @TicketId = 'T060020', @Kind = 'Reply', @At = @ee107_At,
        @ByType = 'user', @ById = 'u7', @Text = NULL, @Html = N'<p>Batches of five work, but it still crashes on the final merge.</p>';
    DECLARE @ee108_At DATETIMEOFFSET = DATEADD(MINUTE, 1806, @t19_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e108', @TicketId = 'T060020', @Kind = 'Event', @At = @ee108_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Awaiting User to Open by P. Nair', @Html = NULL;

    -- Ticket 21/30: T060021 (software) "Salesforce reports page will not load"
    DECLARE @t20_Created DATETIMEOFFSET = DATEADD(HOUR, -50, SYSDATETIMEOFFSET());
    DECLARE @t20_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Salesforce","version":"","errorMsg":"Reports page stays blank with a spinning wheel","impact":"difficulty","details":"Since Monday I can''t load any reports in Salesforce. Dashboards load fine. I need the weekly case report for my team meeting."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060021', @Number = '060021', @Subject = N'Salesforce reports page will not load', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'InProgress', @Priority = 'High', @RequesterUserId = 'u10', @AffectedUserId = 'u10',
        @CreatedByType = 'user', @CreatedById = 'u10', @Created = @t20_Created, @Updated = @t20_Created,
        @AssignedAgentId = 'a2', @AssignedTeamId = 't1',
        @FieldsJson = @t20_FieldsJson;
    DECLARE @ee109_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e109', @TicketId = 'T060021', @Kind = 'Event', @At = @ee109_At,
        @ByType = 'user', @ById = 'u10', @Text = N'Created by H. Doyle', @Html = NULL;
    DECLARE @ee110_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e110', @TicketId = 'T060021', @Kind = 'Event', @At = @ee110_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee111_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e111', @TicketId = 'T060021', @Kind = 'Event', @At = @ee111_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to R. Singh', @Html = NULL;
    DECLARE @ee112_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e112', @TicketId = 'T060021', @Kind = 'Reply', @At = @ee112_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Hi Hannah, I can reproduce this on your profile. I''ve raised it with our Salesforce partner and will update you by tomorrow.</p>';
    DECLARE @ee113_At DATETIMEOFFSET = DATEADD(MINUTE, 126, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e113', @TicketId = 'T060021', @Kind = 'Event', @At = @ee113_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by R. Singh', @Html = NULL;
    DECLARE @ee114_At DATETIMEOFFSET = DATEADD(MINUTE, 1200, @t20_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e114', @TicketId = 'T060021', @Kind = 'Note', @At = @ee114_At,
        @ByType = 'agent', @ById = 'a2', @Text = NULL, @Html = N'<p>Partner case 00981234 opened. Probably a permission set change after the winter release.</p>';
    INSERT INTO dbo.TicketViewers (TicketId, AgentId) VALUES
        ('T060021', 'a3');

    -- Ticket 22/30: T060022 (vpn) "VPN client missing on replacement laptop"
    DECLARE @t21_Created DATETIMEOFFSET = DATEADD(HOUR, -46, SYSDATETIMEOFFSET());
    DECLARE @t21_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"vpnDevice":"TCB-LT-0156","reason":"My replacement laptop has no VPN client and I need the CRM file share on the road.","dateFrom":"@@DAY4@@"}';
    SET @t21_FieldsJson = REPLACE(@t21_FieldsJson, '@@DAY4@@', FORMAT(DATEADD(DAY, -1, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060022', @Number = '060022', @Subject = N'VPN client missing on replacement laptop', @Topic = 'vpn', @Department = N'Infrastructure',
        @Status = 'InProgress', @Priority = 'Normal', @RequesterUserId = 'u9', @AffectedUserId = 'u9',
        @CreatedByType = 'user', @CreatedById = 'u9', @Created = @t21_Created, @Updated = @t21_Created,
        @AssignedAgentId = 'a5', @AssignedTeamId = 't2',
        @FieldsJson = @t21_FieldsJson;
    DECLARE @ee115_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t21_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e115', @TicketId = 'T060022', @Kind = 'Event', @At = @ee115_At,
        @ByType = 'user', @ById = 'u9', @Text = N'Created by E. Clarke', @Html = NULL;
    DECLARE @ee116_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t21_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e116', @TicketId = 'T060022', @Kind = 'Event', @At = @ee116_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;
    DECLARE @ee117_At DATETIMEOFFSET = DATEADD(MINUTE, 60, @t21_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e117', @TicketId = 'T060022', @Kind = 'Event', @At = @ee117_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to N. Hussain', @Html = NULL;
    DECLARE @ee118_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t21_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e118', @TicketId = 'T060022', @Kind = 'Reply', @At = @ee118_At,
        @ByType = 'agent', @ById = 'a5', @Text = NULL, @Html = N'<p>Hi Ethan, I''ve pushed the VPN client to your laptop. Please restart it while connected to the internet and let me know when that''s done.</p>';
    DECLARE @ee119_At DATETIMEOFFSET = DATEADD(MINUTE, 186, @t21_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e119', @TicketId = 'T060022', @Kind = 'Event', @At = @ee119_At,
        @ByType = 'system', @ById = NULL, @Text = N'Status changed from Open to In Progress by N. Hussain', @Html = NULL;

    -- Ticket 23/30: T060023 (general) "Access to the shared Contracts mailbox"
    DECLARE @t22_Created DATETIMEOFFSET = DATEADD(HOUR, -30, SYSDATETIMEOFFSET());
    DECLARE @t22_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"details":"I need access to the shared Contracts mailbox for the supplier renewals project. My line manager has approved it."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060023', @Number = '060023', @Subject = N'Access to the shared Contracts mailbox', @Topic = 'general', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u12', @AffectedUserId = 'u12',
        @CreatedByType = 'user', @CreatedById = 'u12', @Created = @t22_Created, @Updated = @t22_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't1',
        @FieldsJson = @t22_FieldsJson;
    DECLARE @ee120_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t22_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e120', @TicketId = 'T060023', @Kind = 'Event', @At = @ee120_At,
        @ByType = 'user', @ById = 'u12', @Text = N'Created by R. Adeyemi', @Html = NULL;
    DECLARE @ee121_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t22_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e121', @TicketId = 'T060023', @Kind = 'Event', @At = @ee121_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;

    -- Ticket 24/30: T060024 (hardware) "Second monitor flickering"
    DECLARE @t23_Created DATETIMEOFFSET = DATEADD(HOUR, -26, SYSDATETIMEOFFSET());
    DECLARE @t23_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Monitor","device":"TCB-MN-0212","problem":"Display problem","started":"Yesterday","impact":"inconvenience","tried":["Checked cables and power"],"details":"The right-hand monitor flickers every few seconds and sometimes goes black for a second.","location":"desk","visitTime":"Mornings before 11am"}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060024', @Number = '060024', @Subject = N'Second monitor flickering', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u4', @AffectedUserId = 'u4',
        @CreatedByType = 'user', @CreatedById = 'u4', @Created = @t23_Created, @Updated = @t23_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t23_FieldsJson;
    DECLARE @ee122_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t23_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e122', @TicketId = 'T060024', @Kind = 'Event', @At = @ee122_At,
        @ByType = 'user', @ById = 'u4', @Text = N'Created by D. Okafor', @Html = NULL;
    DECLARE @ee123_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t23_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e123', @TicketId = 'T060024', @Kind = 'Event', @At = @ee123_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee124_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t23_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e124', @TicketId = 'T060024', @Kind = 'Event', @At = @ee124_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee125_At DATETIMEOFFSET = DATEADD(MINUTE, 192, @t23_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e125', @TicketId = 'T060024', @Kind = 'Reply', @At = @ee125_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Hi Daniel, I''ll bring a replacement DisplayPort cable up this afternoon. If that doesn''t fix it we''ll swap the monitor.</p>';

    -- Ticket 25/30: T060025 (leaver) "Leaver: Tom Hargreaves (retiring)"
    DECLARE @t24_Created DATETIMEOFFSET = DATEADD(HOUR, -24, SYSDATETIMEOFFSET());
    DECLARE @t24_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"leavingDate":"@@DAY7@@","lineManager":"u4","returnAssets":["TCB-DT-0031"],"mailbox":"Delegate access","mailboxTarget":"u4","details":"Tom retires on his last day. Please keep his desktop for whoever replaces him in the warehouse."}';
    SET @t24_FieldsJson = REPLACE(@t24_FieldsJson, '@@DAY7@@', FORMAT(DATEADD(DAY, 56, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060025', @Number = '060025', @Subject = N'Leaver: Tom Hargreaves (retiring)', @Topic = 'leaver', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u3', @AffectedUserId = 'u2',
        @CreatedByType = 'user', @CreatedById = 'u3', @Created = @t24_Created, @Updated = @t24_Created,
        @AssignedAgentId = 'a1', @AssignedTeamId = 't1',
        @FieldsJson = @t24_FieldsJson;
    DECLARE @ee126_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t24_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e126', @TicketId = 'T060025', @Kind = 'Event', @At = @ee126_At,
        @ByType = 'user', @ById = 'u3', @Text = N'Created by L. Brennan', @Html = NULL;
    DECLARE @ee127_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t24_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e127', @TicketId = 'T060025', @Kind = 'Event', @At = @ee127_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee128_At DATETIMEOFFSET = DATEADD(MINUTE, 180, @t24_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e128', @TicketId = 'T060025', @Kind = 'Event', @At = @ee128_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to J. Moore', @Html = NULL;
    DECLARE @ee129_At DATETIMEOFFSET = DATEADD(MINUTE, 240, @t24_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e129', @TicketId = 'T060025', @Kind = 'Reply', @At = @ee129_At,
        @ByType = 'agent', @ById = 'a1', @Text = NULL, @Html = N'<p>Thanks Leah, noted. I''ll schedule the account changes for Tom''s last day and leave the desktop in the warehouse.</p>';
    DECLARE @k9_Due DATE = CAST(DATEADD(DAY, 56, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k9', @TicketId = 'T060025', @Text = N'Disable account', @AssigneeAgentId = 'a1',
        @Due = @k9_Due, @Done = 0;
    DECLARE @k10_Due DATE = CAST(DATEADD(DAY, 56, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k10', @TicketId = 'T060025', @Text = N'Recover laptop', @AssigneeAgentId = 'a1',
        @Due = @k10_Due, @Done = 0;
    DECLARE @k11_Due DATE = CAST(DATEADD(DAY, 57, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k11', @TicketId = 'T060025', @Text = N'Remove licences', @AssigneeAgentId = 'a5',
        @Due = @k11_Due, @Done = 0;
    DECLARE @k12_Due DATE = CAST(DATEADD(DAY, 57, SYSDATETIMEOFFSET()) AS DATE);
    EXEC dbo.usp_TicketTask_Insert
        @Id = 'k12', @TicketId = 'T060025', @Text = N'Archive mailbox', @AssigneeAgentId = 'a5',
        @Due = @k12_Due, @Done = 0;

    -- Ticket 26/30: T060026 (newuser) "New starter: Account Manager, Manchester sales"
    DECLARE @t25_Created DATETIMEOFFSET = DATEADD(HOUR, -20, SYSDATETIMEOFFSET());
    DECLARE @t25_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"newName":"Jordan Fairclough","startDate":"@@DAY2@@","jobTitle":"Account Manager","department":"Sales","lineManager":"u5","software":["Microsoft 365","Salesforce","Teams Phone"],"deviceNeeded":"Standard laptop","details":"Jordan will sit at desk S-24 in the Manchester office."}';
    SET @t25_FieldsJson = REPLACE(@t25_FieldsJson, '@@DAY2@@', FORMAT(DATEADD(DAY, 14, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060026', @Number = '060026', @Subject = N'New starter: Account Manager, Manchester sales', @Topic = 'newuser', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u5', @AffectedUserId = 'u5',
        @CreatedByType = 'user', @CreatedById = 'u5', @Created = @t25_Created, @Updated = @t25_Created,
        @AssignedAgentId = 'a3', @AssignedTeamId = 't1',
        @FieldsJson = @t25_FieldsJson;
    DECLARE @ee130_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t25_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e130', @TicketId = 'T060026', @Kind = 'Event', @At = @ee130_At,
        @ByType = 'user', @ById = 'u5', @Text = N'Created by S. Lumb', @Html = NULL;
    DECLARE @ee131_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t25_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e131', @TicketId = 'T060026', @Kind = 'Event', @At = @ee131_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee132_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t25_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e132', @TicketId = 'T060026', @Kind = 'Event', @At = @ee132_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to E. Shaw', @Html = NULL;
    DECLARE @ee133_At DATETIMEOFFSET = DATEADD(MINUTE, 150, @t25_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e133', @TicketId = 'T060026', @Kind = 'Note', @At = @ee133_At,
        @ByType = 'agent', @ById = 'a3', @Text = NULL, @Html = N'<p>Two Latitude 5440s left in the Horwich store. Reserving one for this starter.</p>';

    -- Ticket 27/30: T060027 (software) "Outlook keeps asking for my password"
    DECLARE @t26_Created DATETIMEOFFSET = DATEADD(HOUR, -11, SYSDATETIMEOFFSET());
    DECLARE @t26_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"appName":"Microsoft Outlook","version":"Microsoft 365, version 2409","errorMsg":"\"Need password\" banner every few minutes","impact":"difficulty","details":"Since yesterday afternoon Outlook prompts for my password. It works for 5 to 10 minutes after I enter it, then asks again. Teams is fine."}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060027', @Number = '060027', @Subject = N'Outlook keeps asking for my password', @Topic = 'software', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'High', @RequesterUserId = 'u6', @AffectedUserId = 'u6',
        @CreatedByType = 'user', @CreatedById = 'u6', @Created = @t26_Created, @Updated = @t26_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't1',
        @FieldsJson = @t26_FieldsJson;
    DECLARE @ee134_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t26_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e134', @TicketId = 'T060027', @Kind = 'Event', @At = @ee134_At,
        @ByType = 'user', @ById = 'u6', @Text = N'Created by M. Webb', @Html = NULL;
    DECLARE @ee135_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t26_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e135', @TicketId = 'T060027', @Kind = 'Event', @At = @ee135_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee136_At DATETIMEOFFSET = DATEADD(MINUTE, 480, @t26_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e136', @TicketId = 'T060027', @Kind = 'Reply', @At = @ee136_At,
        @ByType = 'user', @ById = 'u6', @Text = NULL, @Html = N'<p>It''s now happening on my phone as well.</p>';

    -- Ticket 28/30: T060028 (hardware) "Laptop won't power on after the weekend"
    DECLARE @t27_Created DATETIMEOFFSET = DATEADD(HOUR, -6, SYSDATETIMEOFFSET());
    DECLARE @t27_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Laptop","device":"TCB-LT-0142","problem":"Won''t turn on","started":"Today","impact":"cant","tried":["Restarted the device","Checked cables and power"],"details":"I pressed the power button this morning and nothing happens: no lights and no fan. It was on the dock all weekend. I need to post month-end journals today.","location":"desk","visitTime":"Any time today, I am at my desk until 5pm"}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060028', @Number = '060028', @Subject = N'Laptop won''t power on after the weekend', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'Urgent', @RequesterUserId = 'u1', @AffectedUserId = 'u1',
        @CreatedByType = 'user', @CreatedById = 'u1', @Created = @t27_Created, @Updated = @t27_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't1',
        @FieldsJson = @t27_FieldsJson;
    DECLARE @ee137_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t27_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e137', @TicketId = 'T060028', @Kind = 'Event', @At = @ee137_At,
        @ByType = 'user', @ById = 'u1', @Text = N'Created by A. Patel', @Html = NULL;
    DECLARE @ee138_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t27_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e138', @TicketId = 'T060028', @Kind = 'Event', @At = @ee138_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;
    DECLARE @ee139_At DATETIMEOFFSET = DATEADD(MINUTE, 120, @t27_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e139', @TicketId = 'T060028', @Kind = 'Reply', @At = @ee139_At,
        @ByType = 'user', @ById = 'u1', @Text = NULL, @Html = N'<p>Still nothing. I''ve borrowed a colleague''s PC but I can''t get to my files.</p>';
    INSERT INTO dbo.TicketViewers (TicketId, AgentId) VALUES
        ('T060028', 'a2');

    -- Ticket 29/30: T060029 (server) "More memory on the test app server for load testing"
    DECLARE @t28_Created DATETIMEOFFSET = DATEADD(HOUR, -5, SYSDATETIMEOFFSET());
    DECLARE @t28_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"environment":"Test","requestType":"Increase CPU or memory","serverName":"TCB-TST-APP02","justification":"Load testing for the customer portal release needs the test app server raised from 8 GB to 16 GB RAM for two weeks.","neededBy":"@@DAY1@@"}';
    SET @t28_FieldsJson = REPLACE(@t28_FieldsJson, '@@DAY1@@', FORMAT(DATEADD(DAY, 5, SYSDATETIMEOFFSET()), 'yyyy-MM-dd'));
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060029', @Number = '060029', @Subject = N'More memory on the test app server for load testing', @Topic = 'server', @Department = N'Infrastructure',
        @Status = 'Open', @Priority = 'Normal', @RequesterUserId = 'u11', @AffectedUserId = 'u11',
        @CreatedByType = 'user', @CreatedById = 'u11', @Created = @t28_Created, @Updated = @t28_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't2',
        @FieldsJson = @t28_FieldsJson;
    DECLARE @ee140_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t28_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e140', @TicketId = 'T060029', @Kind = 'Event', @At = @ee140_At,
        @ByType = 'user', @ById = 'u11', @Text = N'Created by O. Pryce', @Html = NULL;
    DECLARE @ee141_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t28_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e141', @TicketId = 'T060029', @Kind = 'Event', @At = @ee141_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Infrastructure', @Html = NULL;

    -- Ticket 30/30: T060030 (hardware) "Headset microphone cutting out on calls"
    DECLARE @t29_Created DATETIMEOFFSET = DATEADD(HOUR, -2, SYSDATETIMEOFFSET());
    DECLARE @t29_FieldsJson NVARCHAR(MAX) = N'{"attachments":[],"deviceType":"Headset/Phone","device":"TCB-HS-0054","problem":"Audio or call problem","started":"This week","impact":"difficulty","tried":["Restarted the device","Tried a different port or dock"],"details":"Customers say my voice cuts out mid-sentence on Teams calls. The mute light flickers on its own.","location":"desk","visitTime":""}';
    EXEC dbo.usp_Ticket_Create
        @Id = 'T060030', @Number = '060030', @Subject = N'Headset microphone cutting out on calls', @Topic = 'hardware', @Department = N'IT Business Support',
        @Status = 'Open', @Priority = 'High', @RequesterUserId = 'u8', @AffectedUserId = 'u8',
        @CreatedByType = 'user', @CreatedById = 'u8', @Created = @t29_Created, @Updated = @t29_Created,
        @AssignedAgentId = NULL, @AssignedTeamId = 't1',
        @FieldsJson = @t29_FieldsJson;
    DECLARE @ee142_At DATETIMEOFFSET = DATEADD(MINUTE, 0, @t29_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e142', @TicketId = 'T060030', @Kind = 'Event', @At = @ee142_At,
        @ByType = 'user', @ById = 'u8', @Text = N'Created by G. Whitfield', @Html = NULL;
    DECLARE @ee143_At DATETIMEOFFSET = DATEADD(MINUTE, 6, @t29_Created);
    EXEC dbo.usp_TicketTimelineEntry_Insert
        @Id = 'e143', @TicketId = 'T060030', @Kind = 'Event', @At = @ee143_At,
        @ByType = 'system', @ById = NULL, @Text = N'Assigned to Service Desk', @Html = NULL;

END
GO

-- Realign dbo.TicketNumberSeq (created at 60001 in 04_Schema_Extensions.sql) so the next
-- ticket created through the app doesn't collide with the Number values just seeded above.
-- Computed from MAX(Number)+1 rather than a hardcoded literal so this stays correct even if
-- the seeded ticket count above changes later. Safe to re-run: a no-op once already aligned.
DECLARE @nextTicketNumber INT = (SELECT ISNULL(MAX(CAST(Number AS INT)), 60000) + 1 FROM dbo.Tickets);
DECLARE @currentSeqValue INT = (SELECT CAST(current_value AS INT) FROM sys.sequences WHERE name = N'TicketNumberSeq' AND schema_id = SCHEMA_ID('dbo'));
IF @currentSeqValue < @nextTicketNumber
BEGIN
    DECLARE @sql NVARCHAR(200) = N'ALTER SEQUENCE dbo.TicketNumberSeq RESTART WITH ' + CAST(@nextTicketNumber AS NVARCHAR(20)) + N';';
    EXEC (@sql);
END
GO

-- Development passwords for the seeded accounts, stored as the same PBKDF2 hashes the app
-- writes (AccountPasswordService): users TopCashback1!, Administrator agents AdminDemo1!, other
-- agents ServiceDesk1! (shown on the development sign-in page). Every account needs a real hash:
-- an account with no hash cannot sign in. Only for development databases -- these are well-known
-- passwords. Re-runnable: an account whose password has since been changed keeps it.
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$IFwOWXNse+Dcl/QfTuSA7Q==$pxFIjbbbW7ERTQBtYcLOHm+gHpjpVCtxBEAu7Ci+5bY=' WHERE Id = N'u1' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$r53mEA9NSlNWWUDfGSBYWw==$6F+G3uCrEWpLzvkO9ZkYe+Yc4ixuXtcdLERtbB6GVgU=' WHERE Id = N'u2' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$Z79WhAULBJ3uN6vUp2kr8w==$YWLJHbdKsS8dZDi5O4/riFHIe1jXASItVY4fjXkYWgU=' WHERE Id = N'u3' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$NnufY6ZlYEAgFodfkJk0MA==$xKIZbdxWHnm41xjgoJ6rqKQtqa2tfjm5uDhd+UcD5H8=' WHERE Id = N'u4' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$Hca5x+2NOhT2J/5T6BTyfA==$0vcFt5OPUuFxOcnlGWp+eyj/LJrACjgZvXrufjV8ZmY=' WHERE Id = N'u5' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$FETsj0yJH6R5nzKNPfqWFg==$r33r4nzcjiJWITY/Br4OALwis2XYEtL/A2IVDgvRmp8=' WHERE Id = N'u6' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$ddb16baMGmgWIBGH3VIywA==$i1+up06jgX8tSGOGZQee5iwpG8YRdUkXwreImL+KzP8=' WHERE Id = N'u7' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$AqoT/RwS8ceYPYwbsYCE7A==$DWB8X/tFKwoci1bxMil8H/Y8or10FqlcGmk6BSalDZw=' WHERE Id = N'u8' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$A/iyN92hwGkkwIJfZVUTiQ==$6ChfXIMGmWAvySviEU9U3L94QCp6TJ8qD+s3do0daZQ=' WHERE Id = N'u9' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$YOkRO8dTNqnWGuI5ywUHkw==$GBTuvL0xTV/8k49ZdD2appG5ZR4lpTlRmOJhydwTl+E=' WHERE Id = N'u10' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$+tkuBAkPKRkckjEUppl4aQ==$3U8QhBRTsH+/UlSjO12DHh/raMUWOFB5hnaxJYazSX8=' WHERE Id = N'u11' AND PasswordHash IS NULL;
UPDATE dbo.Users SET PasswordHash = N'pbkdf2-sha256$600000$D6aqXjyp+GDaIiztSP2Gqg==$+lXKAfeWkDylmbUI01fd90KYCkAsdBeFNjMsmhbCdGE=' WHERE Id = N'u12' AND PasswordHash IS NULL;
UPDATE dbo.Agents SET PasswordHash = N'pbkdf2-sha256$600000$9Qy318Uj8faIKP17fRdxqA==$DGK4Ls6K4eTvJhUqOeZzV/BtBKS+nsruaAV4fQdbbUY=' WHERE Id = N'a1' AND PasswordHash IS NULL;
UPDATE dbo.Agents SET PasswordHash = N'pbkdf2-sha256$600000$+U3+o8sXE5BSkmVUV6jKuw==$zquisU3ucNvsbpRsqQFHhCRpX6tSOU3eM5UTRsV5pSI=' WHERE Id = N'a2' AND PasswordHash IS NULL;
UPDATE dbo.Agents SET PasswordHash = N'pbkdf2-sha256$600000$2T5xVsY/RDJn5qMwJRKiyg==$/MathyfpAWXrvlDgRehvlvMUWZEysmDP0Eq+bRoAzhQ=' WHERE Id = N'a3' AND PasswordHash IS NULL;
UPDATE dbo.Agents SET PasswordHash = N'pbkdf2-sha256$600000$4lkz/W5KsDO6dA9jSXCbsQ==$Z0nTI6DV25dOo8EDAJZnIbA4JVLFstLgi1rLdF3RkWM=' WHERE Id = N'a4' AND PasswordHash IS NULL;
UPDATE dbo.Agents SET PasswordHash = N'pbkdf2-sha256$600000$QscX61wGcgGCPPbRokwBHQ==$3lHC4zIJeppo58q9sbM2q+4nZkrqalEuLOiSgkvywRQ=' WHERE Id = N'a5' AND PasswordHash IS NULL;
GO
