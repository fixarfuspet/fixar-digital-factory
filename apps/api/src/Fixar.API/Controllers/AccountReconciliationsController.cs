using System.Text.Json;
using Asp.Versioning;
using Fixar.API.Security;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Route("api/v{version:apiVersion}/account-reconciliations")]
[Authorize(Policy = AuthorizationPolicies.CanViewCustomerFinance)]
public class AccountReconciliationsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? partyType, Guid? customerId, Guid? supplierId, string? status, CancellationToken ct)
    {
        var query = db.AccountReconciliations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partyType)) query = query.Where(x => x.AccountPartyType == partyType);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId);
        if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var rows = await query.OrderByDescending(x => x.PeriodEnd).Select(x => new
        {
            x.Id, x.ReconciliationNumber, x.AccountPartyType, x.CustomerId, x.SupplierId,
            PartyName = x.Customer != null ? (x.Customer.CompanyName ?? x.Customer.Name) : x.Supplier != null ? x.Supplier.Name : null,
            x.PeriodStart, x.PeriodEnd, x.OpeningBalance, x.PeriodDebit, x.PeriodCredit,
            x.ClosingBalance, x.Currency, x.Status, x.ApprovedAt, x.ApprovedBy
        }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var row = await db.AccountReconciliations.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.ReconciliationNumber, x.AccountPartyType, x.CustomerId, x.SupplierId,
            PartyName = x.Customer != null ? (x.Customer.CompanyName ?? x.Customer.Name) : x.Supplier != null ? x.Supplier.Name : null,
            x.PeriodStart, x.PeriodEnd, x.OpeningBalance, x.PeriodDebit, x.PeriodCredit,
            x.ClosingBalance, x.Currency, x.Status, x.SnapshotJson, x.CounterpartyNote,
            x.InternalNote, x.ApprovedAt, x.ApprovedBy, x.Created
        }).FirstOrDefaultAsync(ct);
        return row is null
            ? NotFound(ApiResponse<object>.Fail("Mutabakat kaydı bulunamadı.", "RECONCILIATION_NOT_FOUND"))
            : Ok(ApiResponse<object>.SuccessResponse(row));
    }

    [HttpPost, Authorize(Policy = AuthorizationPolicies.CanRecordCollections), Idempotent]
    public async Task<IActionResult> Create(ReconciliationRequest request, CancellationToken ct)
    {
        if (request.PeriodEnd.Date < request.PeriodStart.Date)
            return BadRequest(ApiResponse<object>.Fail("Dönem bitişi başlangıçtan önce olamaz.", "INVALID_PERIOD"));
        if (request.PartyType is not ("Customer" or "Supplier") || request.PartyType == "Customer" == !request.CustomerId.HasValue || request.PartyType == "Supplier" == !request.SupplierId.HasValue)
            return BadRequest(ApiResponse<object>.Fail("Müşteri veya tedarikçi seçimi geçersiz.", "INVALID_PARTY"));

        var start = request.PeriodStart.Date.ToUniversalTime();
        var end = request.PeriodEnd.Date.AddDays(1).ToUniversalTime();
        decimal openingDebit, openingCredit, periodDebit, periodCredit;
        object lines;
        string partyName;
        if (request.PartyType == "Customer")
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CustomerId, ct);
            if (customer is null) return NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND"));
            partyName = customer.CompanyName ?? customer.Name;
            var query = db.CustomerLedgerEntries.AsNoTracking().Where(x => x.CustomerId == request.CustomerId && x.Currency == request.Currency.ToUpper());
            openingDebit = await query.Where(x => x.TransactionDate < start).SumAsync(x => (decimal?)x.DebitAmount, ct) ?? 0;
            openingCredit = await query.Where(x => x.TransactionDate < start).SumAsync(x => (decimal?)x.CreditAmount, ct) ?? 0;
            periodDebit = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).SumAsync(x => (decimal?)x.DebitAmount, ct) ?? 0;
            periodCredit = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).SumAsync(x => (decimal?)x.CreditAmount, ct) ?? 0;
            lines = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).OrderBy(x => x.TransactionDate).Select(x => new { x.EntryNumber, x.TransactionDate, x.EntryType, x.ReferenceNumber, x.DebitAmount, x.CreditAmount, x.Description }).ToListAsync(ct);
        }
        else
        {
            var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SupplierId, ct);
            if (supplier is null) return NotFound(ApiResponse<object>.Fail("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND"));
            partyName = supplier.Name;
            var query = db.SupplierLedgerEntries.AsNoTracking().Where(x => x.SupplierId == request.SupplierId && x.Currency == request.Currency.ToUpper());
            openingDebit = await query.Where(x => x.TransactionDate < start).SumAsync(x => (decimal?)x.DebitAmount, ct) ?? 0;
            openingCredit = await query.Where(x => x.TransactionDate < start).SumAsync(x => (decimal?)x.CreditAmount, ct) ?? 0;
            periodDebit = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).SumAsync(x => (decimal?)x.DebitAmount, ct) ?? 0;
            periodCredit = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).SumAsync(x => (decimal?)x.CreditAmount, ct) ?? 0;
            lines = await query.Where(x => x.TransactionDate >= start && x.TransactionDate < end).OrderBy(x => x.TransactionDate).Select(x => new { x.EntryNumber, x.TransactionDate, x.EntryType, x.ReferenceNumber, x.DebitAmount, x.CreditAmount, x.Description }).ToListAsync(ct);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81314)", ct);
        var prefix = $"MUT-{DateTime.UtcNow:yyyyMMdd}-";
        var numbers = await db.AccountReconciliations.Where(x => x.ReconciliationNumber.StartsWith(prefix)).Select(x => x.ReconciliationNumber).ToListAsync(ct);
        var next = numbers.Select(x => int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty().Max() + 1;
        var opening = openingDebit - openingCredit;
        var entity = new AccountReconciliation
        {
            Id = Guid.NewGuid(), ReconciliationNumber = $"{prefix}{next:0000}", AccountPartyType = request.PartyType,
            CustomerId = request.CustomerId, SupplierId = request.SupplierId, PeriodStart = start, PeriodEnd = end.AddTicks(-1),
            OpeningBalance = opening, PeriodDebit = periodDebit, PeriodCredit = periodCredit,
            ClosingBalance = opening + periodDebit - periodCredit, Currency = request.Currency.ToUpper(), Status = "Draft",
            SnapshotJson = JsonSerializer.Serialize(new { PartyName = partyName, CreatedAt = DateTime.UtcNow, Lines = lines }),
            CounterpartyNote = request.CounterpartyNote, InternalNote = request.InternalNote, Created = DateTime.UtcNow
        };
        db.AccountReconciliations.Add(entity);
        CustomerFinanceSupport.Audit(db, this, "Account Reconciliation Created", entity.Id, new { entity.ReconciliationNumber, entity.AccountPartyType, entity.ClosingBalance });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.ReconciliationNumber, entity.ClosingBalance, entity.Status }));
    }

    [HttpPost("{id:guid}/approve"), Authorize(Policy = AuthorizationPolicies.CanRecordCollections), Idempotent]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var entity = await db.AccountReconciliations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Mutabakat kaydı bulunamadı.", "RECONCILIATION_NOT_FOUND"));
        if (entity.Status != "Draft") return Conflict(ApiResponse<object>.Fail("Yalnız taslak mutabakat onaylanabilir.", "INVALID_STATUS"));
        entity.Status = "Approved"; entity.ApprovedAt = DateTime.UtcNow; entity.ApprovedBy = User.Identity?.Name;
        CustomerFinanceSupport.Audit(db, this, "Account Reconciliation Approved", entity.Id, new { entity.ReconciliationNumber });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.Status }));
    }
}

public record ReconciliationRequest(string PartyType, Guid? CustomerId, Guid? SupplierId, DateTime PeriodStart, DateTime PeriodEnd, string Currency, string? CounterpartyNote, string? InternalNote);
