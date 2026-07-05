using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Order : BaseAuditableEntity
{
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

    public string Status { get; set; } = "Aktif";
}
