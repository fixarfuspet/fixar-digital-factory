using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class RecipeItem : BaseAuditableEntity
{
    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = default!;

    public Guid MaterialId { get; set; }

    public Material Material { get; set; } = default!;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal WastePercent { get; set; }

    public bool IsOptional { get; set; }

    public int Sequence { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
