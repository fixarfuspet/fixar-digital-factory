using System.Text.Json;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

internal static class CustomerFinanceSupport
{
    public static string Status(CustomerReceivable x, DateTime? now = null)
    {
        if (x.IsCancelled) return "Cancelled";
        if (x.OutstandingAmount <= 0) return "Paid";
        if (x.DueDate.Date < (now ?? DateTime.UtcNow).Date) return "Overdue";
        return x.AllocatedAmount > 0 ? "PartiallyPaid" : "Open";
    }

    public static async Task<string> NextNumber(ApplicationDbContext db, string prefix, DateTime date, CancellationToken ct)
    {
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync(prefix == "REC" ? "SELECT pg_advisory_xact_lock(81301)" : prefix == "COL" ? "SELECT pg_advisory_xact_lock(81302)" : "SELECT pg_advisory_xact_lock(81303)", ct);
        var start = $"{prefix}-{date:yyyyMMdd}-";
        var values = prefix switch
        {
            "REC" => await db.CustomerReceivables.Where(x => x.ReceivableNumber.StartsWith(start)).Select(x => x.ReceivableNumber).ToListAsync(ct),
            "COL" => await db.CustomerCollections.Where(x => x.CollectionNumber.StartsWith(start)).Select(x => x.CollectionNumber).ToListAsync(ct),
            _ => await db.CustomerLedgerEntries.Where(x => x.EntryNumber.StartsWith(start)).Select(x => x.EntryNumber).ToListAsync(ct)
        };
        var next = values.Select(x => int.TryParse(x[start.Length..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
        return $"{start}{next:0000}";
    }

    public static async Task<CustomerReceivable> CreateReceivable(ApplicationDbContext db, Customer customer, Order? order, decimal amount, string currency, DateTime date, DateTime dueDate, string sourceType, string? description, CancellationToken ct)
    {
        var receivable = new CustomerReceivable { Id = Guid.NewGuid(), ReceivableNumber = await NextNumber(db, "REC", date, ct), CustomerId = customer.Id, OrderId = order?.Id, OrderNumberSnapshot = order?.OrderNumber, TransactionDate = date, DueDate = dueDate, Currency = currency.ToUpperInvariant(), OriginalAmount = amount, OutstandingAmount = amount, AllocatedAmount = 0, SourceType = sourceType, Description = description, Status = dueDate.Date < DateTime.UtcNow.Date ? "Overdue" : "Open", IsActive = true };
        db.CustomerReceivables.Add(receivable);
        db.CustomerLedgerEntries.Add(new CustomerLedgerEntry { Id = Guid.NewGuid(), EntryNumber = await NextNumber(db, "LED", date, ct), CustomerId = customer.Id, TransactionDate = date, DueDate = dueDate, EntryType = "Debit", SourceType = "Receivable", SourceId = receivable.Id, ReferenceNumber = receivable.ReceivableNumber, Currency = receivable.Currency, DebitAmount = amount, Description = description ?? $"{receivable.ReceivableNumber} alacağı", Created = DateTime.UtcNow });
        return receivable;
    }

    public static void Audit(ApplicationDbContext db, ControllerBase controller, string name, Guid id, object payload)
        => db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserName = controller.User.Identity?.Name ?? "system", Action = AuditAction.Update, EntityName = name, EntityId = id.ToString(), NewValues = JsonSerializer.Serialize(payload), Timestamp = DateTime.UtcNow, IpAddress = controller.HttpContext.Connection.RemoteIpAddress?.ToString() });
}
