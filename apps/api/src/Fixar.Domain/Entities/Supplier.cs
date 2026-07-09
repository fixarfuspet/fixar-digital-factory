using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Supplier : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? TaxOffice { get; set; }

    public string? TaxNumber { get; set; }

    public string? Address { get; set; }

    public string DefaultCurrency { get; set; } = "TRY";

    public int? PaymentTermDays { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
