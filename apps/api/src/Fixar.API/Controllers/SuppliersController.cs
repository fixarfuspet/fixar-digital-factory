using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SuppliersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var suppliers = await _db.Suppliers
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(suppliers));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (supplier is null)
            return NotFound(ApiResponse<object>.Fail("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(supplier));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Tedarikçi adı zorunludur.", "NAME_REQUIRED"));

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            TaxOffice = request.TaxOffice,
            TaxNumber = request.TaxNumber,
            Address = request.Address,
            DefaultCurrency = string.IsNullOrWhiteSpace(request.DefaultCurrency) ? "TRY" : request.DefaultCurrency.Trim(),
            PaymentTermDays = request.PaymentTermDays,
            Note = request.Note,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(supplier, "Tedarikçi oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (supplier is null)
            return NotFound(ApiResponse<object>.Fail("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Tedarikçi adı zorunludur.", "NAME_REQUIRED"));

        supplier.Name = request.Name.Trim();
        supplier.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.TaxOffice = request.TaxOffice;
        supplier.TaxNumber = request.TaxNumber;
        supplier.Address = request.Address;
        supplier.DefaultCurrency = string.IsNullOrWhiteSpace(request.DefaultCurrency) ? "TRY" : request.DefaultCurrency.Trim();
        supplier.PaymentTermDays = request.PaymentTermDays;
        supplier.Note = request.Note;
        supplier.IsActive = request.IsActive ?? supplier.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(supplier, "Tedarikçi güncellendi."));
    }
}

public record CreateSupplierRequest(
    string Name,
    string? Code,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? TaxOffice,
    string? TaxNumber,
    string? Address,
    string? DefaultCurrency,
    int? PaymentTermDays,
    string? Note,
    bool? IsActive
);
