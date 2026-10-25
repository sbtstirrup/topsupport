using TopSupport.Blazor.Components;
using TopSupport.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// App-level services mirroring the demo's global S state and navItems()/workspaceNav().
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<PageHeadingState>();
// Scoped (not singleton) -- NavProvider now calls the per-circuit TopSupportApiClient to
// populate nav-count badges, so it can no longer be a shared singleton.
builder.Services.AddScoped<NavProvider>();
builder.Services.AddScoped<LookupDataService>();
builder.Services.AddScoped<ModalState>();
builder.Services.AddScoped<ToastState>();

// Typed HttpClient calling TopSupport.Api -- the real HTTP boundary to the middle tier.
var apiBaseUrl = builder.Configuration["TopSupportApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing \"TopSupportApi:BaseUrl\" configuration value.");
builder.Services.AddHttpClient<TopSupportApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
