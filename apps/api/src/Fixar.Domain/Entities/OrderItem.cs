using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }

    public string? ProductionType { get; set; }

    public string? FabricColor { get; set; }

    public int QuantityPairs { get; set; }

    public int ProducedPairs { get; set; }

    public int CutPairs { get; set; }

    public int ShippedPairs { get; set; }

    public string Status { get; set; } = "Bekliyor";

    public string? Note { get; set; }
}
