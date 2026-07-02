namespace Fixar.Application.Common.Models;

/// <summary>
/// Binds to the "Jwt" configuration section. <see cref="Secret"/> must be
/// supplied via environment variable / secret store in every real
/// environment — never commit an actual secret to appsettings.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
