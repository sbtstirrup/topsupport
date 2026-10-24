using Microsoft.AspNetCore.Mvc;
using TopSupport.Services.Auth;

namespace TopSupport.Api.Controllers;

/// <summary>
/// Stub auth endpoints. Delegates to IAuthService, which currently throws
/// NotImplementedException pending the ADO.NET-backed Identity store (see
/// TopSupport.Api/Identity and TopSupport.Database/README.md).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    public record SignInRequest(string Username, string Password);

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.SignInAsync(request.Username, request.Password, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await _authService.SignOutAsync(cancellationToken);
        return Ok();
    }
}
