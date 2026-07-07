using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StockItem : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string Category { get; set; } = "Genel";

    public string Unit { get; set; } = "kg";

    public decimal CurrentQuantity { get; set; }

    public decimal CriticalQuantity { get; set; }

    public decimal? LastPurchasePrice { get; set; }

    public string? SupplierName { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}