using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionBoxEvent : BaseAuditableEntity
{
    public Guid ProductionBoxId { get; set; }
    public ProductionBox ProductionBox { get; set; } = default!;

    // Oluşturuldu, Üretimden Çıktı, Kesime Başladı, Kesim Bitti, Depoya Girdi, Sevk Edildi
    public string EventType { get; set; } = string.Empty;

    public string? FromLocation { get; set; }

    public string? ToLocation { get; set; }

    public string? OperatorName { get; set; }

    public int? QuantityPairs { get; set; }

    public string? Note { get; set; }

    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}
