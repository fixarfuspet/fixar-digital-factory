using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Mold : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? SizeRange { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
