using Fixar.Application.Common.Interfaces;
using Fixar.API.Controllers;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Fixar.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class AtomicFinanceWorkflowTests
{
    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(40, 40, 0)]
    [InlineData(130, 100, 30)]
    public async Task Customer_collection_updates_receivable_cash_and_advance_atomically(decimal amount, decimal allocated, decimal advance)
    {
        await using var seed = await Seed(); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordCustomerCollectionAsync(CustomerCommand(seed, amount, "COL-CASE-" + amount), default);
        Assert.True(result.Success); Assert.Equal(allocated, result.AllocatedAmount); Assert.Equal(advance, result.AdvanceAmount);
        Assert.Equal(100 - allocated, (await seed.Db.CustomerReceivables.SingleAsync()).OutstandingAmount);
        Assert.Equal(amount, (await seed.Db.FinancialTransactions.SingleAsync()).Amount);
        Assert.Equal(amount, (await seed.Db.CustomerLedgerEntries.SingleAsync()).CreditAmount);
    }

    [Fact]
    public async Task Customer_collection_allocates_multiple_receivables_oldest_first()
    {
        await using var seed = await Seed(secondDocument: true); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordCustomerCollectionAsync(CustomerCommand(seed, 120, "COL-FIFO"), default);
        Assert.True(result.Success); var receivables = await seed.Db.CustomerReceivables.OrderBy(x => x.DueDate).ToListAsync();
        Assert.Equal(0, receivables[0].OutstandingAmount); Assert.Equal(30, receivables[1].OutstandingAmount); Assert.Equal(2, await seed.Db.CollectionAllocations.CountAsync());
    }

    [Fact]
    public async Task Customer_collection_supports_selected_allocation()
    {
        await using var seed = await Seed(secondDocument: true); var service = new AtomicFinanceService(seed.Db, seed.User); var target = await seed.Db.CustomerReceivables.OrderByDescending(x => x.DueDate).FirstAsync();
        var command = CustomerCommand(seed, 20, "COL-SELECT") with { AutoAllocate = false, Allocations = [new(target.Id, 20)] };
        var result = await service.RecordCustomerCollectionAsync(command, default);
        Assert.True(result.Success); Assert.Equal(30, target.OutstandingAmount); Assert.Equal(100, (await seed.Db.CustomerReceivables.OrderBy(x => x.DueDate).FirstAsync()).OutstandingAmount);
    }

    [Fact]
    public async Task Customer_collection_keeps_exchange_rate_snapshot()
    {
        await using var seed = await Seed(currency: "EUR"); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordCustomerCollectionAsync(CustomerCommand(seed, 10, "COL-FX") with { ExchangeRate = 42.5m, ReportingCurrency = "TRY" }, default);
        var movement = await seed.Db.FinancialTransactions.SingleAsync(); Assert.True(result.Success); Assert.Equal(42.5m, movement.ExchangeRate); Assert.Equal(425m, movement.ReportingAmount); Assert.Equal("TRY", movement.ReportingCurrency);
    }

    [Fact]
    public async Task Duplicate_customer_reference_is_rejected()
    {
        await using var seed = await Seed(); var service = new AtomicFinanceService(seed.Db, seed.User); var command = CustomerCommand(seed, 10, "COL-DUP");
        Assert.True((await service.RecordCustomerCollectionAsync(command, default)).Success); var duplicate = await service.RecordCustomerCollectionAsync(command, default);
        Assert.False(duplicate.Success); Assert.Equal("DUPLICATE_OPERATION", duplicate.ErrorCode); Assert.Single(seed.Db.CustomerCollections);
    }

    [Fact]
    public async Task Invalid_customer_allocation_does_not_persist_any_finance_record()
    {
        await using var seed = await Seed(); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordCustomerCollectionAsync(CustomerCommand(seed, 10, "COL-ROLLBACK") with { AutoAllocate = false, Allocations = [new(Guid.NewGuid(), 10)] }, default);
        Assert.False(result.Success); Assert.Empty(seed.Db.CustomerCollections); Assert.Empty(seed.Db.FinancialTransactions); Assert.Empty(seed.Db.CustomerLedgerEntries);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(40, 40, 0)]
    [InlineData(130, 100, 30)]
    public async Task Supplier_payment_updates_payable_cash_and_advance_atomically(decimal amount, decimal allocated, decimal advance)
    {
        await using var seed = await Seed(openingBalance: 500); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, amount, "PAY-CASE-" + amount), default);
        Assert.True(result.Success); Assert.Equal(allocated, result.AllocatedAmount); Assert.Equal(advance, result.AdvanceAmount);
        Assert.Equal(100 - allocated, (await seed.Db.SupplierPayables.SingleAsync()).OutstandingAmount);
        Assert.Equal(amount, (await seed.Db.FinancialTransactions.SingleAsync()).Amount); Assert.Equal("Outflow", (await seed.Db.FinancialTransactions.SingleAsync()).Direction);
        Assert.Equal(amount, (await seed.Db.SupplierLedgerEntries.SingleAsync()).DebitAmount);
    }

    [Fact]
    public async Task Supplier_payment_allocates_multiple_payables_oldest_first()
    {
        await using var seed = await Seed(openingBalance: 500, secondDocument: true); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 120, "PAY-FIFO"), default);
        Assert.True(result.Success); var payables = await seed.Db.SupplierPayables.OrderBy(x => x.DueDate).ToListAsync();
        Assert.Equal(0, payables[0].OutstandingAmount); Assert.Equal(30, payables[1].OutstandingAmount); Assert.Equal(2, await seed.Db.SupplierPaymentAllocations.CountAsync());
    }

    [Fact]
    public async Task Supplier_payment_supports_selected_allocation()
    {
        await using var seed = await Seed(openingBalance: 500, secondDocument: true); var service = new AtomicFinanceService(seed.Db, seed.User); var target = await seed.Db.SupplierPayables.OrderByDescending(x => x.DueDate).FirstAsync();
        var command = SupplierCommand(seed, 20, "PAY-SELECT") with { AutoAllocate = false, Allocations = [new(target.Id, 20)] };
        var result = await service.RecordSupplierPaymentAsync(command, default);
        Assert.True(result.Success); Assert.Equal(30, target.OutstandingAmount); Assert.Equal(100, (await seed.Db.SupplierPayables.OrderBy(x => x.DueDate).FirstAsync()).OutstandingAmount);
    }

    [Fact]
    public async Task Supplier_payment_keeps_exchange_rate_snapshot()
    {
        await using var seed = await Seed(openingBalance: 500, currency: "EUR"); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-FX") with { ExchangeRate = 42.5m, ReportingCurrency = "TRY" }, default);
        var movement = await seed.Db.FinancialTransactions.SingleAsync(); Assert.True(result.Success); Assert.Equal(42.5m, movement.ExchangeRate); Assert.Equal(425m, movement.ReportingAmount);
    }

    [Fact]
    public async Task Duplicate_supplier_reference_is_rejected()
    {
        await using var seed = await Seed(openingBalance: 500); var service = new AtomicFinanceService(seed.Db, seed.User); var command = SupplierCommand(seed, 10, "PAY-DUP");
        Assert.True((await service.RecordSupplierPaymentAsync(command, default)).Success); var duplicate = await service.RecordSupplierPaymentAsync(command, default);
        Assert.False(duplicate.Success); Assert.Equal("DUPLICATE_OPERATION", duplicate.ErrorCode); Assert.Single(seed.Db.SupplierPayments);
    }

    [Fact]
    public async Task Insufficient_supplier_balance_returns_controlled_conflict_data_without_writes()
    {
        await using var seed = await Seed(openingBalance: 5); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-NO-BALANCE"), default);
        Assert.False(result.Success); Assert.Equal("INSUFFICIENT_BALANCE", result.ErrorCode); Assert.Equal(5, result.AvailableBalance); Assert.Equal(10, result.RequestedAmount); Assert.Empty(seed.Db.SupplierPayments); Assert.Empty(seed.Db.FinancialTransactions);
    }

    [Fact]
    public async Task Invalid_supplier_allocation_does_not_persist_any_finance_record()
    {
        await using var seed = await Seed(openingBalance: 500); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-ROLLBACK") with { AutoAllocate = false, Allocations = [new(Guid.NewGuid(), 10)] }, default);
        Assert.False(result.Success); Assert.Empty(seed.Db.SupplierPayments); Assert.Empty(seed.Db.FinancialTransactions); Assert.Empty(seed.Db.SupplierLedgerEntries);
    }

    [Fact]
    public async Task Non_ceo_cannot_use_negative_balance_override()
    {
        await using var seed = await Seed(openingBalance: 0, ceo: false); var service = new AtomicFinanceService(seed.Db, seed.User);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-OVERRIDE") with { AllowNegativeBalance = true, OverrideReason = "Onay" }, default);
        Assert.False(result.Success); Assert.Equal("INSUFFICIENT_BALANCE", result.ErrorCode);
    }

    [Fact]
    public async Task Ceo_override_requires_reason_and_is_audited()
    {
        await using var seed = await Seed(openingBalance: 0); var service = new AtomicFinanceService(seed.Db, seed.User);
        Assert.False((await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-NO-REASON") with { AllowNegativeBalance = true }, default)).Success);
        var result = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 10, "PAY-WITH-REASON") with { AllowNegativeBalance = true, OverrideReason = "CEO acil ödeme onayı" }, default);
        Assert.True(result.Success); Assert.Contains(seed.Db.AuditLogs, x => x.EntityName == "Atomic Supplier Payment Recorded");
    }

    [Fact]
    public async Task Customer_reversal_reopens_receivable_and_reverses_cash_once()
    {
        await using var seed = await Seed(); var service = new AtomicFinanceService(seed.Db, seed.User); var recorded = await service.RecordCustomerCollectionAsync(CustomerCommand(seed, 100, "COL-REVERSE"), default);
        var controller = new CustomerCollectionsController(seed.Db, service) { ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } }; var first = await controller.Reverse(recorded.OperationId!.Value, new CancelFinanceRequest("Test geri alma"), default); var second = await controller.Reverse(recorded.OperationId.Value, new CancelFinanceRequest("İkinci geri alma"), default);
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(first); Assert.IsType<Microsoft.AspNetCore.Mvc.ConflictObjectResult>(second); Assert.Equal(100, (await seed.Db.CustomerReceivables.SingleAsync()).OutstandingAmount); Assert.Equal(2, await seed.Db.FinancialTransactions.CountAsync()); Assert.Single(seed.Db.CustomerLedgerEntries.Where(x => x.EntryType == "Debit"));
    }

    [Fact]
    public async Task Supplier_reversal_reopens_payable_and_reverses_cash_once()
    {
        await using var seed = await Seed(openingBalance: 500); var service = new AtomicFinanceService(seed.Db, seed.User); var recorded = await service.RecordSupplierPaymentAsync(SupplierCommand(seed, 100, "PAY-REVERSE"), default);
        var controller = new SupplierPaymentsController(seed.Db, service) { ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } }; var first = await controller.Reverse(recorded.OperationId!.Value, new CancelFinanceRequest("Test geri alma"), default); var second = await controller.Reverse(recorded.OperationId.Value, new CancelFinanceRequest("İkinci geri alma"), default);
        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(first); Assert.IsType<Microsoft.AspNetCore.Mvc.ConflictObjectResult>(second); Assert.Equal(100, (await seed.Db.SupplierPayables.SingleAsync()).OutstandingAmount); Assert.Equal(2, await seed.Db.FinancialTransactions.CountAsync()); Assert.Single(seed.Db.SupplierLedgerEntries.Where(x => x.EntryType == "Credit"));
    }

    [Fact]
    public async Task Finance_xlsx_export_is_a_real_zip_workbook()
    {
        await using var seed = await Seed(); var controller = ExportController(seed.Db); var result = Assert.IsType<Microsoft.AspNetCore.Mvc.FileContentResult>(await controller.Xlsx("cash-transactions", null, null, null, null, null, "TRY", default));
        Assert.Equal((byte)'P', result.FileContents[0]); Assert.Equal((byte)'K', result.FileContents[1]); Assert.EndsWith(".xlsx", result.FileDownloadName);
    }

    [Fact]
    public async Task Finance_pdf_export_is_a_real_pdf_with_content()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        await using var seed = await Seed(); var controller = ExportController(seed.Db); var result = Assert.IsType<Microsoft.AspNetCore.Mvc.FileContentResult>(await controller.Pdf("customer-reconciliation", seed.Customer.Id, null, null, null, null, "TRY", default));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(result.FileContents, 0, 4)); Assert.True(result.FileContents.Length > 1_000); Assert.EndsWith(".pdf", result.FileDownloadName);
    }

    [Fact]
    public async Task PostgreSql_concurrent_payments_cannot_make_account_negative()
    {
        var connection = Environment.GetEnvironmentVariable("FIXAR_FINANCE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;
        var user = new TestUser(true); var clock = new TestClock(); var ids = (Account: Guid.NewGuid(), Supplier: Guid.NewGuid(), Payable: Guid.NewGuid()); var suffix = Guid.NewGuid().ToString("N")[..8];
        await using (var seed = PostgresDb(connection, user, clock))
        {
            seed.AddRange(new Supplier { Id = ids.Supplier, Code = $"CON-{suffix}", Name = "Concurrency Supplier", DefaultCurrency = "TRY" }, new FinancialAccount { Id = ids.Account, AccountCode = $"CON-{suffix}", Name = "Concurrency Cash", AccountType = "Cash", Currency = "TRY", OpeningBalance = 100, OpeningBalanceDate = DateTime.UtcNow }, new SupplierPayable { Id = ids.Payable, PayableNumber = $"CON-PAY-{suffix}", SupplierId = ids.Supplier, TransactionDate = DateTime.UtcNow, DueDate = DateTime.UtcNow, Currency = "TRY", OriginalAmount = 200, OutstandingAmount = 200 });
            await seed.SaveChangesAsync();
        }
        async Task<AtomicFinanceResult> Pay(string reference) { await using var context = PostgresDb(connection, user, clock); return await new AtomicFinanceService(context, user).RecordSupplierPaymentAsync(new(ids.Supplier, ids.Account, DateTime.UtcNow, "TRY", 80, "Cash", reference, AutoAllocate: true, ReportingCurrency: "TRY"), default); }
        var results = await Task.WhenAll(Pay($"CON-A-{suffix}"), Pay($"CON-B-{suffix}"));
        Assert.Single(results, x => x.Success); Assert.Single(results, x => !x.Success && x.ErrorCode == "INSUFFICIENT_BALANCE");
        await using var verify = PostgresDb(connection, user, clock); Assert.Equal(80, await verify.FinancialTransactions.Where(x => x.FinancialAccountId == ids.Account && !x.IsReversed).SumAsync(x => x.Amount));
    }

    private static FinanceExportsController ExportController(ApplicationDbContext db) => new(db) { ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
    private static ApplicationDbContext PostgresDb(string connection, ICurrentUserService user, IDateTimeService clock) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new AuditableEntitySaveChangesInterceptor(user, clock));

    private static AtomicCustomerCollectionCommand CustomerCommand(SeedData seed, decimal amount, string reference) => new(seed.Customer.Id, seed.Account.Id, DateTime.UtcNow, seed.Account.Currency, amount, seed.Account.AccountType == "Cash" ? "Cash" : "BankTransfer", reference, AutoAllocate: true, ReportingCurrency: seed.Account.Currency);
    private static AtomicSupplierPaymentCommand SupplierCommand(SeedData seed, decimal amount, string reference) => new(seed.Supplier.Id, seed.Account.Id, DateTime.UtcNow, seed.Account.Currency, amount, seed.Account.AccountType == "Cash" ? "Cash" : "BankTransfer", reference, AutoAllocate: true, ReportingCurrency: seed.Account.Currency);

    private static async Task<SeedData> Seed(decimal openingBalance = 0, string currency = "TRY", bool secondDocument = false, bool ceo = true)
    {
        var user = new TestUser(ceo); var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var db = new ApplicationDbContext(options, new AuditableEntitySaveChangesInterceptor(user, new TestClock()));
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = "C-1", Name = "Test Müşteri", DefaultCurrency = currency };
        var supplier = new Supplier { Id = Guid.NewGuid(), Code = "S-1", Name = "Test Tedarikçi", DefaultCurrency = currency };
        var account = new FinancialAccount { Id = Guid.NewGuid(), AccountCode = "CASH-1", Name = "Test Kasa", AccountType = "Cash", Currency = currency, OpeningBalance = openingBalance, OpeningBalanceDate = DateTime.UtcNow };
        db.AddRange(customer, supplier, account);
        db.CustomerReceivables.Add(new CustomerReceivable { Id = Guid.NewGuid(), ReceivableNumber = "REC-1", CustomerId = customer.Id, TransactionDate = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(-5), Currency = currency, OriginalAmount = 100, OutstandingAmount = 100 });
        db.SupplierPayables.Add(new SupplierPayable { Id = Guid.NewGuid(), PayableNumber = "PAYABLE-1", SupplierId = supplier.Id, TransactionDate = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(-5), Currency = currency, OriginalAmount = 100, OutstandingAmount = 100 });
        if (secondDocument) { db.CustomerReceivables.Add(new CustomerReceivable { Id = Guid.NewGuid(), ReceivableNumber = "REC-2", CustomerId = customer.Id, TransactionDate = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(5), Currency = currency, OriginalAmount = 50, OutstandingAmount = 50 }); db.SupplierPayables.Add(new SupplierPayable { Id = Guid.NewGuid(), PayableNumber = "PAYABLE-2", SupplierId = supplier.Id, TransactionDate = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(5), Currency = currency, OriginalAmount = 50, OutstandingAmount = 50 }); }
        await db.SaveChangesAsync(); return new(db, user, customer, supplier, account);
    }

    private sealed record SeedData(ApplicationDbContext Db, TestUser User, Customer Customer, Supplier Supplier, FinancialAccount Account) : IAsyncDisposable { public ValueTask DisposeAsync() => Db.DisposeAsync(); }
    private sealed class TestClock : IDateTimeService { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class TestUser(bool ceo) : ICurrentUserService { public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001"); public string? Email => "finance@test.local"; public string? UserName => "Finance Test"; public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => true; public IReadOnlyList<string> Roles => ceo ? ["CEO"] : ["Finance"]; }
}
