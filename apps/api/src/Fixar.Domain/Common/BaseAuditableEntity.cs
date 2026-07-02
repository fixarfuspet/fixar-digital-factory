namespace Fixar.Domain.Common;

/// <summary>
/// Base class for entities that participate in the audit trail. The
/// EF Core save-changes interceptor stamps these properties automatically
/// on every insert/update, so business modules never set them by hand.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime Created { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public Guid? LastModifiedBy { get; set; }
}
