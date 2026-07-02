using Fixar.Application.Features.Auth;

namespace Fixar.Application.Common.Interfaces;

/// <summary>
/// Authentication use cases exposed to the API layer. Implemented in
/// Infrastructure on top of ASP.NET Identity + JWT issuance, so
/// controllers never depend on UserManager/SignInManager directly.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
}
