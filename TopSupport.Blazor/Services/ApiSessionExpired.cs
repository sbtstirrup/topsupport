using System.Net;

namespace TopSupport.Blazor.Services;

/// <summary>
/// The Api refused the signed-in account's token (401): it expired, the signing key changed, or the
/// account was locked, deactivated or given another role. Deliberately not an HttpRequestException,
/// so pages' "show the Api's message" catches don't treat it as an ordinary failed call: it reaches
/// MainLayout's error boundary, which sends the browser to /account/expired.
/// </summary>
public sealed class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException(string requestUri)
        : base($"The Api refused the session token (401) for {requestUri}.")
    {
    }
}

/// <summary>HttpClient handler on TopSupportApiClient: turns every 401 into <see cref="ApiSessionExpiredException"/>.</summary>
public sealed class ApiSessionExpiredHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            throw new ApiSessionExpiredException(request.RequestUri?.AbsolutePath ?? "(unknown)");
        }

        return response;
    }
}
