using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize(Policy = AuthorizationPolicies.CanManageExchangeRates)]
[Route("api/v{version:apiVersion}/exchange-rates")]
public sealed class ExchangeRatesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(DateTime? date, string? fromCurrency, string? toCurrency, CancellationToken ct) { var q = db.ExchangeRates.AsNoTracking().AsQueryable(); if (date.HasValue) q = q.Where(x => x.RateDate.Date == date.Value.Date); if (!string.IsNullOrWhiteSpace(fromCurrency)) q = q.Where(x => x.BaseCurrency == fromCurrency.ToUpper()); if (!string.IsNullOrWhiteSpace(toCurrency)) q = q.Where(x => x.QuoteCurrency == toCurrency.ToUpper()); return Ok(ApiResponse<object>.SuccessResponse(await q.OrderByDescending(x => x.RateDate).ToListAsync(ct))); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) { var x = await db.ExchangeRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); return x is null ? NotFound(ApiResponse<object>.Fail("Döviz kuru bulunamadı.", "RATE_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x)); }
    [HttpPost] public async Task<IActionResult> Create(ExchangeRateRequest r, CancellationToken ct) { var error = Validate(r); if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR")); var x = new ExchangeRate(); Apply(x, r); db.ExchangeRates.Add(x); await db.SaveChangesAsync(ct); return CreatedAtAction(nameof(Get), new { id = x.Id, version = "1" }, ApiResponse<object>.SuccessResponse(new { x.Id })); }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, ExchangeRateRequest r, CancellationToken ct) { var x = await db.ExchangeRates.FirstOrDefaultAsync(x => x.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Döviz kuru bulunamadı.", "RATE_NOT_FOUND")); var error = Validate(r); if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR")); Apply(x, r); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id })); }
    [HttpPost("{id:guid}/activate")] public Task<IActionResult> Activate(Guid id, CancellationToken ct) => Active(id, true, ct);
    [HttpPost("{id:guid}/deactivate")] public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => Active(id, false, ct);
    [HttpGet("resolve")] public async Task<IActionResult> Resolve(DateTime date, string fromCurrency, string toCurrency, CancellationToken ct) { date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc); var from = fromCurrency.ToUpperInvariant(); var to = toCurrency.ToUpperInvariant(); if (from == to) return Ok(ApiResponse<object>.SuccessResponse(new { Date = date.Date, FromCurrency = from, ToCurrency = to, Rate = 1m, Source = "Identity" })); var direct = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == from && x.QuoteCurrency == to && x.RateDate.Date <= date.Date).OrderByDescending(x => x.RateDate).FirstOrDefaultAsync(ct); if (direct is not null) return Ok(ApiResponse<object>.SuccessResponse(new { Date = direct.RateDate, FromCurrency = from, ToCurrency = to, direct.Rate, direct.Source })); var inverse = await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive && x.BaseCurrency == to && x.QuoteCurrency == from && x.RateDate.Date <= date.Date).OrderByDescending(x => x.RateDate).FirstOrDefaultAsync(ct); return inverse is null ? UnprocessableEntity(ApiResponse<object>.Fail("Bu maliyet hesabı için gerekli döviz kuru bulunamadı.", "EXCHANGE_RATE_MISSING")) : Ok(ApiResponse<object>.SuccessResponse(new { Date = inverse.RateDate, FromCurrency = from, ToCurrency = to, Rate = 1 / inverse.Rate, Source = $"Inverse:{inverse.Source}" })); }
    private async Task<IActionResult> Active(Guid id, bool active, CancellationToken ct) { var x = await db.ExchangeRates.FirstOrDefaultAsync(x => x.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Döviz kuru bulunamadı.", "RATE_NOT_FOUND")); x.IsActive = active; await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id, x.IsActive })); }
    private static string? Validate(ExchangeRateRequest x) => x.Rate <= 0 ? "Kur sıfırdan büyük olmalıdır." : x.BaseCurrency.Equals(x.QuoteCurrency, StringComparison.OrdinalIgnoreCase) ? "Kaynak ve hedef para birimi farklı olmalıdır." : null;
    private static void Apply(ExchangeRate x, ExchangeRateRequest r) { x.RateDate = r.RateDate.Date; x.BaseCurrency = r.BaseCurrency.Trim().ToUpperInvariant(); x.QuoteCurrency = r.QuoteCurrency.Trim().ToUpperInvariant(); x.Rate = r.Rate; x.Source = string.IsNullOrWhiteSpace(r.Source) ? "Manual" : r.Source.Trim(); x.Notes = r.Notes; x.IsActive = r.IsActive; }
}
public sealed record ExchangeRateRequest(DateTime RateDate, string BaseCurrency, string QuoteCurrency, decimal Rate, string Source, string? Notes, bool IsActive);
