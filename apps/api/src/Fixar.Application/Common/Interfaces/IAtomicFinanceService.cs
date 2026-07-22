namespace Fixar.Application.Common.Interfaces;

public interface IAtomicFinanceService
{
    Task<AtomicFinanceResult> RecordCustomerCollectionAsync(AtomicCustomerCollectionCommand command, CancellationToken ct);
    Task<AtomicFinanceResult> RecordSupplierPaymentAsync(AtomicSupplierPaymentCommand command, CancellationToken ct);
    Task<AccountBalanceResult> GetAvailableBalanceAsync(Guid accountId, bool lockAccount, CancellationToken ct);
}

public sealed record AtomicAllocationLine(Guid DocumentId, decimal Amount, string? Notes = null);

public sealed record AtomicCustomerCollectionCommand(
    Guid CustomerId, Guid FinancialAccountId, DateTime TransactionDate, string Currency, decimal Amount,
    string PaymentMethod, string ExternalReference, IReadOnlyList<AtomicAllocationLine>? Allocations = null,
    bool AutoAllocate = false, bool AllowAdvance = true, decimal ExchangeRate = 1,
    string ReportingCurrency = "TRY", string? BankReference = null, string? Notes = null);

public sealed record AtomicSupplierPaymentCommand(
    Guid SupplierId, Guid FinancialAccountId, DateTime TransactionDate, string Currency, decimal Amount,
    string PaymentMethod, string ExternalReference, IReadOnlyList<AtomicAllocationLine>? Allocations = null,
    bool AutoAllocate = false, bool AllowAdvance = true, decimal ExchangeRate = 1,
    string ReportingCurrency = "TRY", string? BankReference = null, string? Notes = null,
    bool AllowNegativeBalance = false, string? OverrideReason = null);

public sealed record AtomicFinanceResult(
    bool Success, string? ErrorCode = null, string? Message = null, Guid? OperationId = null,
    string? OperationNumber = null, Guid? FinancialTransactionId = null, decimal AllocatedAmount = 0,
    decimal AdvanceAmount = 0, decimal? AvailableBalance = null, decimal? RequestedAmount = null,
    string? Currency = null)
{
    public static AtomicFinanceResult Fail(string code, string message, decimal? available = null, decimal? requested = null, string? currency = null)
        => new(false, code, message, AvailableBalance: available, RequestedAmount: requested, Currency: currency);
}

public sealed record AccountBalanceResult(bool Found, Guid AccountId, string? AccountCode, string? Name, string? Currency, decimal AvailableBalance, bool IsActive);
