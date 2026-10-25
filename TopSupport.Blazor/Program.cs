using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TopSupport.Blazor.Components;
using TopSupport.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // The rich-text editor sends its HTML to the server as it changes; the 32 KB default would
    // drop the circuit well before the Api's 100,000-character reply limit.
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 256 * 1024);

// Sign-in: SignIn.razor posts the credentials to the Api (POST api/auth/sign-in) and keeps the
// returned bearer token in this cookie (AuthCookie). Pages check the role through RoleGate; the
// Api enforces the same roles itself, so the cookie only decides what the UI shows.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TopSupport.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/signin";
        options.AccessDeniedPath = "/home";
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = AuthCookie.ValidateAsync;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// App-level services mirroring the demo's global S state and navItems()/workspaceNav().
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<PageHeadingState>();
// Scoped (not singleton) -- NavProvider now calls the per-circuit TopSupportApiClient to
// populate nav-count badges, so it can no longer be a shared singleton.
builder.Services.AddScoped<NavProvider>();
builder.Services.AddScoped<LookupDataService>();
builder.Services.AddScoped<ModalState>();
builder.Services.AddScoped<ToastState>();
builder.Services.AddSingleton<TestAccountSwitcher>();

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

// OWASP A05: no framing (clickjacking), no MIME sniffing, no referrer leakage, and a CSP that
// only allows this site's own scripts plus the Google Fonts stylesheet the app links. Inline
// styles are allowed (the markup uses style attributes); inline scripts only for Blazor's import
// map. wss: is the Blazor circuit.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers.XFrameOptions = "DENY";
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; "
            + "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self' wss:; "
            + "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
        return Task.CompletedTask;
    });
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ACT.signout: a form post (with an antiforgery token) from the shell's Sign out buttons. Audits
// the sign-out in the Api, then drops the cookie; the Api token simply expires.
app.MapPost("/account/signout", async (HttpContext context, TopSupportApiClient api) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        try
        {
            await api.SignOutAsync();
        }
        catch (HttpRequestException)
        {
            // An expired token can't audit the sign-out; signing out locally still matters more.
        }
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("~/");
}).WithMetadata(new RequireAntiforgeryTokenAttribute());

// Development-only test-account switcher (TestAccountSwitcher.cs): never mapped otherwise.
if (app.Services.GetRequiredService<TestAccountSwitcher>().IsEnabled)
{
    app.MapPost("/account/test-switch", (HttpContext context, TopSupportApiClient api, [FromForm] string? accountId, [FromForm] string? returnUrl)
        => TestAccountSwitcher.SwitchAsync(context, api, accountId, returnUrl));
}

app.Run();
