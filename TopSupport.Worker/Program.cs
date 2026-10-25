using System.Net;
using Amazon;
using Amazon.CloudWatch;
using Amazon.SimpleEmailV2;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.Extensions.Options;
using TopSupport.Worker.Data;
using TopSupport.Worker.Email;
using TopSupport.Worker.Jobs;
using TopSupport.Worker.Metrics;
using TopSupport.Worker.Options;

var builder = WebApplication.CreateBuilder(args);

// Outside Development, settings (and secrets) can also come from AWS Systems Manager Parameter
// Store under "Aws:ParameterStorePath", e.g. /TopSupport/Worker/ConnectionStrings/TopSupport.
// Locally, secrets go in user secrets (dotnet user-secrets), never appsettings.
var parameterStorePath = builder.Configuration["Aws:ParameterStorePath"];
if (!builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(parameterStorePath))
{
    builder.Configuration.AddSystemsManager(source =>
    {
        source.Path = parameterStorePath;
        source.Optional = true;
    });
}

// One build runs as a console app, a Windows service or a systemd unit (each is a no-op
// unless the process was started by that service manager).
builder.Services.AddWindowsService(options => options.ServiceName = "TopSupport.Worker");
builder.Services.AddSystemd();

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<WorkerDashboardOptions>(builder.Configuration.GetSection(WorkerDashboardOptions.SectionName));
builder.Services.Configure<CloudWatchOptions>(builder.Configuration.GetSection(CloudWatchOptions.SectionName));
builder.Services.Configure<JobOptions>(builder.Configuration.GetSection(JobOptions.SectionName));

// Jobs read the time only through TimeProvider (tests swap in a FakeTimeProvider).
builder.Services.AddSingleton(TimeProvider.System);

// Data access: worker.* stored procs over the existing TopSupport database (15_Worker.sql).
builder.Services.AddSingleton<WorkerDb>();
builder.Services.AddSingleton<IWorkerTicketStore, WorkerTicketStore>();
builder.Services.AddSingleton<INotificationLog, NotificationLog>();
builder.Services.AddSingleton<IJobCursorStore, JobCursorStore>();

// Email: "Email:Provider" picks the sender. SES is refused in Development so nothing local can
// send real email; AWS clients use the default credential chain (the EC2 instance role).
var emailProvider = builder.Configuration[$"{EmailOptions.SectionName}:Provider"] ?? "Smtp";
var awsRegion = builder.Configuration["Aws:Region"];
switch (emailProvider.ToLowerInvariant())
{
    case "smtp":
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        break;
    case "ses" when builder.Environment.IsDevelopment():
        throw new InvalidOperationException("Email:Provider \"Ses\" is not allowed in Development; use \"Smtp\" with smtp4dev.");
    case "ses":
        builder.Services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => string.IsNullOrWhiteSpace(awsRegion)
            ? new AmazonSimpleEmailServiceV2Client()
            : new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(awsRegion)));
        builder.Services.AddSingleton<IEmailSender, SesEmailSender>();
        break;
    default:
        throw new InvalidOperationException($"Unknown Email:Provider \"{emailProvider}\"; use \"Smtp\" or \"Ses\".");
}

builder.Services.AddSingleton<NotificationDispatcher>();

if (builder.Configuration.GetValue<bool>($"{CloudWatchOptions.SectionName}:Enabled") && !builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAmazonCloudWatch>(_ => string.IsNullOrWhiteSpace(awsRegion)
        ? new AmazonCloudWatchClient()
        : new AmazonCloudWatchClient(RegionEndpoint.GetBySystemName(awsRegion)));
    builder.Services.AddSingleton<IMetricsPublisher, CloudWatchMetricsPublisher>();
}
else
{
    builder.Services.AddSingleton<IMetricsPublisher, NullMetricsPublisher>();
}

builder.Services.AddTransient<TicketEventsJob>();
builder.Services.AddTransient<SlaSweepJob>();
builder.Services.AddTransient<AutoCloseJob>();
builder.Services.AddTransient<DailyDigestJob>();
builder.Services.AddTransient<HeartbeatJob>();

// Hangfire: SQL Server storage in the "jobs" schema of the same database (created on first start).
var connectionString = builder.Configuration.GetConnectionString("TopSupport")
    ?? throw new InvalidOperationException("Missing \"ConnectionStrings:TopSupport\" configuration value.");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        SchemaName = "jobs",
        PrepareSchemaIfNecessary = true
    }));
builder.Services.AddHangfireServer(options => options.ServerName = $"TopSupport.Worker:{Environment.MachineName}");

// The dashboard listens on localhost only. On EC2 it's reached through an SSM port-forwarding
// session (see TopSupport.Worker/README.md), so it is never exposed and needs no login.
// In a container (the .NET images set DOTNET_RUNNING_IN_CONTAINER) it has to listen on all
// interfaces for Docker's port mapping to reach it; docker-compose.worker.yml then publishes it
// on the host's 127.0.0.1 only, and requests arrive from the Docker gateway rather than loopback.
var inContainer = builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
var dashboardPort = builder.Configuration.GetValue<int?>($"{WorkerDashboardOptions.SectionName}:Port") ?? new WorkerDashboardOptions().Port;
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(inContainer ? IPAddress.Any : IPAddress.Loopback, dashboardPort));

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "TopSupport Worker",
    Authorization = inContainer
        ? Array.Empty<IDashboardAuthorizationFilter>()
        : new IDashboardAuthorizationFilter[] { new LocalRequestsOnlyAuthorizationFilter() }
});
app.MapGet("/", () => Results.Redirect("/hangfire"));

RecurringJobsRegistration.Register(app.Services.GetRequiredService<IRecurringJobManager>(), app.Services.GetRequiredService<IOptions<JobOptions>>().Value);

app.Run();
