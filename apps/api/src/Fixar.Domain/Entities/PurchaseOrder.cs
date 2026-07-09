using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class PurchaseOrder : BaseAuditableEntity
{
    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierCode { get; set; }

    public string? DocumentNo { get; set; }

    public string? InvoiceNo { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public string PaymentType { get; set; } = "Cari Hesap";

    public string Currency { get; set; } = "TRY";

    public decimal? VatRate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public string Status { get; set; } = "Oluşturuldu";

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
