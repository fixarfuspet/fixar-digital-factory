using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class FinancialAccount : BaseAuditableEntity
{
    public string AccountCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Cash";
    public string Currency { get; set; } = "TRY";
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? Iban { get; set; }
    public string? AccountNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningBalanceDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class FinancialTransaction : BaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public Guid FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = default!;
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public string TransactionType { get; set; } = "CustomerCollection";
    public string Direction { get; set; } = "Inflow";
    public string SourceType { get; set; } = "Manual";
    public Guid? SourceId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CustomerCollectionId { get; set; }
    public Guid? ChequeId { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? BankReference { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversalTransactionId { get; set; }
    public DateTime? ReversedAt { get; set; }
    public string? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public DateTime Created { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class CustomerCheque : BaseAuditableEntity
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string PortfolioNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid? CustomerCollectionId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string DrawerName { get; set; } = string.Empty;
    public DateTime ChequeDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime ReceivedDate { get; set; }
    public DateTime? DepositedDate { get; set; }
    public DateTime? CollectedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public DateTime? BouncedDate { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public string? BankReference { get; set; }
    public string? Notes { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public string? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? EndorsedSupplierId { get; set; }
    public Guid? SupplierPaymentId { get; set; }
    public DateTime? EndorsedAt { get; set; }
    public string? EndorsedBy { get; set; }
    public string? EndorsementReference { get; set; }
    public string? EndorsementNotes { get; set; }
    public ICollection<ChequeEvent> Events { get; set; } = new List<ChequeEvent>();
}

public class ChequeEvent : BaseEntity
{
    public Guid CustomerChequeId { get; set; }
    public CustomerCheque CustomerCheque { get; set; } = default!;
    public string EventType { get; set; } = "Created";
    public DateTime EventDate { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid? FinancialAccountId { get; set; }
    public Guid? FinancialTransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
    public DateTime Created { get; set; }
    public Guid? CreatedBy { get; set; }
}
