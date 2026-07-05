using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
