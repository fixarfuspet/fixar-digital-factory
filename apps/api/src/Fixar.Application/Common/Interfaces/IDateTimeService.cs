namespace Fixar.Application.Common.Interfaces;

/// <summary>
/// Abstracts the system clock so timestamps (audit trail, token expiry)
/// are testable and consistently UTC across the application.
/// </summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
