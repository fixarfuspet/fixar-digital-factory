using System.Data;
using System.Text.Json;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.Infrastructure.Services;

public sealed class AtomicFinanceService(ApplicationDbContext db, ICurrentUserService currentUser) : IAtomicFinanceService
{
    public async Task<AtomicFinanceResult> RecordCustomerCollectionAsync(AtomicCustomerCollectionCommand command, CancellationToken ct)
    {
        var error = ValidateCommon(command.Amount, command.Currency, command.ExternalReference, command.ExchangeRate, command.ReportingCurrency);
        if (error is not null) return error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.CustomerId && x.IsActive, ct);
        if (customer is null) return AtomicFinanceResult.Fail("CUSTOMER_NOT_FOUND", "Müşteri bulunamadı.");
        var account = await LockedAccount(command.FinancialAccountId, ct);
        var accountError = ValidateAccount(account, command.Currency, command.PaymentMethod, false);
        if (accountError is not null) return accountError;
        var reference = NormalizeReference(command.ExternalReference);
        if (await db.FinancialTransactions.AnyAsync(x => x.BusinessReference == $"CUSTOMER_COLLECTION:{reference}" && !x.IsReversed, ct))
            return AtomicFinanceResult.Fail("DUPLICATE_OPERATION", "Bu referansla tahsilat daha önce kaydedilmiş.");

        var date = command.TransactionDate.ToUniversalTime();
        var collection = new CustomerCollection
        {
            Id = Guid.NewGuid(), CollectionNumber = await Next("COL", date, db.CustomerCollections.Select(x => x.CollectionNumber), ct),
            CustomerId = command.CustomerId, CollectionDate = date, Currency = command.Currency.ToUpperInvariant(),
            Amount = command.Amount, UnallocatedAmount = command.Amount, PaymentMethod = command.PaymentMethod,
            FinancialAccountId = account!.Id, ReferenceNumber = command.ExternalReference.Trim(), BankReference = command.BankReference,
            Notes = command.Notes, Status = "Posted", FinancePostingStatus = "Posted"
        };
        db.CustomerCollections.Add(collection);

        var allocations = await CustomerAllocations(command, collection, ct);
        if (!allocations.Success) return allocations.Error!;
        if (collection.UnallocatedAmount > 0 && !command.AllowAdvance)
            return AtomicFinanceResult.Fail("ADVANCE_CONFIRMATION_REQUIRED", "Tahsilat tutarı seçilen alacaklardan fazladır. Kalan tutarı avans olarak kaydedebilirsiniz.");

