using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/production-planning/lookups")]
public class ProductionPlanningLookupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductionPlanningLookupsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken)
    {
        var customers = await _db.Customers
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                Name = !string.IsNullOrWhiteSpace(x.CompanyName) ? x.CompanyName : x.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(customers));
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .Include(x => x.Items)
            .Where(x => x.Status == "Aktif")
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        var orders = await query
            .OrderBy(x => x.Customer.Name)
            .Select(x => new
            {
                x.Id,
                CustomerName = !string.IsNullOrWhiteSpace(x.Customer.CompanyName) ? x.Customer.CompanyName : x.Customer.Name,
                ProductName = x.Product.Name,
                x.SizeRange,
                x.Color,
                x.Quantity,
                RemainingQuantity = x.Quantity - x.ProducedQuantity,
                Items = x.Items.Select(i => new
{
    i.Id,
    ProductId = i.ProductId ?? x.ProductId,
    MoldId = i.MoldId,
    ProductName = i.Product != null ? i.Product.Name : x.Product.Name,
    ProductCode = i.Product != null ? i.Product.Code : x.Product.Code,
    MoldName = i.Mold != null ? i.Mold.Name : "-",
    i.QuantityPairs,
    i.ProducedPairs,
    RemainingPairs = i.QuantityPairs - i.ProducedPairs,
    i.ProductionType,
    i.FabricColor
})
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(orders));
    }

    [HttpGet("molds")]
    public async Task<IActionResult> Molds(CancellationToken cancellationToken)
    {
        var molds = await _db.Molds
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SizeRange
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(molds));
    }

    [HttpGet("operators")]
    public IActionResult Operators()
    {
        var operators = new[]
        {
            new { Id = "mahmut", Name = "Mahmut" },
            new { Id = "erdem", Name = "Erdem" },
            new { Id = "ramazan", Name = "Ramazan" }
        };

        return Ok(ApiResponse<object>.SuccessResponse(operators));
    }
}
