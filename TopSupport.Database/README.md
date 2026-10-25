# TopSupport.Database

Hand-written SQL Server schema and stored procedures for the TopSupport PoC. There is no EF Core
and no migration tooling here by design — the API's data-access layer (`TopSupport.Api/Data`) is
raw ADO.NET calling the stored procedures defined here.

## Running the scripts

Run the scripts in order against the dev SQL Server instance with `sqlcmd`. The PoC targets the
default local instance (`.`) with Windows integrated auth, matching the connection string in
`TopSupport.Api/appsettings.Development.json`:

```sh
sqlcmd -S . -E -f 65001 -i Scripts/00_CreateDatabase.sql
sqlcmd -S . -E -f 65001 -i Scripts/01_Schema_Identity.sql
sqlcmd -S . -E -f 65001 -i Scripts/02_Schema_Core.sql
sqlcmd -S . -E -f 65001 -i Scripts/03_StoredProcedures_Core.sql
sqlcmd -S . -E -f 65001 -i Scripts/04_Schema_Extensions.sql
sqlcmd -S . -E -f 65001 -i Scripts/05_StoredProcedures_Extensions.sql
sqlcmd -S . -E -f 65001 -i Scripts/06_Admin_Audit.sql
sqlcmd -S . -E -f 65001 -i Scripts/07_Admin_Accounts.sql
sqlcmd -S . -E -f 65001 -i Scripts/08_Admin_UsersOrgs.sql
sqlcmd -S . -E -f 65001 -i Scripts/09_Admin_AgentsRoles.sql
sqlcmd -S . -E -f 65001 -i Scripts/10_Admin_SecuritySiteProfile.sql
sqlcmd -S . -E -f 65001 -i Scripts/11_Admin_KnowledgeBase.sql
sqlcmd -S . -E -f 65001 -i Scripts/12_Admin_Dashboard.sql
sqlcmd -S . -E -f 65001 -i Scripts/13_Auth_SignIn.sql
sqlcmd -S . -E -f 65001 -i Scripts/14_Attachments.sql
sqlcmd -S . -E -f 65001 -i Scripts/15_Worker.sql
sqlcmd -S . -E -f 65001 -i Scripts/16_Dev_TestAccounts.sql
sqlcmd -S . -E -f 65001 -i Scripts/99_SeedData.sql
```

