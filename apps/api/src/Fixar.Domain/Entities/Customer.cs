using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Customer : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
