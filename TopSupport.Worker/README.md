# TopSupport.Worker

Background job runner for TopSupport: a .NET 10 worker hosting a Hangfire server, with Hangfire's
own tables in the `jobs` schema of the existing `TopSupport` database. It reads the existing tables
through its own stored procedures (`worker.*`, in `TopSupport.Database/Scripts/15_Worker.sql`) and
does not depend on the Api or the Blazor app. The same build runs as a console app, a Windows
service or a Linux systemd unit.

| Job | Schedule (Europe/London) | What it does |
| --- | --- | --- |
| `ticket-events` | every minute | Emails ticket created → requester, agent public reply → requester (never internal notes), assigned → the agent, resolved → requester. The first run only looks back an hour. |
| `sla-sweep` | every 5 minutes | Active ticket with no agent reply past its target (Urgent 4h, High 8h, Normal 24h, Low 72h) → the assigned agent, or every active team member if no agent. Once per ticket. |
| `auto-close` | daily 02:00 | Tickets Resolved for 7+ days → Closed, with the Api's "Closed by System with status Closed" timeline event and audit entry. |
| `daily-digest` | weekdays 08:00 | Each team lead gets their team's open tickets, overdue ones flagged. |
| `heartbeat` | every 5 minutes | Logs a line; publishes a CloudWatch `Heartbeat` metric when `CloudWatch:Enabled` is true. |

Every email is recorded in `worker.NotificationLog` (unique on ticket, event key and recipient)
*before* it's sent, so a retry or an overlapping run never sends it twice; a failed send removes the
row again so Hangfire's retry (3 attempts) sends it.

## Running locally

1. Apply the worker's SQL once (re-runnable), from `TopSupport.Database`:

   ```sh
   sqlcmd -S . -E -f 65001 -i Scripts/15_Worker.sql
   ```

2. Start smtp4dev (the local mail catcher):

   ```sh
   docker compose -f docker-compose.worker.yml up -d
   ```

   Without Docker: `dotnet tool install -g Rnwood.Smtp4dev` then `smtp4dev --smtpport=2525 --urls=http://localhost:5000`.

3. Run the worker (Development uses Windows auth to your local SQL Server, like the Api):

   ```sh
   dotnet run --project TopSupport.Worker
   ```

- **Dashboard:** http://localhost:5280/hangfire shows the five recurring jobs, runs, retries and failures. "Trigger now" runs a job immediately.
- **Emails:** http://localhost:5000 (smtp4dev). Raise a ticket in the Blazor app and the "We've received your ticket" email arrives within about a minute.
- **Secrets** (e.g. an SMTP password) go in user secrets, never in appsettings:
  `dotnet user-secrets set "Email:Smtp:Password" "..." --project TopSupport.Worker`

Development refuses `Email:Provider = Ses` and never publishes CloudWatch metrics, so nothing local
sends real email or calls AWS. Tests use fakes and a `FakeTimeProvider`: `dotnet test --project TopSupport.Worker.Tests`.

### Running the worker in Docker too

A Linux container can't use Windows authentication, so it needs a SQL login. Put the connection
string in a `.env` file next to `docker-compose.worker.yml` (git-ignored):

```
TOPSUPPORT_SQL_CONNECTION=Server=host.docker.internal,1433;Database=TopSupport;User Id=topsupport_worker;Password=...;TrustServerCertificate=True
```

then `docker compose -f docker-compose.worker.yml --profile container up -d --build`. The dashboard is
published on the host's `127.0.0.1:5280` only.

## Configuration

| Setting | Meaning |
| --- | --- |
| `ConnectionStrings:TopSupport` | The existing database (Hangfire adds the `jobs` schema; the worker's tables are in `worker`). |
| `Email:Provider` | `Smtp` (local) or `Ses` (AWS). |
| `Email:FromAddress`, `Email:FromName` | Sender. On SES the address must be a verified identity. |
| `Email:Smtp:Host`, `Port`, `UseStartTls`, `Username`, `Password` | SMTP settings (password in user secrets). |
| `App:BaseUrl` | The Blazor app's address, for the ticket links in every email. |
| `Dashboard:Port` | Dashboard port; it always listens on localhost only. |
| `CloudWatch:Enabled`, `CloudWatch:Namespace` | Heartbeat metric (off by default; never on in Development). |
| `Aws:Region` | Optional; otherwise the SDK's default region (the instance's region on EC2). |
| `Aws:ParameterStorePath` | Outside Development, settings are also read from SSM Parameter Store under this path. |
| `Jobs:*Cron`, `Jobs:TimeZone` | Schedules (cron) and their time zone. |
| `Jobs:FirstRunLookbackMinutes`, `Jobs:EventOverlapMinutes`, `Jobs:AutoCloseAfterDays` | Limits described above. |

AWS credentials always come from the SDK's default chain (the EC2 instance role); never put access
keys in configuration.

## Deploying to EC2

Publish once and copy the output to the instance (it runs unchanged on Windows or Linux):

```sh
dotnet publish TopSupport.Worker -c Release -o ./publish/worker
```

Store settings in Parameter Store under `/TopSupport/Worker` (use SecureString for secrets), for example:

```sh
aws ssm put-parameter --name /TopSupport/Worker/ConnectionStrings/TopSupport --type SecureString --value "Server=...;Database=TopSupport;User Id=...;Password=...;Encrypt=True"
aws ssm put-parameter --name /TopSupport/Worker/App/BaseUrl --type String --value "https://topsupport.example"
aws ssm put-parameter --name /TopSupport/Worker/CloudWatch/Enabled --type String --value "true"
```

The instance role needs `ssm:GetParametersByPath` (and `kms:Decrypt` for SecureStrings) on that
path, `ses:SendEmail`, and `cloudwatch:PutMetricData`. The database login needs read access to the
`dbo` tables, execute on the `worker` and `dbo` procs it uses, and (first start only) permission
to create the `jobs` schema, or apply Hangfire's schema in advance.

### As a Windows service

```powershell
sc.exe create TopSupport.Worker binPath= "C:\apps\topsupport-worker\TopSupport.Worker.exe" start= delayed-auto
sc.exe description TopSupport.Worker "TopSupport background jobs (Hangfire)"
sc.exe start TopSupport.Worker
```

Set `DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` to `Production` for the service (they default to
Production when unset).

### As a systemd unit

`/etc/systemd/system/topsupport-worker.service`:

```ini
[Unit]
Description=TopSupport background jobs (Hangfire)
After=network-online.target

[Service]
Type=notify
WorkingDirectory=/opt/topsupport-worker
ExecStart=/usr/bin/dotnet /opt/topsupport-worker/TopSupport.Worker.dll
Restart=always
RestartSec=10
User=topsupport
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

```sh
sudo systemctl daemon-reload
sudo systemctl enable --now topsupport-worker
journalctl -u topsupport-worker -f
```

### Opening the dashboard on EC2

The dashboard only listens on the instance's localhost, so it is never exposed. Forward it to your
machine with an SSM session (needs the Session Manager plugin; no inbound ports or SSH):

```sh
aws ssm start-session --target i-0123456789abcdef0 \
  --document-name AWS-StartPortForwardingSession \
  --parameters '{"portNumber":["5280"],"localPortNumber":["5280"]}'
```

Then open http://localhost:5280/hangfire.
