using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionBox : BaseAuditableEntity
{
    public string BoxCode { get; set; } = string.Empty;

    public string? BoxNumber { get; set; }

    public string? Barcode { get; set; }

    public string TraceabilityCode { get; set; } = string.Empty;

    public Guid? StationAssignmentId { get; set; }
    public StationAssignment? StationAssignment { get; set; }

    public Guid? CuttingRecordId { get; set; }
    public CuttingRecord? CuttingRecord { get; set; }

    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }

    public string? CustomerName { get; set; }

    // Çıplak, Kumaşlı, Yapışkanlı
    public string? ProductionType { get; set; }

    // Siyah, Kırmızı, Lacivert...
    public string? FabricColor { get; set; }

    // Kasadaki çift adedi
    public int QuantityPairs { get; set; }

    public int PairCount { get; set; }

    // Boş, Üretimde, Kesimde, Depoda, Sevkiyatta
    public string CurrentStatus { get; set; } = "Boş";

    public string Status { get; set; } = "Packed";

    // İstasyon 5, Kesim, Raf A12...
    public string? CurrentLocation { get; set; }

    public string? WarehouseLocation { get; set; }

    public string? RackCode { get; set; }

    // Mahmut, Ramazan...
    public string? OperatorName { get; set; }

    public DateTime? FilledAt { get; set; }

    public DateTime? PackedAt { get; set; }

    public Guid? PackedByOperatorId { get; set; }
    public Operator? PackedByOperator { get; set; }

    public DateTime? ReceivedToWarehouseAt { get; set; }

    public string? ReceivedToWarehouseBy { get; set; }

    public DateTime? ReadyForShipmentAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public string? ShipmentReference { get; set; }

    public string? ShipmentNotes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsCancelled { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? ProductCodeSnapshot { get; set; }

    public string? ProductNameSnapshot { get; set; }

    public string? WorkOrderNumberSnapshot { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByName { get; set; }

    public string? UpdatedByName { get; set; }

    public ICollection<ProductionBoxEvent> Events { get; set; }
        = new List<ProductionBoxEvent>();
}
