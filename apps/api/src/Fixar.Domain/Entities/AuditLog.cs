using Fixar.Domain.Common;
using Fixar.Domain.Enums;

namespace Fixar.Domain.Entities;

/// <summary>
/// Immutable record of a data change, written automatically by the EF Core
/// save-changes interceptor. Deliberately does not derive from
/// <see cref="BaseAuditableEntity"/> to avoid auditing the audit trail itself.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public AuditAction Action { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the entity's property values before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the entity's property values after the change.</summary>
    public string? NewValues { get; set; }

    /// <summary>Comma-separated list of property names that changed.</summary>
    public string? AffectedColumns { get; set; }

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }
}
