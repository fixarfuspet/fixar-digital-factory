using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ProductionBox : BaseAuditableEntity
{
    public string BoxCode { get; set; } = string.Empty;

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

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

    // Boş, Üretimde, Kesimde, Depoda, Sevkiyatta
    public string CurrentStatus { get; set; } = "Boş";

    // İstasyon 5, Kesim, Raf A12...
    public string? CurrentLocation { get; set; }

    // Mahmut, Ramazan...
    public string? OperatorName { get; set; }

    public DateTime? FilledAt { get; set; }

    public ICollection<ProductionBoxEvent> Events { get; set; }
        = new List<ProductionBoxEvent>();
}
