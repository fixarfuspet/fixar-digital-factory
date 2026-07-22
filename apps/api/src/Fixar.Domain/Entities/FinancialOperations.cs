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
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? FinanceCategoryId { get; set; }
    public FinanceCategory? FinanceCategory { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? CustomerCollectionId { get; set; }
    public Guid? ChequeId { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public string ReportingCurrency { get; set; } = "TRY";
    public decimal ReportingAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CounterpartyName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? BusinessReference { get; set; }
    public bool AffectsBalance { get; set; } = true;
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

public class FinanceCategory : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "Expense";
    public Guid? ParentCategoryId { get; set; }
    public FinanceCategory? ParentCategory { get; set; }
    public ICollection<FinanceCategory> Children { get; set; } = new List<FinanceCategory>();
    public string? CostCenter { get; set; }
    public bool IncludeInProductionCost { get; set; }
    public string CostBehavior { get; set; } = "Variable";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class AccountReconciliation : BaseAuditableEntity
{
    public string ReconciliationNumber { get; set; } = string.Empty;
    public string AccountPartyType { get; set; } = "Customer";
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = "Draft";
    public string SnapshotJson { get; set; } = "{}";
    public string? CounterpartyNote { get; set; }
    public string? InternalNote { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
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
