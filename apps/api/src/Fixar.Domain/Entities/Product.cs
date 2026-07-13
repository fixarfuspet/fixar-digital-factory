using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? Category { get; set; }

    public string? ModelCode { get; set; }

    public string? Description { get; set; }

    public string FoamType { get; set; } = "10100";

    public string ProductType { get; set; } = "Normal";

    public bool IsFabric { get; set; }

    public bool IsAdhesive { get; set; }

    public bool HasDTFLabel { get; set; }

    public bool HasPolibond { get; set; }

    public decimal? AverageWeight { get; set; }

    public decimal? TargetDensity { get; set; }

    public decimal? StandardCycleTime { get; set; }

    public int? DefaultBoxQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<Mold> Molds { get; set; } = new List<Mold>();
}
