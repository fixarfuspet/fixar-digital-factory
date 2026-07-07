using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class StockMovement : BaseAuditableEntity
{
    public Guid StockItemId { get; set; }

    public StockItem StockItem { get; set; } = default!;

    public string MovementType { get; set; } = "Giriş";

    public decimal Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public string? SourceType { get; set; }

    public string? SourceDocumentNo { get; set; }

    public string? Note { get; set; }
}