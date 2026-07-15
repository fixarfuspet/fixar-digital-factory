using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class CustomerReceivable : BaseAuditableEntity
{
    public string ReceivableNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public string? OrderNumberSnapshot { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal OriginalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Status { get; set; } = "Open";
    public string SourceType { get; set; } = "SalesOrder";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public ICollection<CollectionAllocation> Allocations { get; set; } = new List<CollectionAllocation>();
}

public class CustomerCollection : BaseAuditableEntity
{
    public string CollectionNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime CollectionDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? ReferenceNumber { get; set; }
    public string? BankReference { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal UnallocatedAmount { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public string? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? ReversalCollectionId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public Guid? CustomerChequeId { get; set; }
    public Guid? FinancialTransactionId { get; set; }
    public string FinancePostingStatus { get; set; } = "Pending";
    public string? FinancePostingWarning { get; set; }
    public ICollection<CollectionAllocation> Allocations { get; set; } = new List<CollectionAllocation>();
}

public class CollectionAllocation : BaseEntity
{
    public Guid CustomerCollectionId { get; set; }
    public CustomerCollection CustomerCollection { get; set; } = default!;
    public Guid CustomerReceivableId { get; set; }
    public CustomerReceivable CustomerReceivable { get; set; } = default!;
    public Guid? OrderId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime AllocationDate { get; set; }
    public string? Notes { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public DateTime Created { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class CustomerLedgerEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime TransactionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string EntryType { get; set; } = "Debit";
    public string SourceType { get; set; } = "Receivable";
    public Guid SourceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversalEntryId { get; set; }
    public DateTime Created { get; set; }
    public Guid? CreatedBy { get; set; }
}
