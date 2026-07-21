using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Quote : BaseAuditableEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public string Currency { get; set; } = "TRY";
    public int PaymentTermDays { get; set; }
    public bool PartialDeliveryAllowed { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal? TotalEstimatedCost { get; set; }
    public decimal? EstimatedGrossProfit { get; set; }
    public decimal? EstimatedGrossMarginPercent { get; set; }
    public decimal? EstimatedLeadTimeDays { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public Guid? ConvertedOrderId { get; set; }
    public Order? ConvertedOrder { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? CalculationWarnings { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}

public class QuoteItem : BaseAuditableEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = default!;
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool FabricRequired { get; set; }
    public bool DtfRequired { get; set; }
    public string? LabelDescription { get; set; }
    public decimal? UnitEstimatedCost { get; set; }
    public decimal? TotalEstimatedCost { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal? EstimatedGrossProfit { get; set; }
    public decimal? EstimatedGrossMarginPercent { get; set; }
    public decimal? EstimatedLeadTimeDays { get; set; }
    public string? CalculationWarnings { get; set; }
    public string? Notes { get; set; }
}