        var ledger = new CustomerLedgerEntry
        {
            Id = Guid.NewGuid(), EntryNumber = await Next("LED", date, db.CustomerLedgerEntries.Select(x => x.EntryNumber), ct),
            CustomerId = collection.CustomerId, TransactionDate = date, EntryType = "Credit", SourceType = "Collection",
            SourceId = collection.Id, ReferenceNumber = collection.CollectionNumber, Currency = collection.Currency,
            CreditAmount = collection.Amount, Description = collection.UnallocatedAmount > 0 ? "Müşteri tahsilatı ve avans" : "Müşteri tahsilatı", Created = DateTime.UtcNow
        };
        db.CustomerLedgerEntries.Add(ledger);
        var finance = await CreateTransaction(account, date, "CustomerCollection", "Inflow", "CustomerCollection", collection.Id,
            collection.Currency, collection.Amount, command.ExchangeRate, command.ReportingCurrency, command.PaymentMethod,
            customer.CompanyName ?? customer.Name, collection.CollectionNumber, command.ExternalReference, command.BankReference,
            $"CUSTOMER_COLLECTION:{reference}", customerId: customer.Id, ct: ct);
        collection.FinancialTransactionId = finance.Id;
        Audit("Atomic Customer Collection Recorded", collection.Id, new { collection.CollectionNumber, command.ExternalReference, collection.Amount, Allocated = allocations.Allocated, Advance = collection.UnallocatedAmount, command.ExchangeRate, command.ReportingCurrency });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, OperationId: collection.Id, OperationNumber: collection.CollectionNumber, FinancialTransactionId: finance.Id, AllocatedAmount: allocations.Allocated, AdvanceAmount: collection.UnallocatedAmount, AvailableBalance: account.OpeningBalance + await Movement(account.Id, ct), Currency: collection.Currency);
    }

    public async Task<AtomicFinanceResult> RecordSupplierPaymentAsync(AtomicSupplierPaymentCommand command, CancellationToken ct)
    {
        var error = ValidateCommon(command.Amount, command.Currency, command.ExternalReference, command.ExchangeRate, command.ReportingCurrency);
        if (error is not null) return error;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.SupplierId, ct);
        if (supplier is null) return AtomicFinanceResult.Fail("SUPPLIER_NOT_FOUND", "Tedarikçi bulunamadı.");
        var account = await LockedAccount(command.FinancialAccountId, ct);
        var accountError = ValidateAccount(account, command.Currency, command.PaymentMethod, true);
        if (accountError is not null) return accountError;
        var available = account!.OpeningBalance + await Movement(account.Id, ct);
        var canOverride = command.AllowNegativeBalance && currentUser.Roles.Contains("CEO") && !string.IsNullOrWhiteSpace(command.OverrideReason);
        if (available < command.Amount && !canOverride)
            return AtomicFinanceResult.Fail("INSUFFICIENT_BALANCE", "Seçilen kasa veya banka hesabında yeterli bakiye bulunmuyor.", available, command.Amount, account.Currency);
        var reference = NormalizeReference(command.ExternalReference);
        if (await db.FinancialTransactions.AnyAsync(x => x.BusinessReference == $"SUPPLIER_PAYMENT:{reference}" && !x.IsReversed, ct))
            return AtomicFinanceResult.Fail("DUPLICATE_OPERATION", "Bu referansla ödeme daha önce kaydedilmiş.");

        var date = command.TransactionDate.ToUniversalTime();
        var payment = new SupplierPayment
        {
            Id = Guid.NewGuid(), PaymentNumber = await Next("SPY", date, db.SupplierPayments.Select(x => x.PaymentNumber), ct),
            SupplierId = command.SupplierId, PaymentDate = date, Currency = command.Currency.ToUpperInvariant(), Amount = command.Amount,
            UnallocatedAmount = command.Amount, PaymentMethod = command.PaymentMethod, FinancialAccountId = account.Id,
            ReferenceNumber = command.ExternalReference.Trim(), BankReference = command.BankReference, Notes = command.Notes,
            Status = "Posted", FinancePostingStatus = "Posted"
        };
        db.SupplierPayments.Add(payment);
        var allocations = await SupplierAllocations(command, payment, ct);
        if (!allocations.Success) return allocations.Error!;
        if (payment.UnallocatedAmount > 0 && !command.AllowAdvance)
            return AtomicFinanceResult.Fail("ADVANCE_CONFIRMATION_REQUIRED", "Ödeme tutarı seçilen borçlardan fazladır. Kalan tutarı avans olarak kaydedebilirsiniz.");

        db.SupplierLedgerEntries.Add(new SupplierLedgerEntry
        {
            Id = Guid.NewGuid(), EntryNumber = await Next("SLE", date, db.SupplierLedgerEntries.Select(x => x.EntryNumber), ct),
            SupplierId = payment.SupplierId, TransactionDate = date, EntryType = "Debit", SourceType = "Payment", SourceId = payment.Id,
            ReferenceNumber = payment.PaymentNumber, Currency = payment.Currency, DebitAmount = payment.Amount,
            Description = payment.UnallocatedAmount > 0 ? "Tedarikçi ödemesi ve avans" : "Tedarikçi ödemesi", Created = DateTime.UtcNow
        });
        var purchaseOrderId = allocations.PurchaseOrderIds.Count == 1 ? allocations.PurchaseOrderIds[0] : null;
        var finance = await CreateTransaction(account, date, "SupplierPayment", "Outflow", "SupplierPayment", payment.Id,
            payment.Currency, payment.Amount, command.ExchangeRate, command.ReportingCurrency, command.PaymentMethod, supplier.Name,
            payment.PaymentNumber, command.ExternalReference, command.BankReference, $"SUPPLIER_PAYMENT:{reference}", supplierId: supplier.Id,
            purchaseOrderId: purchaseOrderId, ct: ct);
        payment.FinancialTransactionId = finance.Id;
        Audit("Atomic Supplier Payment Recorded", payment.Id, new { payment.PaymentNumber, command.ExternalReference, payment.Amount, Allocated = allocations.Allocated, Advance = payment.UnallocatedAmount, command.ExchangeRate, command.ReportingCurrency, command.AllowNegativeBalance, command.OverrideReason });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, OperationId: payment.Id, OperationNumber: payment.PaymentNumber, FinancialTransactionId: finance.Id, AllocatedAmount: allocations.Allocated, AdvanceAmount: payment.UnallocatedAmount, AvailableBalance: available - payment.Amount, Currency: payment.Currency);
    }

    public async Task<AccountBalanceResult> GetAvailableBalanceAsync(Guid accountId, bool lockAccount, CancellationToken ct)
    {
        var account = lockAccount ? await LockedAccount(accountId, ct) : await db.FinancialAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId, ct);
        return account is null
            ? new(false, accountId, null, null, null, 0, false)
            : new(true, account.Id, account.AccountCode, account.Name, account.Currency, account.OpeningBalance + await Movement(account.Id, ct), account.IsActive);
    }

    private async Task<(bool Success, decimal Allocated, List<Guid?> PurchaseOrderIds, AtomicFinanceResult? Error)> CustomerAllocations(AtomicCustomerCollectionCommand command, CustomerCollection collection, CancellationToken ct)
    {
        var selected = command.Allocations?.ToList() ?? [];
        List<CustomerReceivable> receivables;
        if (command.AutoAllocate)
            receivables = await db.CustomerReceivables.Where(x => x.CustomerId == command.CustomerId && x.Currency == collection.Currency && !x.IsCancelled && x.OutstandingAmount > 0).OrderBy(x => x.DueDate).ThenBy(x => x.TransactionDate).ThenBy(x => x.ReceivableNumber).ToListAsync(ct);
        else
        {
            if (selected.GroupBy(x => x.DocumentId).Any(x => x.Count() > 1)) return (false, 0, [], AtomicFinanceResult.Fail("DUPLICATE_ALLOCATION", "Aynı alacak birden fazla kez seçilemez."));
            var ids = selected.Select(x => x.DocumentId).ToList();
            receivables = await db.CustomerReceivables.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            if (receivables.Count != ids.Count) return (false, 0, [], AtomicFinanceResult.Fail("RECEIVABLE_NOT_FOUND", "Seçilen alacak bulunamadı."));
        }
        decimal allocated = 0;
        foreach (var receivable in receivables)
        {
            if (receivable.CustomerId != collection.CustomerId) return (false, 0, [], AtomicFinanceResult.Fail("CUSTOMER_MISMATCH", "Tahsilat ve alacak aynı müşteriye ait olmalıdır."));
            if (receivable.Currency != collection.Currency) return (false, 0, [], AtomicFinanceResult.Fail("CURRENCY_MISMATCH", "Tahsilat ve alacak para birimleri eşleşmiyor."));
            var requested = command.AutoAllocate ? Math.Min(collection.UnallocatedAmount, receivable.OutstandingAmount) : selected.Single(x => x.DocumentId == receivable.Id).Amount;
            if (requested <= 0 || requested > receivable.OutstandingAmount || requested > collection.UnallocatedAmount) return (false, 0, [], AtomicFinanceResult.Fail("ALLOCATION_EXCEEDED", "Dağıtım tutarı açık alacak veya tahsilat tutarını aşıyor."));
            db.CollectionAllocations.Add(new CollectionAllocation { Id = Guid.NewGuid(), CustomerCollectionId = collection.Id, CustomerReceivableId = receivable.Id, OrderId = receivable.OrderId, AllocatedAmount = requested, Currency = collection.Currency, AllocationDate = DateTime.UtcNow, Notes = command.AutoAllocate ? "Otomatik FIFO dağıtım" : selected.Single(x => x.DocumentId == receivable.Id).Notes, Created = DateTime.UtcNow });
            receivable.AllocatedAmount += requested; receivable.OutstandingAmount -= requested; receivable.Status = ReceivableStatus(receivable);
            collection.UnallocatedAmount -= requested; allocated += requested;
            if (collection.UnallocatedAmount == 0) break;
        }
        collection.Status = collection.UnallocatedAmount == 0 ? "FullyAllocated" : allocated > 0 ? "PartiallyAllocated" : "Posted";
        return (true, allocated, [], null);
    }

    private async Task<(bool Success, decimal Allocated, List<Guid?> PurchaseOrderIds, AtomicFinanceResult? Error)> SupplierAllocations(AtomicSupplierPaymentCommand command, SupplierPayment payment, CancellationToken ct)
    {
        var selected = command.Allocations?.ToList() ?? [];
        List<SupplierPayable> payables;
        if (command.AutoAllocate)
            payables = await db.SupplierPayables.Where(x => x.SupplierId == command.SupplierId && x.Currency == payment.Currency && !x.IsCancelled && x.OutstandingAmount > 0).OrderBy(x => x.DueDate).ThenBy(x => x.TransactionDate).ThenBy(x => x.PayableNumber).ToListAsync(ct);
        else
        {
            if (selected.GroupBy(x => x.DocumentId).Any(x => x.Count() > 1)) return (false, 0, [], AtomicFinanceResult.Fail("DUPLICATE_ALLOCATION", "Aynı borç birden fazla kez seçilemez."));
            var ids = selected.Select(x => x.DocumentId).ToList();
            payables = await db.SupplierPayables.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            if (payables.Count != ids.Count) return (false, 0, [], AtomicFinanceResult.Fail("PAYABLE_NOT_FOUND", "Seçilen borç bulunamadı."));
        }
        decimal allocated = 0; var orders = new List<Guid?>();
        foreach (var payable in payables)
        {
            if (payable.SupplierId != payment.SupplierId) return (false, 0, [], AtomicFinanceResult.Fail("SUPPLIER_MISMATCH", "Ödeme ve borç aynı tedarikçiye ait olmalıdır."));
            if (payable.Currency != payment.Currency) return (false, 0, [], AtomicFinanceResult.Fail("CURRENCY_MISMATCH", "Ödeme ve borç para birimleri eşleşmiyor."));
            var requested = command.AutoAllocate ? Math.Min(payment.UnallocatedAmount, payable.OutstandingAmount) : selected.Single(x => x.DocumentId == payable.Id).Amount;
            if (requested <= 0 || requested > payable.OutstandingAmount || requested > payment.UnallocatedAmount) return (false, 0, [], AtomicFinanceResult.Fail("ALLOCATION_EXCEEDED", "Dağıtım tutarı açık borç veya ödeme tutarını aşıyor."));
            db.SupplierPaymentAllocations.Add(new SupplierPaymentAllocation { Id = Guid.NewGuid(), SupplierPaymentId = payment.Id, SupplierPayableId = payable.Id, PurchaseOrderId = payable.PurchaseOrderId, AllocatedAmount = requested, Currency = payment.Currency, AllocationDate = DateTime.UtcNow, Notes = command.AutoAllocate ? "Otomatik FIFO dağıtım" : selected.Single(x => x.DocumentId == payable.Id).Notes, Created = DateTime.UtcNow });
            payable.AllocatedAmount += requested; payable.OutstandingAmount -= requested; payable.Status = PayableStatus(payable);
            payment.UnallocatedAmount -= requested; allocated += requested; orders.Add(payable.PurchaseOrderId);
            if (payment.UnallocatedAmount == 0) break;
        }
        payment.Status = payment.UnallocatedAmount == 0 ? "FullyAllocated" : allocated > 0 ? "PartiallyAllocated" : "Posted";
        return (true, allocated, orders.Distinct().ToList(), null);
    }

    private async Task<FinancialAccount?> LockedAccount(Guid id, CancellationToken ct)
    {
        if (db.Database.IsNpgsql()) return await db.FinancialAccounts.FromSqlInterpolated($"SELECT * FROM \"FinancialAccounts\" WHERE \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        return await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    private async Task<decimal> Movement(Guid accountId, CancellationToken ct) => await db.FinancialTransactions.Where(x => x.FinancialAccountId == accountId && !x.IsReversed && x.AffectsBalance).SumAsync(x => (decimal?)(x.Direction == "Inflow" ? x.Amount : -x.Amount), ct) ?? 0;

    private async Task<FinancialTransaction> CreateTransaction(FinancialAccount account, DateTime date, string type, string direction, string sourceType, Guid sourceId, string currency, decimal amount, decimal rate, string reportingCurrency, string method, string party, string document, string reference, string? bankReference, string businessReference, Guid? customerId = null, Guid? supplierId = null, Guid? purchaseOrderId = null, CancellationToken ct = default)
    {
        var entity = new FinancialTransaction { Id = Guid.NewGuid(), TransactionNumber = await Next("FTX", date, db.FinancialTransactions.Select(x => x.TransactionNumber), ct), FinancialAccountId = account.Id, TransactionDate = date, TransactionType = type, Direction = direction, SourceType = sourceType, SourceId = sourceId, CustomerId = customerId, SupplierId = supplierId, PurchaseOrderId = purchaseOrderId, Currency = currency, Amount = amount, ExchangeRate = rate, ReportingCurrency = reportingCurrency.ToUpperInvariant(), ReportingAmount = decimal.Round(amount * rate, 4), PaymentMethod = method, CounterpartyName = party, DocumentNumber = document, ReferenceNumber = reference, BankReference = bankReference, BusinessReference = businessReference, AffectsBalance = true, Description = document, Created = DateTime.UtcNow };
        db.FinancialTransactions.Add(entity); return entity;
    }

    private async Task<string> Next(string prefix, DateTime date, IQueryable<string> source, CancellationToken ct)
    {
        if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81311)", ct);
        var full = $"{prefix}-{date:yyyyMMdd}-"; var values = await source.Where(x => x.StartsWith(full)).ToListAsync(ct);
        var number = values.Select(x => int.TryParse(x[full.Length..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
        return $"{full}{number:0000}";
    }

    private static AtomicFinanceResult? ValidateCommon(decimal amount, string currency, string reference, decimal rate, string reportingCurrency)
    {
        if (amount <= 0) return AtomicFinanceResult.Fail("VALIDATION_ERROR", "Tutar sıfırdan büyük olmalıdır.");
        if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(reportingCurrency)) return AtomicFinanceResult.Fail("CURRENCY_REQUIRED", "Para birimi zorunludur.");
        if (string.IsNullOrWhiteSpace(reference)) return AtomicFinanceResult.Fail("REFERENCE_REQUIRED", "Tekil işlem referansı zorunludur.");
        if (rate <= 0) return AtomicFinanceResult.Fail("EXCHANGE_RATE_REQUIRED", "Kur sıfırdan büyük olmalıdır.");
        return null;
    }

    private static AtomicFinanceResult? ValidateAccount(FinancialAccount? account, string currency, string method, bool outflow)
    {
        if (account is null) return AtomicFinanceResult.Fail("ACCOUNT_NOT_FOUND", "Finans hesabı bulunamadı.");
        if (!account.IsActive) return AtomicFinanceResult.Fail("ACCOUNT_INACTIVE", "Finans hesabı aktif değil.");
        if (account.Currency != currency.ToUpperInvariant()) return AtomicFinanceResult.Fail("CURRENCY_MISMATCH", "Finans hesabının para birimi işlem ile eşleşmiyor.");
        if (method == "Cash" && account.AccountType != "Cash") return AtomicFinanceResult.Fail("ACCOUNT_TYPE", "Nakit işlem için kasa hesabı seçilmelidir.");
        if (method == "BankTransfer" && account.AccountType != "Bank") return AtomicFinanceResult.Fail("ACCOUNT_TYPE", "Havale işlemi için banka hesabı seçilmelidir.");
        if (outflow && method is not ("Cash" or "BankTransfer" or "CreditCard" or "Other")) return AtomicFinanceResult.Fail("PAYMENT_METHOD", "Bu ödeme yöntemi atomik kasa çıkışı için desteklenmiyor.");
        return null;
    }

    private void Audit(string entity, Guid id, object values) => db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserId = currentUser.UserId, UserName = currentUser.UserName ?? "system", Action = AuditAction.Create, EntityName = entity, EntityId = id.ToString(), NewValues = JsonSerializer.Serialize(values), Timestamp = DateTime.UtcNow, IpAddress = currentUser.IpAddress });
    private static string NormalizeReference(string value) => value.Trim().ToUpperInvariant();
    private static string ReceivableStatus(CustomerReceivable x) => x.OutstandingAmount == 0 ? "Paid" : x.AllocatedAmount > 0 ? "PartiallyPaid" : x.DueDate.Date < DateTime.UtcNow.Date ? "Overdue" : "Open";
    private static string PayableStatus(SupplierPayable x) => x.OutstandingAmount == 0 ? "Paid" : x.AllocatedAmount > 0 ? "PartiallyPaid" : x.DueDate.Date < DateTime.UtcNow.Date ? "Overdue" : "Open";
}