**Always pass `-f 65001`** (input codepage = UTF-8). These scripts are saved as UTF-8 and contain
non-ASCII characters (`·`, smart quotes, etc. in seed data strings like "Horwich · Unit 4, desk
F-12"). Without `-f 65001`, `sqlcmd` reads the file using the console's codepage instead, which
silently mangles any UTF-8 multi-byte sequence into two garbled Windows-1252 characters (e.g. `·`
becomes `Â·`) — the data loads without any error or warning, so this is easy to miss. If you ever
see a stray `Â` in seeded text, the database was built without `-f 65001` and needs rebuilding.

(`-E` uses the current Windows login, i.e. integrated security. Swap `-S .` for
`"(localdb)\MSSQLLocalDB"` to run against LocalDB instead, if you don't have a full local SQL
Server instance installed.)

Every script is written to be safely re-run (`IF OBJECT_ID(...) IS NULL`, `CREATE OR ALTER`,
`IF NOT EXISTS` guards on seed inserts).

## Script order

| Script | Purpose |
| --- | --- |
| `00_CreateDatabase.sql` | Creates the `TopSupport` database. |
| `01_Schema_Identity.sql` | ASP.NET Core Identity tables (see note below). |
| `02_Schema_Core.sql` | Core domain tables: Orgs, Users, Agents, Teams, Assets, Tickets, TicketTimelineEntries, TicketTasks, TicketViewers. |
| `03_StoredProcedures_Core.sql` | First representative stored procs (ticket get/insert) proving the ADO.NET repository pattern. |
| `04_Schema_Extensions.sql` | `TicketAttachments` (VARBINARY(MAX) file storage), `CannedResponses`, and the `TicketNumberSeq` sequence (ticket numbering, replacing the original demo's in-memory counter). |
| `05_StoredProcedures_Extensions.sql` | The full ticket-lifecycle stored proc set: status/priority/topic/assign updates, timeline/task/attachment/canned-response CRUD, queue search, and User/Agent/Team/Asset lookups. |
| `06_Admin_Audit.sql` | `AuditEntries` table, insert/read procs and the demo's 16 seed audit entries (audit trail page; every admin change writes here). |
| `07_Admin_Accounts.sql` | `PasswordHash` columns on `Users`/`Agents` (PBKDF2; NULL = buildSeed() demo password) and the get/set password procs. |
| `08_Admin_UsersOrgs.sql` | `Users.Deleted` soft-delete flag plus user and organisation admin procs (list, create/edit, lock, delete). |
| `09_Admin_AgentsRoles.sql` | `RolePermissions` table (seeded from buildSeed()'s `perms`), `Created` ordering columns on `Agents`/`Teams`, and agent/team admin procs. |
| `10_Admin_SecuritySiteProfile.sql` | `AppSettings` key/value table (security password rules and site page text) plus My Profile procs. |
| `11_Admin_KnowledgeBase.sql` | `KbCategories`/`KbArticles` tables, KB procs and the demo's 4 categories and 8 articles. |
| `12_Admin_Dashboard.sql` | Proc that derives each ticket's closed-at time for the dashboard charts. |
| `13_Auth_SignIn.sql` | Real sign-in: `FailedSignIns` on `Users`/`Agents` (lockout after the security setting `lockoutAttempts`), the username lookup proc, and failed-sign-in procs. Redefines `usp_Account_SetPasswordHash` and `usp_User_SetLocked` so an admin reset or unlock also clears the failed count, so run it after `07_` and `08_`. |
| `14_Attachments.sql` | `usp_TicketAttachment_GetTicketId`: the ticket an attachment belongs to, so the download endpoint checks end-user visibility against that one ticket instead of loading all of the caller's tickets. |
| `15_Worker.sql` | The `worker` schema for TopSupport.Worker (the Hangfire job runner): `NotificationLog` (unique on ticket, event key and recipient, so no email is sent twice), `JobCursor`, and the read procs each job uses, plus `usp_AutoClose_CloseTicket`. Hangfire creates its own tables in the `jobs` schema. See `TopSupport.Worker/README.md`. |
| `16_Dev_TestAccounts.sql` | `usp_Account_ListForTesting`: the accounts the development-only test-account switcher lists. The Api only exposes it in Development with `Auth:TestAccountSwitcher:Enabled` on. |
| `99_SeedData.sql` | Full reference data ported from `buildSeed()` in the original `topsupport-app.js` demo: 12 users, 5 agents, 2 teams, 15 assets, 5 canned responses, and all 30 seed tickets with their complete timelines and leaver-checklist tasks. Also realigns `TicketNumberSeq` past the highest seeded ticket number so real ticket creation never collides with seed data. |

## Sign-in and the unused Identity tables

Sign-in does not use ASP.NET Core Identity. `POST api/auth/sign-in` (AuthController →
AuthService) looks the username up with `usp_Account_GetByUsername`, verifies the password against
the PBKDF2 `PasswordHash` from `07_Admin_Accounts.sql` (a NULL hash still means the buildSeed()
demo password), applies the lockout, and returns a signed bearer token (JWT, HMAC-SHA256) carrying
the account id, name and role. Every other Api endpoint requires that token, and the admin and
agent endpoints require the role. The Blazor app keeps the token in its encrypted, HttpOnly auth
cookie and sends it on every Api call.

The token signing key is the Api's `Auth:SigningKey` setting. `appsettings.Development.json`
has a development-only key; any other environment must supply its own (at least 32 bytes) through
user secrets, an environment variable (`Auth__SigningKey`) or a key vault, or the Api won't start.

`01_Schema_Identity.sql` still creates the hand-rolled `AspNetUsers`/`AspNetRoles` tables from the
first skeleton pass, but nothing uses them any more.

## Ticket `FieldsJson` column

`dbo.Tickets.FieldsJson` stores each ticket's per-topic dynamic form answers as a JSON object
(validated with `ISJSON`), rather than fixed columns. This mirrors the `fields` object on every
ticket in the original demo, where `topicFields`/`renderTopicField` render a different set of
fields per `TOPICS` entry (hardware vs. leaver vs. VPN, etc.) — a fixed-column schema would need a
migration every time a topic's fields changed.
