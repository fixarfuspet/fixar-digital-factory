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
[Route("api/v{version:apiVersion}/recipes")]
public class RecipesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RecipesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? productId, [FromQuery] bool? isActive, [FromQuery] string? search, [FromQuery] bool includeItems, CancellationToken cancellationToken)
    {
        var query = QueryRecipes();

        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term) ||
                x.Product.Code.ToLower().Contains(term) ||
                x.Product.Name.ToLower().Contains(term));
        }

        var recipes = await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Product.Name)
            .ThenByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        var response = includeItems
            ? recipes.Select(ToDetailResponse).ToList()
            : recipes.Select(ToListResponse).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await QueryRecipes().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (recipe is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(recipe)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveRecipeRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, cancellationToken);
        if (validation is not null)
            return validation;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            CreatedAt = utcNow
        };

        await ApplyRequest(recipe, request, utcNow, true, cancellationToken);
        _db.Recipes.Add(recipe);

        if (recipe.IsDefault)
            await ClearOtherDefaults(recipe.ProductId, recipe.Id, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await QueryRecipes().FirstAsync(x => x.Id == recipe.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created), "Reçete oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (recipe is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        var validation = await ValidateRequest(request, id, cancellationToken);
        if (validation is not null)
            return validation;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var code = request.Code.Trim();
        var name = request.Name.Trim();
        var outputUnit = string.IsNullOrWhiteSpace(request.OutputUnit) ? "Çift" : request.OutputUnit.Trim();
        var isDefault = request.IsDefault && request.IsActive;
        var effectiveFrom = request.EffectiveFrom.HasValue ? NormalizeUtc(request.EffectiveFrom.Value) : (DateTime?)null;
        var effectiveTo = request.EffectiveTo.HasValue ? NormalizeUtc(request.EffectiveTo.Value) : (DateTime?)null;

        await _db.RecipeItems
            .Where(x => x.RecipeId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (isDefault)
            await ClearOtherDefaults(request.ProductId, id, cancellationToken);

        var affectedRows = await _db.Recipes
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Code, code)
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.ProductId, request.ProductId)
                .SetProperty(x => x.Version, request.Version)
                .SetProperty(x => x.Description, request.Description)
                .SetProperty(x => x.OutputQuantity, request.OutputQuantity)
                .SetProperty(x => x.OutputUnit, outputUnit)
                .SetProperty(x => x.IsActive, request.IsActive)
                .SetProperty(x => x.IsDefault, isDefault)
                .SetProperty(x => x.EffectiveFrom, effectiveFrom)
                .SetProperty(x => x.EffectiveTo, effectiveTo)
                .SetProperty(x => x.Notes, request.Notes)
                .SetProperty(x => x.UpdatedAt, utcNow),
                cancellationToken);

        if (affectedRows == 0)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        var recipeItems = await BuildRecipeItems(id, request, utcNow, cancellationToken);
        _db.RecipeItems.AddRange(recipeItems);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updated = await QueryRecipes().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(updated), "Reçete güncellendi."));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        return await SetActive(id, true, cancellationToken);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        return await SetActive(id, false, cancellationToken);
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var recipe = await _db.Recipes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (recipe is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        if (!recipe.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif reçete varsayılan yapılamaz.", "RECIPE_INACTIVE"));

        await ClearOtherDefaults(recipe.ProductId, recipe.Id, cancellationToken);
        recipe.IsDefault = true;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updated = await QueryRecipes().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(updated), "Varsayılan reçete güncellendi."));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, [FromBody] DuplicateRecipeRequest request, CancellationToken cancellationToken)
    {
        var source = await QueryRecipes().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (source is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Yeni reçete kodu zorunludur.", "CODE_REQUIRED"));

        var version = request.Version ?? await NextVersion(source.ProductId, cancellationToken);
        var duplicateRequest = new SaveRecipeRequest(
            request.Code,
            string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} Kopya" : request.Name,
            source.ProductId,
            version,
            source.Description,
            source.OutputQuantity,
            source.OutputUnit,
            false,
            false,
            source.EffectiveFrom,
            source.EffectiveTo,
            source.Notes,
            source.Items
                .OrderBy(x => x.Sequence)
                .Select(x => new SaveRecipeItemRequest(x.MaterialId, x.Quantity, x.Unit, x.WastePercent, x.IsOptional, x.Sequence, x.Notes))
                .ToList());

        var validation = await ValidateRequest(duplicateRequest, null, cancellationToken);
        if (validation is not null)
            return validation;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            CreatedAt = utcNow
        };

        await ApplyRequest(recipe, duplicateRequest, utcNow, true, cancellationToken);
        _db.Recipes.Add(recipe);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await QueryRecipes().FirstAsync(x => x.Id == recipe.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created), "Reçete kopyalandı."));
    }

    [HttpGet("{id:guid}/cost")]
    public async Task<IActionResult> GetCost(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await QueryRecipes().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (recipe is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            recipe.Id,
            recipe.Code,
            TotalsByCurrency = CalculateTotalsByCurrency(recipe),
            Items = recipe.Items.OrderBy(x => x.Sequence).Select(ToItemResponse).ToList()
        }));
    }

    private IQueryable<Recipe> QueryRecipes()
    {
        return _db.Recipes
            .Include(x => x.Product)
            .Include(x => x.Items)
            .ThenInclude(x => x.Material);
    }

    private async Task<IActionResult?> ValidateRequest(SaveRecipeRequest request, Guid? recipeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.Fail("Reçete kodu zorunludur.", "CODE_REQUIRED"));

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.Fail("Reçete adı zorunludur.", "NAME_REQUIRED"));

        if (request.Version <= 0)
            return BadRequest(ApiResponse<object>.Fail("Reçete versiyonu pozitif olmalıdır.", "INVALID_VERSION"));

        if (request.OutputQuantity <= 0)
            return BadRequest(ApiResponse<object>.Fail("Çıktı miktarı sıfırdan büyük olmalıdır.", "INVALID_OUTPUT_QUANTITY"));

        if (request.EffectiveFrom.HasValue && request.EffectiveTo.HasValue && NormalizeUtc(request.EffectiveTo.Value) < NormalizeUtc(request.EffectiveFrom.Value))
            return BadRequest(ApiResponse<object>.Fail("Bitiş tarihi başlangıç tarihinden önce olamaz.", "INVALID_DATE_RANGE"));

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Reçetede en az bir malzeme bulunmalıdır.", "ITEMS_REQUIRED"));

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen ürün bulunamadı.", "PRODUCT_NOT_FOUND"));

        if (!product.IsActive)
            return BadRequest(ApiResponse<object>.Fail("Pasif ürün için reçete oluşturulamaz.", "PRODUCT_INACTIVE"));

        var code = request.Code.Trim();
        var codeExists = await _db.Recipes.AnyAsync(x => x.Code == code && (!recipeId.HasValue || x.Id != recipeId.Value), cancellationToken);
        if (codeExists)
            return BadRequest(ApiResponse<object>.Fail("Bu reçete kodu zaten kullanılıyor.", "RECIPE_CODE_EXISTS"));

        var versionExists = await _db.Recipes.AnyAsync(x => x.ProductId == request.ProductId && x.Version == request.Version && (!recipeId.HasValue || x.Id != recipeId.Value), cancellationToken);
        if (versionExists)
            return BadRequest(ApiResponse<object>.Fail("Aynı ürün ve versiyon için reçete zaten mevcut.", "PRODUCT_VERSION_EXISTS"));

        var materialIds = request.Items.Select(x => x.MaterialId).ToList();
        if (materialIds.GroupBy(x => x).Any(x => x.Count() > 1))
            return BadRequest(ApiResponse<object>.Fail("Aynı malzeme bir reçeteye birden fazla kez eklenemez.", "DUPLICATE_MATERIAL"));

        var materials = await _db.Materials.Where(x => materialIds.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(ApiResponse<object>.Fail("Malzeme miktarı sıfırdan büyük olmalıdır.", "INVALID_QUANTITY"));

            if (item.WastePercent < 0)
                return BadRequest(ApiResponse<object>.Fail("Fire/kayıp yüzdesi negatif olamaz.", "INVALID_WASTE_PERCENT"));

            var material = materials.FirstOrDefault(x => x.Id == item.MaterialId);
            if (material is null)
                return NotFound(ApiResponse<object>.Fail("Seçilen malzeme bulunamadı.", "MATERIAL_NOT_FOUND"));

            if (!material.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Pasif malzeme reçeteye eklenemez.", "MATERIAL_INACTIVE"));
        }

        return null;
    }

    private async Task ApplyRequest(Recipe recipe, SaveRecipeRequest request, DateTime utcNow, bool isCreate, CancellationToken cancellationToken)
    {
        recipe.Code = request.Code.Trim();
        recipe.Name = request.Name.Trim();
        recipe.ProductId = request.ProductId;
        recipe.Version = request.Version;
        recipe.Description = request.Description;
        recipe.OutputQuantity = request.OutputQuantity;
        recipe.OutputUnit = string.IsNullOrWhiteSpace(request.OutputUnit) ? "Çift" : request.OutputUnit.Trim();
        recipe.IsActive = request.IsActive;
        recipe.IsDefault = request.IsDefault && request.IsActive;
        recipe.EffectiveFrom = request.EffectiveFrom.HasValue ? NormalizeUtc(request.EffectiveFrom.Value) : null;
        recipe.EffectiveTo = request.EffectiveTo.HasValue ? NormalizeUtc(request.EffectiveTo.Value) : null;
        recipe.Notes = request.Notes;
        recipe.UpdatedAt = utcNow;

        if (!isCreate)
        {
            await _db.RecipeItems
                .Where(x => x.RecipeId == recipe.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        recipe.Items = await BuildRecipeItems(recipe.Id, request, utcNow, cancellationToken);
    }

    private async Task<List<RecipeItem>> BuildRecipeItems(Guid recipeId, SaveRecipeRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var materialIds = request.Items.Select(x => x.MaterialId).ToList();
        var materials = await _db.Materials.Where(x => materialIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        return request.Items
            .OrderBy(x => x.Sequence)
            .Select((item, index) =>
            {
                var material = materials[item.MaterialId];
                return new RecipeItem
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipeId,
                    MaterialId = item.MaterialId,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? material.Unit : item.Unit.Trim(),
                    WastePercent = item.WastePercent,
                    IsOptional = item.IsOptional,
                    Sequence = item.Sequence > 0 ? item.Sequence : index + 1,
                    Notes = item.Notes,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                };
            })
            .ToList();
    }

    private async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var recipe = await _db.Recipes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (recipe is null)
            return NotFound(ApiResponse<object>.Fail("Reçete bulunamadı.", "RECIPE_NOT_FOUND"));

        recipe.IsActive = isActive;
        recipe.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
            recipe.IsDefault = false;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updated = await QueryRecipes().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(updated), isActive ? "Reçete aktifleştirildi." : "Reçete pasif hale getirildi."));
    }

    private async Task ClearOtherDefaults(Guid productId, Guid recipeId, CancellationToken cancellationToken)
    {
        var otherDefaults = await _db.Recipes
            .Where(x => x.ProductId == productId && x.Id != recipeId && x.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var recipe in otherDefaults)
        {
            recipe.IsDefault = false;
            recipe.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<int> NextVersion(Guid productId, CancellationToken cancellationToken)
    {
        var latest = await _db.Recipes
            .Where(x => x.ProductId == productId)
            .MaxAsync(x => (int?)x.Version, cancellationToken);

        return (latest ?? 0) + 1;
    }

    private static object ToListResponse(Recipe recipe)
    {
        return new
        {
            recipe.Id,
            recipe.Code,
            recipe.Name,
            recipe.ProductId,
            ProductCode = recipe.Product.Code,
            ProductName = recipe.Product.Name,
            ProductFoamType = recipe.Product.FoamType,
            ProductType = recipe.Product.ProductType,
            recipe.Version,
            recipe.OutputQuantity,
            recipe.OutputUnit,
            ItemCount = recipe.Items.Count,
            recipe.IsActive,
            recipe.IsDefault,
            recipe.EffectiveFrom,
            recipe.EffectiveTo,
            TotalsByCurrency = CalculateTotalsByCurrency(recipe),
            recipe.CreatedAt,
            recipe.UpdatedAt
        };
    }

    private static object ToDetailResponse(Recipe recipe)
    {
        return new
        {
            recipe.Id,
            recipe.Code,
            recipe.Name,
            recipe.ProductId,
            ProductCode = recipe.Product.Code,
            ProductName = recipe.Product.Name,
            ProductFoamType = recipe.Product.FoamType,
            ProductType = recipe.Product.ProductType,
            recipe.Version,
            recipe.Description,
            recipe.OutputQuantity,
            recipe.OutputUnit,
            recipe.IsActive,
            recipe.IsDefault,
            recipe.EffectiveFrom,
            recipe.EffectiveTo,
            recipe.Notes,
            recipe.CreatedAt,
            recipe.UpdatedAt,
            recipe.CreatedBy,
            UpdatedBy = recipe.LastModifiedBy,
            TotalsByCurrency = CalculateTotalsByCurrency(recipe),
            Items = recipe.Items.OrderBy(x => x.Sequence).Select(ToItemResponse).ToList()
        };
    }

    private static object ToItemResponse(RecipeItem item)
    {
        var totalQuantity = item.Quantity + item.Quantity * item.WastePercent / 100;
        var unitPrice = item.Material.LastPurchasePrice ?? 0;
        var currency = string.IsNullOrWhiteSpace(item.Material.Currency) ? "TRY" : item.Material.Currency;

        return new
        {
            item.Id,
            item.RecipeId,
            item.MaterialId,
            MaterialCode = item.Material.Code,
            MaterialName = item.Material.Name,
            MaterialType = item.Material.MaterialType,
            item.Quantity,
            item.Unit,
            item.WastePercent,
            WasteQuantity = item.Quantity * item.WastePercent / 100,
            TotalQuantity = totalQuantity,
            item.IsOptional,
            item.Sequence,
            item.Notes,
            MaterialUnitPrice = unitPrice,
            Currency = currency,
            ItemCost = totalQuantity * unitPrice
        };
    }

    private static Dictionary<string, decimal> CalculateTotalsByCurrency(Recipe recipe)
    {
        return recipe.Items
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Material.Currency) ? "TRY" : x.Material.Currency!)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(item => (item.Quantity + item.Quantity * item.WastePercent / 100) * (item.Material.LastPurchasePrice ?? 0)));
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

public record SaveRecipeRequest(
    string Code,
    string Name,
    Guid ProductId,
    int Version,
    string? Description,
    decimal OutputQuantity,
    string OutputUnit,
    bool IsActive,
    bool IsDefault,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string? Notes,
    List<SaveRecipeItemRequest> Items
);

public record SaveRecipeItemRequest(
    Guid MaterialId,
    decimal Quantity,
    string? Unit,
    decimal WastePercent,
    bool IsOptional,
    int Sequence,
    string? Notes
);

public record DuplicateRecipeRequest(
    string Code,
    string? Name,
    int? Version
);
