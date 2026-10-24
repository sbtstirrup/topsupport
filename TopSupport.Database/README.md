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
| `99_SeedData.sql` | Full reference data ported from `buildSeed()` in the original `topsupport-app.js` demo: 12 users, 5 agents, 2 teams, 15 assets, 5 canned responses, and all 30 seed tickets with their complete timelines and leaver-checklist tasks. Also realigns `TicketNumberSeq` past the highest seeded ticket number so real ticket creation never collides with seed data. |

## Why hand-rolled Identity tables, and what's still missing

ASP.NET Core Identity normally persists through EF Core's `UserStore`/`RoleStore`, which expect an
EF `DbContext`. This PoC uses ADO.NET + stored procedures exclusively, so `01_Schema_Identity.sql`
reproduces Identity's default table shape (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.)
by hand, with two TopSupport-specific columns added to `AspNetUsers`:

- `AppRole` — which portal (`User` / `Agent` / `Admin`) the account signs into, matching `S.role`
  in the original demo.
- `DomainPersonId` — the `Users.Id` or `Agents.Id` row this login represents.

**This skeleton pass does not yet implement the custom store.** `TopSupport.Api/Identity` contains
stub `IUserStore<ApplicationUser>` / `IRoleStore<ApplicationRole>` implementations that throw
`NotImplementedException` on anything beyond construction. Implementing them (backed by ADO.NET
calls against these tables, with stored procedures alongside `03_StoredProcedures_Core.sql`) is
the next real task before sign-in works end to end.

## Ticket `FieldsJson` column

`dbo.Tickets.FieldsJson` stores each ticket's per-topic dynamic form answers as a JSON object
(validated with `ISJSON`), rather than fixed columns. This mirrors the `fields` object on every
ticket in the original demo, where `topicFields`/`renderTopicField` render a different set of
fields per `TOPICS` entry (hardware vs. leaver vs. VPN, etc.) — a fixed-column schema would need a
migration every time a topic's fields changed.
