using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

internal static class SupplierFinanceSupport
{
    public static string Status(SupplierPayable value) => value.IsCancelled ? "Cancelled" : value.OutstandingAmount <= 0 ? "Paid" : value.DueDate.Date < DateTime.UtcNow.Date ? "Overdue" : value.AllocatedAmount > 0 ? "PartiallyPaid" : "Open";

    public static async Task<string> Next(ApplicationDbContext db, string kind, DateTime date, CancellationToken ct)
    {
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync(kind == "PAY" ? "SELECT pg_advisory_xact_lock(81320)" : kind == "SPY" ? "SELECT pg_advisory_xact_lock(81321)" : kind == "SLE" ? "SELECT pg_advisory_xact_lock(81322)" : "SELECT pg_advisory_xact_lock(81323)", ct);
        var prefix = kind is "PAY" or "SPY" ? $"{kind}-{date:yyyyMMdd}-" : kind == "END" ? $"END-{date:yyyyMMdd}-" : $"SLE-{date:yyyyMMdd}-";
        var values = kind switch
        {
            "PAY" => await db.SupplierPayables.Where(x => x.PayableNumber.StartsWith(prefix)).Select(x => x.PayableNumber).ToListAsync(ct),
            "SPY" => await db.SupplierPayments.Where(x => x.PaymentNumber.StartsWith(prefix)).Select(x => x.PaymentNumber).ToListAsync(ct),
            "END" => await db.ChequeEndorsements.Where(x => x.EndorsementNumber.StartsWith(prefix)).Select(x => x.EndorsementNumber).ToListAsync(ct),
            _ => await db.SupplierLedgerEntries.Where(x => x.EntryNumber.StartsWith(prefix)).Select(x => x.EntryNumber).ToListAsync(ct)
        };
        var next = values.Select(x => int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty().Max() + 1;
        return $"{prefix}{next:0000}";
    }
}
