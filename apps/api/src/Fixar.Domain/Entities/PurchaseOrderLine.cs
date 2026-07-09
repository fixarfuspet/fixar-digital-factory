using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = default!;

    public Guid StockItemId { get; set; }

    public StockItem StockItem { get; set; } = default!;

    public string StockName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "kg";

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public string? Note { get; set; }
}
