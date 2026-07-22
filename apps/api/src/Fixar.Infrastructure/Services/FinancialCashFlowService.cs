using Fixar.Application.Common.Interfaces;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.Infrastructure.Services;

public class FinancialCashFlowService(ApplicationDbContext db) : IFinancialCashFlowService
{
    public async Task<object> GetAccountBalanceAsync(Guid id, CancellationToken ct)
    {
        var row = await db.FinancialAccounts.AsNoTracking().Where(a => a.Id == id).Select(a => new
        {
            a.Id, a.AccountCode, a.Name, a.Currency, a.OpeningBalance,
            Inflow = db.FinancialTransactions.Where(t => t.FinancialAccountId == a.Id && !t.IsReversed && t.AffectsBalance && t.Direction == "Inflow").Sum(t => (decimal?)t.Amount) ?? 0,
            Outflow = db.FinancialTransactions.Where(t => t.FinancialAccountId == a.Id && !t.IsReversed && t.AffectsBalance && t.Direction == "Outflow").Sum(t => (decimal?)t.Amount) ?? 0
        }).FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Finans hesabı bulunamadı.");
        return new { row.Id, row.AccountCode, row.Name, row.Currency, row.OpeningBalance, row.Inflow, row.Outflow, CurrentBalance = row.OpeningBalance + row.Inflow - row.Outflow, AvailableBalance = row.OpeningBalance + row.Inflow - row.Outflow };
    }

    public async Task<IReadOnlyList<object>> GetBalancesAsync(CancellationToken ct) => (await db.FinancialAccounts.AsNoTracking().Select(a => new
    {
        a.Id, a.AccountCode, a.Name, a.AccountType, a.Currency, a.OpeningBalance,
        Movement = db.FinancialTransactions.Where(t => t.FinancialAccountId == a.Id && !t.IsReversed && t.AffectsBalance).Sum(t => (decimal?)(t.Direction == "Inflow" ? t.Amount : -t.Amount)) ?? 0,
        a.IsActive
    }).ToListAsync(ct)).Select(x => (object)new { x.Id, x.AccountCode, x.Name, x.AccountType, x.Currency, Balance = x.OpeningBalance + x.Movement, x.IsActive }).ToList();

    public Task<IReadOnlyList<object>> GetCashFlowAsync(DateTime from, DateTime to, string? currency, CancellationToken ct) => GetDailyCashFlowAsync(from, to, currency, ct);

    public async Task<IReadOnlyList<object>> GetDailyCashFlowAsync(DateTime from, DateTime to, string? currency, CancellationToken ct)
    {
        var query = db.FinancialTransactions.AsNoTracking().Where(x => !x.IsReversed && x.AffectsBalance && x.TransactionDate >= from && x.TransactionDate < to);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(x => x.Currency == currency.ToUpper());
        var raw = await query.Select(x => new { x.TransactionDate, x.Currency, x.Direction, x.TransactionType, x.Amount, x.FinancialAccount.AccountType }).ToListAsync(ct);
        return raw.GroupBy(x => new { x.TransactionDate.Date, x.Currency }).Select(group => (object)new
        {
            Date = group.Key.Date, group.Key.Currency,
            Inflow = group.Where(x => x.Direction == "Inflow").Sum(x => x.Amount),
            Outflow = group.Where(x => x.Direction == "Outflow").Sum(x => x.Amount),
            NetCashFlow = group.Where(x => x.TransactionType != "AccountTransfer").Sum(x => x.Direction == "Inflow" ? x.Amount : -x.Amount),
            CashInflow = group.Where(x => x.Direction == "Inflow" && x.AccountType == "Cash").Sum(x => x.Amount),
            BankInflow = group.Where(x => x.Direction == "Inflow" && x.AccountType == "Bank").Sum(x => x.Amount),
            CreditCardInflow = group.Where(x => x.Direction == "Inflow" && x.AccountType == "CreditCardClearing").Sum(x => x.Amount),
            ManualInflow = group.Where(x => x.TransactionType == "ManualIncome").Sum(x => x.Amount),
            ManualOutflow = group.Where(x => x.TransactionType == "ManualExpense").Sum(x => x.Amount),
            TransferIn = group.Where(x => x.TransactionType == "AccountTransfer" && x.Direction == "Inflow").Sum(x => x.Amount),
            TransferOut = group.Where(x => x.TransactionType == "AccountTransfer" && x.Direction == "Outflow").Sum(x => x.Amount),
            TransactionCount = group.Count()
        }).OrderBy(x => ((dynamic)x).Date).ToList();
    }

