using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
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
// returned bearer token in this cookie (AuthCookie). Pages declare their roles with
// [Authorize(Roles = ...)]; the Api enforces the same roles itself, so the cookie only decides
// what the UI shows.
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

// Typed HttpClient calling TopSupport.Api -- the real HTTP boundary to the middle tier. A 401
// (the Api refused the session token) becomes ApiSessionExpiredException; see /account/expired.
var apiBaseUrl = builder.Configuration["TopSupportApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing \"TopSupportApi:BaseUrl\" configuration value.");
builder.Services.AddTransient<ApiSessionExpiredHandler>();
builder.Services.AddHttpClient<TopSupportApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<ApiSessionExpiredHandler>();

// Sign-in attempts: 10 a minute per client address, on top of the Api's per-account lockout (which
// doesn't slow down guessing across many usernames). Behind a reverse proxy, configure forwarded
// headers so RemoteIpAddress is the real client.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals("/signin", StringComparison.OrdinalIgnoreCase)
            ? RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
            : RateLimitPartition.GetNoLimiter("other"));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Redirect("/signin?tooMany=1");
        return ValueTask.CompletedTask;
    };
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
app.UseRateLimiter();

// OWASP A05: no framing (clickjacking), no MIME sniffing, no referrer leakage, and a CSP that
// only allows this site's own scripts plus the Google Fonts stylesheet the app links. Inline
// styles are allowed (the markup uses style attributes); inline scripts only for Blazor's import
// map. 'self' in connect-src covers the Blazor circuit's same-origin WebSocket.
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
            + "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; "
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
// the sign-out in the Api, then drops the cookie; the Api token simply expires. The token is checked
// here: the antiforgery middleware doesn't reject a request to an endpoint that binds no form.
app.MapPost("/account/signout", async (HttpContext context, TopSupportApiClient api, IAntiforgery antiforgery, ILogger<Program> logger) =>
{
    if (!await antiforgery.IsRequestValidAsync(context)) return Results.BadRequest();

    if (context.User.Identity?.IsAuthenticated == true)
    {
        try
        {
            await api.SignOutAsync();
        }
        catch (ApiSessionExpiredException)
        {
            // The token was already refused, so there's no session left to audit.
        }
        catch (HttpRequestException ex)
        {
            // Signing out locally matters more than the audit entry, but the miss is logged.
            logger.LogWarning(ex, "Couldn't audit the sign-out in the Api; signing out anyway");
        }
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("~/");
});

// Where the error boundary sends a page whose Api call got a 401. Signs out only once the Api
// confirms it refuses the token (so a link here can't sign anyone out), then asks them to sign in.
app.MapGet("/account/expired", async (HttpContext context, TopSupportApiClient api) =>
{
    if (context.User.Identity?.IsAuthenticated != true) return Results.LocalRedirect("~/signin");

    try
    {
        await api.GetNavCountsAsync();
        return Results.LocalRedirect("~/home");
    }
    catch (ApiSessionExpiredException)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("~/signin?expired=1");
    }
});

// Development-only test-account switcher (TestAccountSwitcher.cs): never mapped otherwise.
if (app.Services.GetRequiredService<TestAccountSwitcher>().IsEnabled)
{
    app.MapPost("/account/test-switch", (HttpContext context, TopSupportApiClient api, ILogger<TestAccountSwitcher> logger, [FromForm] string? accountId, [FromForm] string? returnUrl)
        => TestAccountSwitcher.SwitchAsync(context, api, logger, accountId, returnUrl));
}

app.Run();
