using Asp.Versioning;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Application.Features.Auth;
using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", result.Errors), "REGISTRATION_FAILED"));
        }

        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Registration completed successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<object>.Fail(string.Join(" ", result.Errors), "LOGIN_FAILED"));
        }

        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Login successful."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<object>.Fail(string.Join(" ", result.Errors), "REFRESH_FAILED"));
        }

        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Token refreshed successfully."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Logged out successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var response = new
        {
            _currentUserService.UserId,
            _currentUserService.UserName,
            _currentUserService.Email,
            _currentUserService.Roles
        };

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }
}
