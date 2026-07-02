using Fixar.Domain.Common;

namespace Fixar.Infrastructure.Identity;

/// <summary>
/// A rotating refresh token tied to a single <see cref="ApplicationUser"/>.
/// Tokens are single-use: redeeming one revokes it and issues a
/// replacement, so a stolen token can only be replayed once before the
/// chain is detectably broken.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsActive => !IsRevoked && !IsExpired;
}
