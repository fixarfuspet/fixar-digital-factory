namespace Fixar.Domain.Common;

/// <summary>
/// Base class for all domain entities. Every entity is identified by a
/// server-generated GUID so identifiers are stable across distributed
/// services and never leak sequential/database-internal information.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
