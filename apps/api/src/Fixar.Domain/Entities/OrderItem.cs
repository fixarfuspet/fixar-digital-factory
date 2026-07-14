using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    public int LineNumber { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }

    public string? ProductionType { get; set; }

    public string? FabricColor { get; set; }
    public string? SizeRange { get; set; }
    public string? Color { get; set; }

    public int QuantityPairs { get; set; }

    public int ProducedPairs { get; set; }

    public int CutPairs { get; set; }

    public int ShippedPairs { get; set; }

    public string Status { get; set; } = "Bekliyor";

    public string? Note { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
