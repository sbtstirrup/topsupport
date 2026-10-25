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

// Add services to the container.

builder.Services.AddControllers();

// Unhandled exceptions become ProblemDetails with no stack trace or SQL text (OWASP A05).
// Services throw ArgumentException for bad input (e.g. an unknown ticket status) and
// KeyNotFoundException for a missing record; anything else is a generic 500.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ADO.NET data access (no EF Core, no Dapper -- see TopSupport.Database/README.md).
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDbExecutor, DbExecutor>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ITicketAttachmentRepository, TicketAttachmentRepository>();
builder.Services.AddScoped<ICannedResponseRepository, CannedResponseRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAccountCredentialRepository, AccountCredentialRepository>();
builder.Services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAgentAdminRepository, AgentAdminRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

// Business/service layer (Controller -> Service -> Repository).
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketFieldOptionsResolver, TicketFieldOptionsResolver>();
builder.Services.AddScoped<ITicketFormValidator, TicketFormValidator>();
builder.Services.AddScoped<ICannedResponseService, CannedResponseService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAccountPasswordService, AccountPasswordService>();
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAgentAdminService, AgentAdminService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();

// Admin: Users and Organisations.
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();
builder.Services.AddScoped<IOrgRepository, OrgRepository>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IOrgAdminService, OrgAdminService>();

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

// Configure the HTTP request pipeline.

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
app.UseAuthorization();

app.MapControllers();

app.Run();
