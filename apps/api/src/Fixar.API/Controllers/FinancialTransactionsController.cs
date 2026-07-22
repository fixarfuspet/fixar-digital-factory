using System.Text;
using Asp.Versioning;
using Fixar.API.Security;
using Fixar.Application.Common.Models;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Route("api/v{version:apiVersion}/financial-transactions"), Authorize(Policy = AuthorizationPolicies.CanViewFinancialAccounts)]
public class FinancialTransactionsController(ApplicationDbContext db, IAtomicFinanceService atomicFinance) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid? accountId, string? direction, string? transactionType, string? sourceType, string? currency, DateTime? dateFrom, DateTime? dateTo, string? search, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 500);
        var query = db.FinancialTransactions.AsNoTracking().AsQueryable();
        if (accountId.HasValue) query = query.Where(x => x.FinancialAccountId == accountId);
        if (!string.IsNullOrWhiteSpace(direction)) query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrWhiteSpace(transactionType)) query = query.Where(x => x.TransactionType == transactionType);
        if (!string.IsNullOrWhiteSpace(sourceType)) query = query.Where(x => x.SourceType == sourceType);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency.ToUpper());
        if (dateFrom.HasValue) query = query.Where(x => x.TransactionDate >= dateFrom.Value.ToUniversalTime());
        if (dateTo.HasValue) query = query.Where(x => x.TransactionDate < dateTo.Value.Date.AddDays(1).ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.ToLower();
            query = query.Where(x => x.TransactionNumber.ToLower().Contains(value) || (x.ReferenceNumber != null && x.ReferenceNumber.ToLower().Contains(value)) || (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(value)) || (x.CounterpartyName != null && x.CounterpartyName.ToLower().Contains(value)) || (x.Description != null && x.Description.ToLower().Contains(value)));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Created).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.Id, x.TransactionNumber, x.FinancialAccountId, AccountCode = x.FinancialAccount.AccountCode,
            AccountName = x.FinancialAccount.Name, x.TransactionDate, x.ValueDate, x.TransactionType,
            x.Direction, x.SourceType, x.CustomerId, x.SupplierId, x.FinanceCategoryId,
            CategoryName = x.FinanceCategory != null ? x.FinanceCategory.Name : null,
            x.Currency, x.Amount, x.ExchangeRate, x.ReportingCurrency, x.ReportingAmount,
            x.PaymentMethod, x.CounterpartyName, x.DocumentNumber, x.Description, x.ReferenceNumber, x.IsReversed
        }).ToListAsync(ct);
        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var row = await db.FinancialTransactions.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.TransactionNumber, x.FinancialAccountId, Account = x.FinancialAccount.Name,
            x.TransactionDate, x.ValueDate, x.TransactionType, x.Direction, x.SourceType, x.SourceId,
            x.CustomerId, x.SupplierId, x.FinanceCategoryId, Category = x.FinanceCategory != null ? x.FinanceCategory.Name : null,
            x.PurchaseOrderId, x.OrderId, x.CustomerCollectionId, x.ChequeId, x.Currency, x.Amount,
            x.ExchangeRate, x.ReportingCurrency, x.ReportingAmount, x.PaymentMethod, x.CounterpartyName,
            x.DocumentNumber, x.BusinessReference, x.Description, x.ReferenceNumber, x.BankReference,
            x.IsReversed, x.ReversalTransactionId, x.ReversedAt, x.ReversedBy, x.ReversalReason
        }).FirstOrDefaultAsync(ct);
        return row is null ? NotFound(ApiResponse<object>.Fail("Finansal hareket bulunamadı.", "NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(row));
    }

    [HttpPost("manual-income"), Authorize(Policy = AuthorizationPolicies.CanRecordFinancialTransactions), Idempotent]
    public Task<IActionResult> Income(ManualTransactionRequest request, CancellationToken ct) => Manual(request, "Inflow", "ManualIncome", ct);

    [HttpPost("manual-expense"), Authorize(Policy = AuthorizationPolicies.CanRecordFinancialTransactions), Idempotent]
    public Task<IActionResult> Expense(ManualTransactionRequest request, CancellationToken ct) => Manual(request, "Outflow", "ManualExpense", ct);

    [HttpPost("transfer"), Authorize(Policy = AuthorizationPolicies.CanRecordFinancialTransactions), Idempotent]
    public async Task<IActionResult> Transfer(TransferRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0) return BadRequest(ApiResponse<object>.Fail("Tutar sıfırdan büyük olmalıdır.", "VALIDATION_ERROR"));
        if (request.SourceAccountId == request.TargetAccountId) return BadRequest(ApiResponse<object>.Fail("Kaynak ve hedef hesap aynı olamaz.", "SAME_ACCOUNT"));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var accounts = await db.FinancialAccounts.Where(x => x.Id == request.SourceAccountId || x.Id == request.TargetAccountId).ToListAsync(ct);
        var source = accounts.FirstOrDefault(x => x.Id == request.SourceAccountId); var target = accounts.FirstOrDefault(x => x.Id == request.TargetAccountId);
        if (source is null || target is null) return NotFound(ApiResponse<object>.Fail("Finans hesabı bulunamadı.", "ACCOUNT_NOT_FOUND"));
        if (!source.IsActive || !target.IsActive) return Conflict(ApiResponse<object>.Fail("Finans hesabı aktif değil.", "ACCOUNT_INACTIVE"));
        if (source.Currency != target.Currency) return Conflict(ApiResponse<object>.Fail("Farklı para birimleri arasında transfer için kur dönüşümlü işlem kullanılmalıdır.", "CURRENCY_MISMATCH"));
        var balance = await Balance(source.Id, ct);
        if (balance < request.Amount && !CanOverrideNegative(request.AllowNegativeBalance, request.OverrideReason)) return Conflict(ApiResponse<object>.Fail("Seçilen kasada yeterli bakiye bulunmuyor.", "INSUFFICIENT_BALANCE"));
        var reference = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? $"TRF-{Guid.NewGuid():N}" : request.ReferenceNumber.Trim();
        var outgoing = await Create(source, "Outflow", "AccountTransfer", "AccountTransfer", null, request.Amount, request.TransactionDate, request.Description, reference, ct, businessReference: $"TRANSFER:{reference}:OUT", paymentMethod: "Transfer", counterparty: target.Name);
        var incoming = await Create(target, "Inflow", "AccountTransfer", "AccountTransfer", outgoing.Id, request.Amount, request.TransactionDate, request.Description, reference, ct, businessReference: $"TRANSFER:{reference}:IN", paymentMethod: "Transfer", counterparty: source.Name);
        outgoing.SourceId = incoming.Id;
        CustomerFinanceSupport.Audit(db, this, "Account Transfer Created", outgoing.Id, new { request.SourceAccountId, request.TargetAccountId, request.Amount, source.Currency, reference, request.AllowNegativeBalance, request.OverrideReason });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { reference, SourceTransactionId = outgoing.Id, TargetTransactionId = incoming.Id }));
    }

    [HttpPost("{id:guid}/reverse"), Authorize(Policy = AuthorizationPolicies.CanReverseFinancialTransactions), Idempotent]
    public async Task<IActionResult> Reverse(Guid id, ReverseFinancialRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.Fail("Geri alma gerekçesi zorunludur.", "VALIDATION_ERROR"));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var original = await db.FinancialTransactions.Include(x => x.FinancialAccount).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (original is null) return NotFound();
        if (original.IsReversed || original.TransactionType == "Reversal") return Conflict(ApiResponse<object>.Fail("Bu finansal hareket daha önce geri alınmış veya ters kayıttır.", "ALREADY_REVERSED"));
        var reverse = await Create(original.FinancialAccount, original.Direction == "Inflow" ? "Outflow" : "Inflow", "Reversal", "Reversal", original.Id, original.Amount, DateTime.UtcNow, request.Reason, original.ReferenceNumber, ct, customerId: original.CustomerId, supplierId: original.SupplierId, categoryId: original.FinanceCategoryId, exchangeRate: original.ExchangeRate, reportingCurrency: original.ReportingCurrency, businessReference: $"REVERSAL:{original.Id:N}", paymentMethod: original.PaymentMethod, counterparty: original.CounterpartyName, documentNumber: original.DocumentNumber, purchaseOrderId: original.PurchaseOrderId, orderId: original.OrderId);
        original.IsReversed = true; original.ReversedAt = DateTime.UtcNow; original.ReversedBy = User.Identity?.Name;
        original.ReversalReason = request.Reason; original.ReversalTransactionId = reverse.Id;
        CustomerFinanceSupport.Audit(db, this, "Financial Transaction Reversed", original.Id, request);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { original.Id, ReversalTransactionId = reverse.Id }));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date; var month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = await db.FinancialTransactions.AsNoTracking().Where(x => !x.IsReversed && x.AffectsBalance).GroupBy(x => x.Currency).Select(g => new
        {
            Currency = g.Key,
            TodayIn = g.Where(x => x.TransactionDate >= today && x.Direction == "Inflow").Sum(x => (decimal?)x.Amount) ?? 0,
            TodayOut = g.Where(x => x.TransactionDate >= today && x.Direction == "Outflow").Sum(x => (decimal?)x.Amount) ?? 0,
            MonthIn = g.Where(x => x.TransactionDate >= month && x.Direction == "Inflow").Sum(x => (decimal?)x.Amount) ?? 0,
            MonthOut = g.Where(x => x.TransactionDate >= month && x.Direction == "Outflow").Sum(x => (decimal?)x.Amount) ?? 0
        }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("dashboard"), Authorize(Policy = AuthorizationPolicies.CanViewCashFlow)]
    public async Task<IActionResult> Dashboard(DateTime? dateFrom, DateTime? dateTo, string reportingCurrency = "TRY", CancellationToken ct = default)
    {
        var from = (dateFrom ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).ToUniversalTime();
        var to = (dateTo ?? DateTime.UtcNow.Date).Date.AddDays(1).ToUniversalTime();
        var transactions = await db.FinancialTransactions.AsNoTracking().Where(x => !x.IsReversed && x.AffectsBalance && x.TransactionDate >= from && x.TransactionDate < to && x.ReportingCurrency == reportingCurrency.ToUpper()).Select(x => new { x.TransactionDate, x.Direction, x.ReportingAmount, x.FinanceCategoryId, CategoryName = x.FinanceCategory != null ? x.FinanceCategory.Name : "Kategorisiz", x.CustomerId, x.SupplierId, x.CounterpartyName }).ToListAsync(ct);
        var accounts = await db.FinancialAccounts.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.AccountCode, x.Name, x.AccountType, x.Currency, Balance = x.OpeningBalance + (db.FinancialTransactions.Where(t => t.FinancialAccountId == x.Id && !t.IsReversed && t.AffectsBalance).Sum(t => (decimal?)(t.Direction == "Inflow" ? t.Amount : -t.Amount)) ?? 0) }).ToListAsync(ct);
        var receivables = await db.CustomerReceivables.AsNoTracking().Where(x => !x.IsCancelled).GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Outstanding = g.Sum(x => x.OutstandingAmount), Overdue = g.Where(x => x.DueDate < DateTime.UtcNow.Date).Sum(x => x.OutstandingAmount) }).ToListAsync(ct);
        var payables = await db.SupplierPayables.AsNoTracking().Where(x => !x.IsCancelled).GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Outstanding = g.Sum(x => x.OutstandingAmount), Overdue = g.Where(x => x.DueDate < DateTime.UtcNow.Date).Sum(x => x.OutstandingAmount) }).ToListAsync(ct);
        var daily = transactions.GroupBy(x => x.TransactionDate.Date).OrderBy(x => x.Key).Select(g => new { Date = g.Key, Income = g.Where(x => x.Direction == "Inflow").Sum(x => x.ReportingAmount), Expense = g.Where(x => x.Direction == "Outflow").Sum(x => x.ReportingAmount) }).ToList();
        var expenses = transactions.Where(x => x.Direction == "Outflow").GroupBy(x => new { x.FinanceCategoryId, x.CategoryName }).Select(g => new { g.Key.FinanceCategoryId, g.Key.CategoryName, Amount = g.Sum(x => x.ReportingAmount) }).OrderByDescending(x => x.Amount).ToList();
        var topCustomers = transactions.Where(x => x.Direction == "Inflow" && x.CustomerId.HasValue).GroupBy(x => new { x.CustomerId, x.CounterpartyName }).Select(g => new { g.Key.CustomerId, Name = g.Key.CounterpartyName, Amount = g.Sum(x => x.ReportingAmount), Count = g.Count() }).OrderByDescending(x => x.Amount).Take(10).ToList();
        var topSuppliers = transactions.Where(x => x.Direction == "Outflow" && x.SupplierId.HasValue).GroupBy(x => new { x.SupplierId, x.CounterpartyName }).Select(g => new { g.Key.SupplierId, Name = g.Key.CounterpartyName, Amount = g.Sum(x => x.ReportingAmount), Count = g.Count() }).OrderByDescending(x => x.Amount).Take(10).ToList();
        return Ok(ApiResponse<object>.SuccessResponse(new { ReportingCurrency = reportingCurrency.ToUpper(), DateFrom = from, DateTo = to, TotalIncome = transactions.Where(x => x.Direction == "Inflow").Sum(x => x.ReportingAmount), TotalExpense = transactions.Where(x => x.Direction == "Outflow").Sum(x => x.ReportingAmount), NetCashFlow = transactions.Sum(x => x.Direction == "Inflow" ? x.ReportingAmount : -x.ReportingAmount), Accounts = accounts, Receivables = receivables, Payables = payables, Daily = daily, ExpenseCategories = expenses, TopCustomers = topCustomers, TopSuppliers = topSuppliers }));
    }

    [HttpGet("collection-backfill-preview")]
    public async Task<IActionResult> CollectionBackfillPreview(CancellationToken ct)
    {
        var query = db.CustomerCollections.AsNoTracking();
        var methods = await query.GroupBy(x => x.PaymentMethod).Select(g => new { PaymentMethod = g.Key, Count = g.Count() }).ToListAsync(ct);
        var candidates = await query.Where(x => x.Status != "Draft" && x.Status != "Cancelled" && !x.IsReversed && !x.FinancialTransactionId.HasValue && !x.CustomerChequeId.HasValue).Select(x => new { x.Id, x.CollectionNumber, x.CustomerId, x.CollectionDate, x.PaymentMethod, x.Currency, x.Amount, Warning = "Finans hesabı açıkça seçilmelidir." }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { Total = await query.CountAsync(ct), PaymentMethods = methods, WithoutFinancialPosting = candidates.Count, Candidates = candidates }));
    }

    [HttpGet("cash-flow"), Authorize(Policy = AuthorizationPolicies.CanViewCashFlow)]
    public async Task<IActionResult> CashFlow(DateTime? dateFrom, DateTime? dateTo, string? currency, CancellationToken ct)
    {
        var from = (dateFrom ?? DateTime.UtcNow.Date.AddDays(-30)).Date; var to = (dateTo ?? DateTime.UtcNow).Date.AddDays(1);
        var query = db.FinancialTransactions.AsNoTracking().Where(x => !x.IsReversed && x.AffectsBalance && x.TransactionDate >= from && x.TransactionDate < to);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency.ToUpper());
        var raw = await query.Select(x => new { x.TransactionDate, x.Currency, x.Direction, x.TransactionType, x.Amount }).ToListAsync(ct);
        var rows = raw.GroupBy(x => new { x.TransactionDate.Date, x.Currency }).Select(g => new { Date = g.Key.Date, g.Key.Currency, Inflow = g.Where(x => x.Direction == "Inflow").Sum(x => x.Amount), Outflow = g.Where(x => x.Direction == "Outflow").Sum(x => x.Amount), NetCashFlow = g.Where(x => x.TransactionType != "AccountTransfer").Sum(x => x.Direction == "Inflow" ? x.Amount : -x.Amount), TransactionCount = g.Count() }).OrderBy(x => x.Date).ToList();
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("export.csv"), Authorize(Policy = AuthorizationPolicies.CanViewCashFlow)]
    public async Task<IActionResult> Export(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var from = (dateFrom ?? DateTime.UtcNow.Date.AddMonths(-1)).ToUniversalTime(); var to = (dateTo ?? DateTime.UtcNow.Date).Date.AddDays(1).ToUniversalTime();
        var rows = await db.FinancialTransactions.AsNoTracking().Where(x => x.TransactionDate >= from && x.TransactionDate < to).OrderByDescending(x => x.TransactionDate).Select(x => new { x.TransactionNumber, x.TransactionDate, Account = x.FinancialAccount.Name, x.Direction, x.TransactionType, Category = x.FinanceCategory != null ? x.FinanceCategory.Name : null, x.CounterpartyName, x.DocumentNumber, x.ReferenceNumber, x.Amount, x.Currency, x.ExchangeRate, x.ReportingAmount, x.ReportingCurrency, x.IsReversed }).ToListAsync(ct);
        var output = new StringBuilder("\uFEFFİşlem No;Tarih;Hesap;Yön;Tür;Kategori;Muhatap;Belge;Referans;Tutar;Para Birimi;Kur;Raporlama Tutarı;Raporlama Para Birimi;Ters Kayıt\n");
        foreach (var x in rows) output.AppendLine($"{x.TransactionNumber};{x.TransactionDate:dd.MM.yyyy};{Clean(x.Account)};{x.Direction};{x.TransactionType};{Clean(x.Category)};{Clean(x.CounterpartyName)};{Clean(x.DocumentNumber)};{Clean(x.ReferenceNumber)};{x.Amount};{x.Currency};{x.ExchangeRate};{x.ReportingAmount};{x.ReportingCurrency};{x.IsReversed}");
        CustomerFinanceSupport.Audit(db, this, "Cash Flow Exported", Guid.Empty, new { dateFrom, dateTo }); await db.SaveChangesAsync(ct);
        return File(Encoding.UTF8.GetBytes(output.ToString()), "text/csv; charset=utf-8", $"finansal-hareketler-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task<IActionResult> Manual(ManualTransactionRequest request, string direction, string type, CancellationToken ct)
    {
        if (request.Amount <= 0) return BadRequest(ApiResponse<object>.Fail("Tutar sıfırdan büyük olmalıdır.", "VALIDATION_ERROR"));
        if (direction == "Outflow" && !request.FinanceCategoryId.HasValue) return BadRequest(ApiResponse<object>.Fail("Gider kategorisi seçiniz.", "CATEGORY_REQUIRED"));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        var account = await db.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == request.FinancialAccountId, ct);
        if (account is null) return NotFound(ApiResponse<object>.Fail("Kasa veya banka hesabı seçiniz.", "ACCOUNT_NOT_FOUND"));
        if (!account.IsActive) return Conflict(ApiResponse<object>.Fail("Finans hesabı aktif değil.", "ACCOUNT_INACTIVE"));
        if (!string.IsNullOrWhiteSpace(request.Currency) && account.Currency != request.Currency.ToUpper()) return Conflict(ApiResponse<object>.Fail("Hesap ve işlem para birimi eşleşmiyor.", "CURRENCY_MISMATCH"));
        FinanceCategory? category = null;
        if (request.FinanceCategoryId.HasValue)
        {
            category = await db.FinanceCategories.FirstOrDefaultAsync(x => x.Id == request.FinanceCategoryId && x.IsActive, ct);
            if (category is null || category.CategoryType != (direction == "Outflow" ? "Expense" : "Income")) return Conflict(ApiResponse<object>.Fail("Gelir/gider kategorisi işlem yönüyle eşleşmiyor.", "CATEGORY_MISMATCH"));
        }
        var exchangeRate = account.Currency == request.ReportingCurrency.ToUpper() ? 1m : request.ExchangeRate ?? 0;
        if (exchangeRate <= 0) return BadRequest(ApiResponse<object>.Fail("Dövizli işlemde kur sıfırdan büyük olmalıdır.", "EXCHANGE_RATE_REQUIRED"));
        if (request.CustomerId.HasValue && !await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct)) return NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND"));
        if (request.SupplierId.HasValue && !await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId, ct)) return NotFound(ApiResponse<object>.Fail("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND"));
        if (direction == "Outflow" && await Balance(account.Id, ct) < request.Amount && !CanOverrideNegative(request.AllowNegativeBalance, request.OverrideReason)) return Conflict(ApiResponse<object>.Fail("Seçilen kasada yeterli bakiye bulunmuyor.", "INSUFFICIENT_BALANCE"));
        var businessReference = !string.IsNullOrWhiteSpace(request.BusinessReference) ? request.BusinessReference.Trim().ToUpperInvariant() : !string.IsNullOrWhiteSpace(request.ReferenceNumber) ? $"MANUAL:{direction}:{request.ReferenceNumber.Trim().ToUpperInvariant()}" : $"MANUAL:{Guid.NewGuid():N}";
        var counterparty = request.CounterpartyName;
        if (request.CustomerId.HasValue) counterparty = await db.Customers.Where(x => x.Id == request.CustomerId).Select(x => x.CompanyName ?? x.Name).FirstAsync(ct);
        if (request.SupplierId.HasValue) counterparty = await db.Suppliers.Where(x => x.Id == request.SupplierId).Select(x => x.Name).FirstAsync(ct);
        var entity = await Create(account, direction, type, "Manual", Guid.NewGuid(), request.Amount, request.TransactionDate, request.Description, request.ReferenceNumber, ct, request.CustomerId, request.SupplierId, request.FinanceCategoryId, exchangeRate, request.ReportingCurrency, businessReference, request.PaymentMethod, counterparty, request.DocumentNumber, request.PurchaseOrderId, request.OrderId);
        CustomerFinanceSupport.Audit(db, this, "Financial Transaction Created", entity.Id, new { request, direction, type });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.TransactionNumber, entity.ReportingAmount, entity.ReportingCurrency }));
    }

    private async Task<decimal> Balance(Guid accountId, CancellationToken ct)
    {
        var account = await atomicFinance.GetAvailableBalanceAsync(accountId, true, ct);
        if (!account.Found) throw new InvalidOperationException("Finans hesabı bulunamadı.");
        return account.AvailableBalance;
    }

    private bool CanOverrideNegative(bool allow, string? reason) => allow && User.IsInRole(RoleNames.CEO) && !string.IsNullOrWhiteSpace(reason);

    private async Task<FinancialTransaction> Create(FinancialAccount account, string direction, string type, string sourceType, Guid? sourceId, decimal amount, DateTime date, string? description, string? reference, CancellationToken ct, Guid? customerId = null, Guid? supplierId = null, Guid? categoryId = null, decimal exchangeRate = 1m, string reportingCurrency = "TRY", string? businessReference = null, string? paymentMethod = null, string? counterparty = null, string? documentNumber = null, Guid? purchaseOrderId = null, Guid? orderId = null)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81311)", ct);
        var normalizedDate = date.ToUniversalTime(); var prefix = $"FTX-{normalizedDate:yyyyMMdd}-";
        var values = await db.FinancialTransactions.Where(x => x.TransactionNumber.StartsWith(prefix)).Select(x => x.TransactionNumber).ToListAsync(ct);
        var next = values.Select(x => int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty().Max() + 1;
        var entity = new FinancialTransaction
        {
            Id = Guid.NewGuid(), TransactionNumber = $"{prefix}{next:0000}", FinancialAccountId = account.Id,
            FinancialAccount = account, TransactionDate = normalizedDate, TransactionType = type, Direction = direction,
            SourceType = sourceType, SourceId = sourceId, CustomerId = customerId, SupplierId = supplierId,
            FinanceCategoryId = categoryId, PurchaseOrderId = purchaseOrderId, OrderId = orderId,
            Currency = account.Currency, Amount = amount, ExchangeRate = exchangeRate,
            ReportingCurrency = reportingCurrency.ToUpperInvariant(), ReportingAmount = decimal.Round(amount * exchangeRate, 4),
            PaymentMethod = paymentMethod, CounterpartyName = counterparty, DocumentNumber = documentNumber,
            BusinessReference = businessReference, Description = description, ReferenceNumber = reference,
            AffectsBalance = true, Created = DateTime.UtcNow
        };
        db.FinancialTransactions.Add(entity); return entity;
    }

    private static string? Clean(string? value) => value?.Replace(';', ',').Replace('\n', ' ');
}

public record ManualTransactionRequest(Guid FinancialAccountId, DateTime TransactionDate, decimal Amount, Guid? CustomerId, string? Description, string? ReferenceNumber, Guid? FinanceCategoryId = null, Guid? SupplierId = null, Guid? PurchaseOrderId = null, Guid? OrderId = null, string? Currency = null, decimal? ExchangeRate = null, string ReportingCurrency = "TRY", string? PaymentMethod = null, string? CounterpartyName = null, string? DocumentNumber = null, string? BusinessReference = null, bool AllowNegativeBalance = false, string? OverrideReason = null);
public record TransferRequest(Guid SourceAccountId, Guid TargetAccountId, DateTime TransactionDate, decimal Amount, string? Description, string? ReferenceNumber, bool AllowNegativeBalance = false, string? OverrideReason = null);
public record ReverseFinancialRequest(string Reason);
