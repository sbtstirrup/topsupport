using Microsoft.AspNetCore.Identity;
using TopSupport.Api.Data;
using TopSupport.Api.Identity;
using TopSupport.Services.Abstractions;
using TopSupport.Services.Admin;
using TopSupport.Services.Auth;
using TopSupport.Services.Tickets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

// ASP.NET Core Identity, backed by the skeleton ADO.NET stores in TopSupport.Api/Identity.
// AdoUserStore/AdoRoleStore are not implemented yet (every member throws) -- see their doc
// comments and TopSupport.Database/README.md for what's still needed before sign-in works.
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddUserStore<AdoUserStore>()
    .AddRoleStore<AdoRoleStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
