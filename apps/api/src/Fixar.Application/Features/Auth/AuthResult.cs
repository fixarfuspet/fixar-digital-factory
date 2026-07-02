namespace Fixar.Application.Features.Auth;

/// <summary>
/// Outcome of a register/login/refresh operation. <see cref="Errors"/> is
/// populated only when <see cref="Succeeded"/> is false.
/// </summary>
public class AuthResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public string AccessToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    public static AuthResult Fail(params string[] errors) => new() { Succeeded = false, Errors = errors };
}
