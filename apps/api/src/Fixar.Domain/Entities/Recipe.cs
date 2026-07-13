using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Recipe : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;

    public int Version { get; set; } = 1;

    public string? Description { get; set; }

    public decimal OutputQuantity { get; set; } = 1;

    public string OutputUnit { get; set; } = "Çift";

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RecipeItem> Items { get; set; } = new List<RecipeItem>();

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
