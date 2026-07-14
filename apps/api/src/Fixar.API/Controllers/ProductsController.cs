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
[Authorize]
[Route("api/v{version:apiVersion}/products")]
public class ProductsController : ControllerBase
{
    private static readonly string[] FoamTypes =
    {
        "10100",
        "10900"
    };

    private static readonly string[] ProductTypes =
    {
        "Normal",
        "Memory Foam"
    };

    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(products));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
            return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı.", "PRODUCT_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(product));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);

        if (validation is not null)
            return validation;

        var product = new Product
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            CustomerName = request.CustomerName,
            Category = request.Category,
            ModelCode = request.ModelCode,
            Description = request.Description,
            FoamType = request.FoamType.Trim(),
            ProductType = request.ProductType.Trim(),
            IsFabric = request.IsFabric,
            IsAdhesive = request.IsAdhesive,
            HasDTFLabel = request.HasDTFLabel,
            HasPolibond = request.HasPolibond,
            AverageWeight = request.AverageWeight,
            TargetDensity = request.TargetDensity,
            StandardCycleTime = request.StandardCycleTime,
            DefaultBoxQuantity = request.DefaultBoxQuantity,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(product, "Ürün oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
            return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı.", "PRODUCT_NOT_FOUND"));

        var validation = ValidateRequest(request);

        if (validation is not null)
            return validation;

        product.Code = request.Code.Trim();
        product.Name = request.Name.Trim();
        product.CustomerName = request.CustomerName;
        product.Category = request.Category;
        product.ModelCode = request.ModelCode;
        product.Description = request.Description;
        product.FoamType = request.FoamType.Trim();
        product.ProductType = request.ProductType.Trim();
        product.IsFabric = request.IsFabric;
        product.IsAdhesive = request.IsAdhesive;
        product.HasDTFLabel = request.HasDTFLabel;
        product.HasPolibond = request.HasPolibond;
        product.AverageWeight = request.AverageWeight;
        product.TargetDensity = request.TargetDensity;
        product.StandardCycleTime = request.StandardCycleTime;
        product.DefaultBoxQuantity = request.DefaultBoxQuantity;
        product.IsActive = request.IsActive ?? product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(product, "Ürün güncellendi."));
    }

    private IActionResult? ValidateRequest(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Ürün adı zorunludur.", "NAME_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Ürün kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.FoamType) || !FoamTypes.Contains(request.FoamType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("FoamType sadece 10100 veya 10900 olabilir.", "INVALID_FOAM_TYPE"));

        if (string.IsNullOrWhiteSpace(request.ProductType) || !ProductTypes.Contains(request.ProductType.Trim()))
            return BadRequest(ApiResponse<object>.Fail("ProductType sadece Normal veya Memory Foam olabilir.", "INVALID_PRODUCT_TYPE"));

        return null;
    }
}

public record CreateProductRequest(
    string Code,
    string Name,
    string? CustomerName,
    string? Category,
    string? ModelCode,
    string? Description,
    string FoamType,
    string ProductType,
    bool IsFabric,
    bool IsAdhesive,
    bool HasDTFLabel,
    bool HasPolibond,
    decimal? AverageWeight,
    decimal? TargetDensity,
    decimal? StandardCycleTime,
    int? DefaultBoxQuantity,
    bool? IsActive
);