    public Task<IReadOnlyList<object>> GetMonthlyCashFlowAsync(DateTime from, DateTime to, string? currency, CancellationToken ct) => GetDailyCashFlowAsync(from, to, currency, ct);

    public async Task<IReadOnlyList<object>> GetPaymentMethodBreakdownAsync(DateTime from, DateTime to, CancellationToken ct) => (await db.CustomerCollections.AsNoTracking().Where(x => !x.IsReversed && x.Status != "Draft" && x.Status != "Cancelled" && x.CollectionDate >= from && x.CollectionDate < to).GroupBy(x => new { x.Currency, x.PaymentMethod }).Select(g => new { g.Key.Currency, g.Key.PaymentMethod, Amount = g.Sum(x => x.Amount), Count = g.Count() }).ToListAsync(ct)).Cast<object>().ToList();

    public async Task<IReadOnlyList<object>> GetChequeMaturityAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        return (await db.CustomerCheques.AsNoTracking().Where(x => x.Status == "InPortfolio" || x.Status == "DepositedToBank").Select(x => new { x.Id, x.PortfolioNumber, Customer = x.Customer.CompanyName ?? x.Customer.Name, x.DueDate, x.Currency, x.Amount, x.Status }).ToListAsync(ct)).Select(x => (object)new { x.Id, x.PortfolioNumber, x.Customer, x.DueDate, x.Currency, x.Amount, x.Status, Bucket = x.DueDate < today ? "Overdue" : x.DueDate == today ? "Today" : x.DueDate <= today.AddDays(7) ? "Next7Days" : x.DueDate <= today.AddDays(30) ? "Next30Days" : "Later" }).ToList();
    }

    public async Task<IReadOnlyList<object>> GetLiquiditySummaryAsync(CancellationToken ct)
    {
        var balances = await db.FinancialAccounts.AsNoTracking().Select(a => new { a.AccountType, a.Currency, Balance = a.OpeningBalance + (db.FinancialTransactions.Where(t => t.FinancialAccountId == a.Id && !t.IsReversed && t.AffectsBalance).Sum(t => (decimal?)(t.Direction == "Inflow" ? t.Amount : -t.Amount)) ?? 0) }).ToListAsync(ct);
        var cheques = await db.CustomerCheques.AsNoTracking().ToListAsync(ct);
        var unposted = await db.CustomerCollections.AsNoTracking().Where(x => x.Status != "Draft" && x.Status != "Cancelled" && !x.IsReversed && x.FinancePostingStatus == "Pending").GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);
        var currencies = balances.Select(x => x.Currency).Concat(cheques.Select(x => x.Currency)).Concat(unposted.Select(x => x.Currency)).Distinct(); var today = DateTime.UtcNow.Date;
        return currencies.Select(currency => (object)new { Currency = currency, CashBalance = balances.Where(x => x.Currency == currency && x.AccountType == "Cash").Sum(x => x.Balance), BankBalance = balances.Where(x => x.Currency == currency && x.AccountType == "Bank").Sum(x => x.Balance), CardClearingBalance = balances.Where(x => x.Currency == currency && x.AccountType == "CreditCardClearing").Sum(x => x.Balance), TotalLiquidBalance = balances.Where(x => x.Currency == currency && x.AccountType is "Cash" or "Bank" or "CreditCardClearing").Sum(x => x.Balance), ChequePortfolioAmount = cheques.Where(x => x.Currency == currency && x.Status is "InPortfolio" or "DepositedToBank").Sum(x => x.Amount), ChequesDueIn7Days = cheques.Where(x => x.Currency == currency && x.DueDate >= today && x.DueDate <= today.AddDays(7) && x.Status is "InPortfolio" or "DepositedToBank").Sum(x => x.Amount), UnpostedCollectionAmount = unposted.FirstOrDefault(x => x.Currency == currency)?.Amount ?? 0 }).ToList();
    }
}
