/*
    01_Schema_Identity.sql
    Hand-written equivalent of ASP.NET Core Identity's default EF Core schema.

    Why hand-written: this PoC uses raw ADO.NET + stored procedures end to end (no EF Core),
    so Identity cannot use its usual EF-backed UserStore/RoleStore out of the box. These
    tables reproduce Identity's default column shape so a custom ADO.NET-backed
    IUserStore<ApplicationUser>/IRoleStore<ApplicationRole> (TopSupport.Api/Identity/
    AdoUserStore.cs, AdoRoleStore.cs) can be implemented against them. That custom store is
    NOT implemented yet in this skeleton pass -- see TopSupport.Database/README.md.
*/
USE TopSupport;
GO

-- Required by the filtered indexes below (e.g. "WHERE NormalizedName IS NOT NULL"); sqlcmd's
-- default session doesn't set this the way SSMS does.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id              NVARCHAR(450)   NOT NULL PRIMARY KEY,
        Name            NVARCHAR(256)   NULL,
        NormalizedName  NVARCHAR(256)   NULL,
        ConcurrencyStamp NVARCHAR(MAX)  NULL
    );

    CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;

    CREATE TABLE dbo.AspNetUsers
    (
        Id                      NVARCHAR(450)   NOT NULL PRIMARY KEY,
        UserName                NVARCHAR(256)   NULL,
        NormalizedUserName      NVARCHAR(256)   NULL,
        Email                   NVARCHAR(256)   NULL,
        NormalizedEmail         NVARCHAR(256)   NULL,
        EmailConfirmed          BIT             NOT NULL DEFAULT 0,
        PasswordHash            NVARCHAR(MAX)   NULL,
        SecurityStamp           NVARCHAR(MAX)   NULL,
        ConcurrencyStamp        NVARCHAR(MAX)   NULL,
        PhoneNumber             NVARCHAR(MAX)   NULL,
        PhoneNumberConfirmed    BIT             NOT NULL DEFAULT 0,
        TwoFactorEnabled        BIT             NOT NULL DEFAULT 0,
        LockoutEnd              DATETIMEOFFSET  NULL,
        LockoutEnabled          BIT             NOT NULL DEFAULT 0,
        AccessFailedCount       INT             NOT NULL DEFAULT 0,

        -- TopSupport-specific: which domain record (Users.Id or Agents.Id) this login maps
        -- to, and which portal it should land in. Not part of stock Identity.
        AppRole                 NVARCHAR(20)    NOT NULL,   -- 'User' | 'Agent' | 'Admin'
        DomainPersonId          NVARCHAR(450)   NOT NULL    -- FK into dbo.Users.Id or dbo.Agents.Id depending on AppRole
    );

    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX IX_AspNetUsers_NormalizedEmail ON dbo.AspNetUsers(NormalizedEmail);

    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId  NVARCHAR(450) NOT NULL,
        RoleId  NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );

    CREATE TABLE dbo.AspNetUserClaims
    (
        Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId      NVARCHAR(450)   NOT NULL,
        ClaimType   NVARCHAR(MAX)   NULL,
        ClaimValue  NVARCHAR(MAX)   NULL,
        CONSTRAINT FK_AspNetUserClaims_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );

    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider       NVARCHAR(450)   NOT NULL,
        ProviderKey         NVARCHAR(450)   NOT NULL,
        ProviderDisplayName NVARCHAR(MAX)   NULL,
        UserId              NVARCHAR(450)   NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );

    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId          NVARCHAR(450) NOT NULL,
        LoginProvider   NVARCHAR(450) NOT NULL,
        Name            NVARCHAR(450) NOT NULL,
        Value           NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );

    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId      NVARCHAR(450)   NOT NULL,
        ClaimType   NVARCHAR(MAX)   NULL,
        ClaimValue  NVARCHAR(MAX)   NULL,
        CONSTRAINT FK_AspNetRoleClaims_Roles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO
