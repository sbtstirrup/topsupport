using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TopSupport.Api;
using TopSupport.Api.Auth;
using TopSupport.Api.Data;
using TopSupport.Models.Enums;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;
using TopSupport.Services.Auth;
using TopSupport.Services.Tickets;

var builder = WebApplication.CreateBuilder(args);

// Structured logs with scopes, so every line carries the request's TraceId (the Blazor app's
// HttpClient sends it as traceparent, so it matches the Blazor app's logs and the traceId in
// error responses) and, once signed in, the AccountId. JSON outside Development.
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(o => { o.IncludeScopes = true; o.SingleLine = true; });
}
else
{
    builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);
}

builder.Services.AddControllers();

// Unhandled exceptions become ProblemDetails with no stack trace or SQL text (OWASP A05):
// ValidationException -> 400, KeyNotFoundException -> 404, anything else a logged 500.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

// ADO.NET data access (no EF Core, no Dapper -- see TopSupport.Database/README.md).
builder.Services.AddSingleton<DbExecutor>();
builder.Services.AddScoped<IAccountCredentialRepository, AccountCredentialRepository>();
builder.Services.AddScoped<IAgentAdminRepository, AgentAdminRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ICannedResponseRepository, CannedResponseRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
builder.Services.AddScoped<IOrgRepository, OrgRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITicketAttachmentRepository, TicketAttachmentRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkerMonitorRepository, WorkerMonitorRepository>();

// Services (Controller -> Service -> Repository).
builder.Services.AddScoped<AccountPasswordService>();
builder.Services.AddScoped<AgentAdminService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CannedResponseService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<KnowledgeBaseService>();
builder.Services.AddScoped<OrgAdminService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<QueueService>();
builder.Services.AddScoped<RolePermissionService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<TicketAttachmentService>();
builder.Services.AddScoped<TicketFieldOptionsResolver>();
builder.Services.AddScoped<TicketFormValidator>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<WorkerMonitorService>();

// Services read the time through TimeProvider (never DateTimeOffset.UtcNow), so tests can fix it.
builder.Services.AddSingleton(TimeProvider.System);

// Sign-in issues a signed bearer token (AuthController); every endpoint requires one unless it
// is [AllowAnonymous], and the admin/agent screens' endpoints also require the role.
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtTokenService.ValidationParameters(authOptions);

        // A valid signature isn't enough: refuse the token once its account is locked, deleted,
        // deactivated or has a different role than the token says (e.g. an admin was demoted).
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var credentials = context.HttpContext.RequestServices.GetRequiredService<IAccountCredentialRepository>();
                var status = await credentials.GetStatusAsync(principal.AccountId(), context.HttpContext.RequestAborted);
                if (status is null || status.Locked || !status.Active
                    || AuthService.RoleOf(status.AccountType, status.AgentRole) != principal.AppRole())
                {
                    context.Fail("The account is no longer allowed to sign in with this token.");
                }
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.Agent, p => p.RequireRole(nameof(AppRole.Agent), nameof(AppRole.Admin)))
    .AddPolicy(Policies.Admin, p => p.RequireRole(nameof(AppRole.Admin)));

var app = builder.Build();

// The Api only returns JSON and attachment downloads: never sniff a client-declared content
// type into something executable, and never allow framing. Set on OnStarting so the headers
// survive the exception handler clearing the response.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        return Task.CompletedTask;
    });
    await next();
});

// Only reads stop when the caller goes away. A change runs to completion: abandoning it between
// its steps (e.g. a ticket's insert and its timeline events) would leave it half-written.
app.Use((context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
    {
        context.RequestAborted = CancellationToken.None;
    }

    return next(context);
});

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// Every log line for a signed-in request names the account (an id, never a name or email).
app.Use(async (context, next) =>
{
    using var scope = context.User.FindFirst(JwtTokenService.AccountIdClaim) is { } accountId
        ? app.Logger.BeginScope(new Dictionary<string, object> { ["AccountId"] = accountId.Value })
        : null;
    await next(context);
});

app.UseAuthorization();

app.MapControllers();

app.Run();
