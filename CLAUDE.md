# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

TopSupport is a self-contained, client-only IT helpdesk demo app (fictional company "TopCashback"). There is no
backend, build step, package manager, or test suite — it's a single HTML entry point plus two hand-written JS files
and a CSS file, all loaded directly by the browser. There are no commands to build, lint, or test; to "run" it, open
`uploads/topsupport.html` in a browser (or serve the directory with any static file server so the relative
`./support.js`, `./topsupport.css`, `./topsupport-app.js` references resolve).

The `_ds/` directory is an unrelated, unused design-system scaffold (a generic red/mono "Modernist" theme sample)
left over from tooling — it has no relationship to the actual green TopSupport UI and isn't wired into anything.
Ignore it unless specifically asked about it.

## Files that matter

- **`uploads/topsupport.html`** — the real HTML entry point. Loads `support.js`, then `topsupport.css`, and defines
  an inline `<x-dc>` / `data-dc-script` block whose `Component extends DCLogic` kicks off loading
  `topsupport-app.js` on mount.
- **`TopSupport.dc.html`** — a second, near-identical `.dc.html` entry point (same pattern, trimmed down).
- **`support.js`** — ⚠️ **generated, do not hand-edit.** Header says `GENERATED from dc-runtime/src/*.ts — do not
  edit. Rebuild with \`cd dc-runtime && bun run build\`.` It's a bundled runtime that: loads React/ReactDOM UMD,
  parses `<x-dc>` documents/templates, and boots whatever class extends `DCLogic` (aliased from
  `StreamableLogic`). The `dc-runtime` source referenced by the header is not present in this repo — treat
  `support.js` as a vendored black box.
- **`topsupport-app.js`** — the actual application. All real logic lives here (~2000 lines, no modules/bundler,
  plain `'use strict'` script evaluated in the global scope).
- **`topsupport.css`** — all styling for the app (green TopSupport theme, not the `_ds` Modernist theme).

## Architecture of `topsupport-app.js`

This is a classic **immediate-mode, single-global-state** vanilla JS SPA — no framework, no virtual DOM diffing of
its own (React is loaded by the runtime but the app itself does plain string-templated HTML, not JSX/createElement).
Every state change re-renders by replacing `#root`'s `innerHTML` wholesale. Key pieces, in the order they appear:

1. **Fixed config & seed data** (top of file): `DEPTS`, `STATUSES`, `PRIORITIES`, `SLA_HOURS`, `TOPICS`,
   `buildSeed(nowMs)` — generates all fictional orgs/users/agents/teams/assets/tickets fresh on first load.
2. **Persistence**: `DB` is the whole in-memory database (seeded by `buildSeed`), persisted to `localStorage` under
   `tcb-supportdesk-data-v1` via `loadDB()`/`saveDB()`. Session (`S.acct`, `S.role`, `S.page`, `S.params`) is
   separately persisted under `tcb-supportdesk-session-v1`. The `store` object wraps `localStorage` with try/catch
   so the demo still works if storage is unavailable.
3. **Global UI state**: the single `S` object (`acct`, `role`, `page`, `params`, `forms`, `ui`, `modal`, `flash`,
   `menuOpen`, `acctMenu`, `toasts`) — this is the entire "application state."
4. **Domain helpers**: lookups (`user(id)`, `agent(id)`, `team(id)`, `org(id)`, `asset(tag)`, `ticket(id)`,
   `topic(id)`), formatting (`fmtDate`, `fmtDT`, `fmtDur`, `fmtRel`, `fmtSize`), SLA logic (`slaDue`, `isOverdue`,
   `isAnswered`, `firstResponse`), and ticket mutation (`createTicket`, `changeStatus`, `assignTicket`, `addEntry`/
   `addEvent` which append to a ticket's `timeline` array).
5. **Reusable form-field renderers**: `fInput`, `fArea`, `fSelect`, `fChecks`, `fRadios`, `optPills`, `combo`
   (searchable person picker), `dropzone` (file upload), `rte` (contenteditable rich-text editor) — all read/write
   through `S.forms` via dotted-path helpers `getPath`/`setPath`/`fval`.
6. **Routing**: `PAGES` is a map of page-name → `{ render(params), init(params)?, after(params)?, roles?, public?,
   bare? }`. `go(page, params)` / `goHome()` switch `S.page`/`S.params` and call `render()`. `homeOf(role)` decides
   the landing page per role (`user` → `mytickets`, `agent` → `queue`, `admin` → `dashboard`).
7. **Actions**: `ACT` is a map of action-name → handler function, invoked by a single delegated
   `document.addEventListener('click', ...)` that looks for the nearest `[data-act]` ancestor and calls
   `ACT[el.dataset.act](el, e)`. Similarly, delegated `input`/`change`/`keydown`/`drop` listeners handle live form
   state, drag-drop file uploads, and the rich-text editor toolbar. **This is the central wiring**: to find what a
   button does, grep for its `data-act="..."` value in both the HTML template strings and the `ACT`/`MODALS` maps.
8. **Modals**: `MODALS` is a map of modal-name → `(data, modalState) => { title, body, ... }`, rendered by
   `openModal(type, data)` / `closeModal()` / `modalHTML()`.
9. **Render loop**: `render()` picks `PAGES[S.page]` (falling back to `landing`), enforces access via
   `allowed(pg)` (redirects to `signin` or the role's home page), renders into `#root` via `pg.render(params)`
   wrapped in `shell()` (authenticated chrome) or `pubShell()` (public pages), renders `#modal-root`, manages focus
   (`captureFocus`/`restoreFocus`, autofocus inside modals), then calls `pg.after(params)` and `saveSession()`.
   `boot()` is the entry point called on mount.
10. **Role model**: three roles — `user` (end-user portal: raise/view tickets, profile), `agent` (service desk:
    queue, ticket actions, canned responses, KB), `admin` (adds dashboard, users/orgs/agents/teams/roles, security,
    site settings, audit log). Page access is gated by `PAGES[name].roles` and checked in `allowed()`.

### Where logic is split across the two app files

`topsupport-app.js` is logically two halves concatenated into one file/global scope (no explicit module boundary,
just a comment divider around line 858: `/* End-user portal: landing, sign-in, profile, ticket forms, my tickets,
ticket view. */`). Everything before that point is shared plumbing (data layer, rendering helpers, routing engine,
event wiring); everything after defines the actual `PAGES.*`/`ACT.*`/`MODALS.*` entries for every screen (ticket
raise flow with per-topic dynamic fields in `topicFields`/`renderTopicField`, the agent queue, admin screens, etc.).
When adding a new screen or action, follow the existing patterns: add a `PAGES.foo = { render, init?, after?,
roles? }` entry, wire buttons with `data-act="..."` pointing at a new `ACT.foo = (el, e) => {...}` handler, and call
`render()` (directly, or implicitly via `go()`) after mutating `S` or `DB`.

## Conventions to preserve

- All HTML is built via template literals with manual escaping through `esc()` — always escape user-controllable
  strings inserted into templates the same way the surrounding code does.
- IDs for form fields are derived deterministically from their state path via `fid(path)`; don't hand-roll new ID
  schemes.
- Never hand-edit `support.js` — it's a generated/vendored runtime artifact.
