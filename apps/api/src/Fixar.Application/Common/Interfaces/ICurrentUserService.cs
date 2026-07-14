namespace Fixar.Application.Common.Interfaces;

/// <summary>
/// Exposes the identity of the caller behind the current request, derived
/// from the validated JWT. Implemented in Infrastructure via
/// IHttpContextAccessor so Application/Domain never depend on ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? UserName { get; }

    string? IpAddress { get; }

    bool IsAuthenticated { get; }

    IReadOnlyList<string> Roles { get; }
}
