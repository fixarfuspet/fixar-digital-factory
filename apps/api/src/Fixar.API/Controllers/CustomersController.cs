using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), AllowAnonymous]
[Route("api/v{version:apiVersion}/customers")]
public class CustomersController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Currencies = ["TRY", "EUR", "USD", "GBP"];

    [HttpGet]
    public async Task<IActionResult> List(string? search, bool? isActive, string? city, string? country, string? currency, CancellationToken ct)
    {
        var q = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(x => x.Name.ToLower().Contains(s) || (x.CompanyName != null && x.CompanyName.ToLower().Contains(s)) || x.CustomerCode.ToLower().Contains(s)); }
        if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(x => x.City == city);
        if (!string.IsNullOrWhiteSpace(country)) q = q.Where(x => x.Country == country);
        if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.DefaultCurrency == currency.ToUpper());
        var rows = await q.OrderBy(x => x.CustomerCode).Select(x => new { x.Id, x.CustomerCode, x.Name, x.CompanyName, x.ContactName, x.Phone, x.Email, x.City, x.Country, x.DefaultCurrency, x.PaymentTermDays, x.CreditLimit, x.IsActive, ActiveOrderCount = x.Orders.Count(o => o.IsActive && !o.IsCancelled && o.Status != "Completed"), CreatedAt = x.Created, UpdatedAt = x.LastModified }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var x = await db.Customers.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.CustomerCode, x.Name, x.CompanyName, x.LegalName, x.TaxOffice, x.TaxNumber, x.AddressLine1, x.AddressLine2, x.City, x.District, x.PostalCode, x.Country, x.DefaultCurrency, x.PaymentTermDays, x.CreditLimit, x.ContactName, x.Phone, x.Email, x.Notes, x.IsActive, CreatedAt = x.Created, UpdatedAt = x.LastModified, RecentOrders = x.Orders.OrderByDescending(o => o.OrderDate).Take(5).Select(o => new { o.Id, o.OrderNumber, o.OrderDate, o.Status, o.Currency, o.GrandTotal }) }).FirstOrDefaultAsync(ct);
        return x is null ? NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerRequest request, CancellationToken ct)
    {
        var error = Validate(request); if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR"));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81001)", ct);
        var max = await db.Customers.Where(x => x.CustomerCode.StartsWith("CUS-")).Select(x => x.CustomerCode).ToListAsync(ct);
        var next = max.Select(x => int.TryParse(x[4..], out var n) ? n : 0).DefaultIfEmpty().Max() + 1;
        var entity = new Customer { CustomerCode = $"CUS-{next:000000}" }; Apply(entity, request); db.Customers.Add(entity); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = entity.Id, version = "1.0" }, ApiResponse<object>.SuccessResponse(new { entity.Id, entity.CustomerCode }, "Müşteri oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CustomerRequest request, CancellationToken ct)
    {
        var error = Validate(request); if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR"));
        var entity = await db.Customers.FindAsync([id], ct); if (entity is null) return NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND"));
        Apply(entity, request); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.CustomerCode }, "Müşteri güncellendi."));
    }

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct) => SetActive(id, true, ct);
    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => SetActive(id, false, ct);

    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == id, ct)) return NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND"));
        var orders = db.Orders.AsNoTracking().Where(x => x.CustomerId == id);
        var result = new { TotalOrders = await orders.CountAsync(ct), ActiveOrders = await orders.CountAsync(x => x.IsActive && !x.IsCancelled && x.Status != "Completed", ct), TotalOrderedPairs = await orders.SelectMany(x => x.Items).SumAsync(x => (int?)x.QuantityPairs, ct) ?? 0, TotalProducedPairs = await orders.SelectMany(x => x.Items).SumAsync(x => (int?)x.ProducedPairs, ct) ?? 0, TotalShippedPairs = await orders.SelectMany(x => x.Items).SumAsync(x => (int?)x.ShippedPairs, ct) ?? 0, OpenOrderAmountByCurrency = await orders.Where(x => x.IsActive && !x.IsCancelled && x.Status != "Completed").GroupBy(x => x.Currency).Select(x => new { Currency = x.Key, Amount = x.Sum(o => o.GrandTotal) }).ToListAsync(ct), LastOrderDate = await orders.MaxAsync(x => (DateTime?)x.OrderDate, ct) };
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    private async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken ct) { var x = await db.Customers.FindAsync([id], ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND")); x.IsActive = active; await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id, x.IsActive }, active ? "Müşteri aktifleştirildi." : "Müşteri pasifleştirildi.")); }
    private static string? Validate(CustomerRequest r) { if (string.IsNullOrWhiteSpace(r.Name)) return "Firma adı zorunludur."; if (!Currencies.Contains(r.DefaultCurrency?.ToUpper())) return "Para birimi TRY, EUR, USD veya GBP olmalıdır."; if (r.PaymentTermDays < 0) return "Ödeme vadesi negatif olamaz."; if (r.CreditLimit < 0) return "Risk limiti negatif olamaz."; if (!string.IsNullOrWhiteSpace(r.Email) && !new EmailAddressAttribute().IsValid(r.Email)) return "E-posta formatı geçersizdir."; if (!string.IsNullOrWhiteSpace(r.TaxNumber) && (r.TaxNumber.Length < 10 || r.TaxNumber.Length > 11 || !r.TaxNumber.All(char.IsDigit))) return "Vergi numarası 10 veya 11 rakam olmalıdır."; return null; }
    private static void Apply(Customer x, CustomerRequest r) { x.Name = r.Name.Trim(); x.CompanyName = r.Name.Trim(); x.LegalName = r.LegalName; x.TaxOffice = r.TaxOffice; x.TaxNumber = r.TaxNumber; x.AddressLine1 = r.AddressLine1; x.AddressLine2 = r.AddressLine2; x.City = r.City; x.District = r.District; x.PostalCode = r.PostalCode; x.Country = r.Country; x.DefaultCurrency = r.DefaultCurrency.ToUpper(); x.PaymentTermDays = r.PaymentTermDays; x.CreditLimit = r.CreditLimit; x.ContactName = r.ContactName; x.Phone = r.Phone; x.Email = r.Email; x.Notes = r.Notes; }
}

public record CustomerRequest(string Name, string? LegalName, string? TaxOffice, string? TaxNumber, string? AddressLine1, string? AddressLine2, string? City, string? District, string? PostalCode, string? Country, string DefaultCurrency = "TRY", int PaymentTermDays = 0, decimal CreditLimit = 0, string? ContactName = null, string? Phone = null, string? Email = null, string? Notes = null);
