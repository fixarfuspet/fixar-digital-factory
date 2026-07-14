using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class ExchangeRate : BaseAuditableEntity
{
    public DateTime RateDate { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Source { get; set; } = "Manual";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
