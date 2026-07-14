using Fixar.Domain.Common;
using System.Collections.Generic;

namespace Fixar.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public string SizeRange { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public int ProducedQuantity { get; set; }
    public int CutQuantity { get; set; }
    public int ShippedQuantity { get; set; }

    public DateTime? DueDate { get; set; }
    public string? CustomerReference { get; set; }
    public string Currency { get; set; } = "TRY";
    public string ExpectedPaymentMethod { get; set; } = "OpenAccount";
    public int PaymentTermDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Aktif";
    public bool IsActive { get; set; } = true;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public ICollection<OrderItem> Items { get; set; }
    = new List<OrderItem>();
}
