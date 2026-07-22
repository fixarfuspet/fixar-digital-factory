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

[ApiController, ApiVersion("1.0"), Route("api/v{version:apiVersion}/finance-categories"), Authorize(Policy = AuthorizationPolicies.CanViewFinancialAccounts)]
public class FinanceCategoriesController(ApplicationDbContext db) : ControllerBase
{
    private static readonly string[] Types = ["Expense", "Income"];
    private static readonly string[] Behaviors = ["Fixed", "Variable"];

    [HttpGet]
    public async Task<IActionResult> List(string? categoryType, bool? isActive, CancellationToken ct)
    {
        var query = db.FinanceCategories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(categoryType)) query = query.Where(x => x.CategoryType == categoryType);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive);
        var rows = await query.OrderBy(x => x.CategoryType).ThenBy(x => x.Code).Select(x => new
        {
            x.Id, x.Code, x.Name, x.CategoryType, x.ParentCategoryId,
            ParentCategoryName = x.ParentCategory != null ? x.ParentCategory.Name : null,
            x.CostCenter, x.IncludeInProductionCost, x.CostBehavior, x.IsActive, x.Description
        }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpPost, Authorize(Policy = AuthorizationPolicies.CanManageFinancialAccounts), Idempotent]
    public async Task<IActionResult> Create(FinanceCategoryRequest request, CancellationToken ct)
    {
        var error = await Validate(request, null, ct);
        if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR"));
        var entity = new FinanceCategory
        {
            Id = Guid.NewGuid(), Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim(),
            CategoryType = request.CategoryType, ParentCategoryId = request.ParentCategoryId,
            CostCenter = request.CostCenter, IncludeInProductionCost = request.IncludeInProductionCost,
            CostBehavior = request.CostBehavior, Description = request.Description, IsActive = true
        };
        db.FinanceCategories.Add(entity);
        CustomerFinanceSupport.Audit(db, this, "Finance Category Created", entity.Id, request);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.Code }));
    }

    [HttpPut("{id:guid}"), Authorize(Policy = AuthorizationPolicies.CanManageFinancialAccounts)]
    public async Task<IActionResult> Update(Guid id, FinanceCategoryRequest request, CancellationToken ct)
    {
        var entity = await db.FinanceCategories.FindAsync([id], ct);
        if (entity is null) return NotFound(ApiResponse<object>.Fail("Finans kategorisi bulunamadı.", "NOT_FOUND"));
        var error = await Validate(request, id, ct);
        if (error is not null) return BadRequest(ApiResponse<object>.Fail(error, "VALIDATION_ERROR"));
        entity.Code = request.Code.Trim().ToUpperInvariant(); entity.Name = request.Name.Trim();
        entity.CategoryType = request.CategoryType; entity.ParentCategoryId = request.ParentCategoryId;
        entity.CostCenter = request.CostCenter; entity.IncludeInProductionCost = request.IncludeInProductionCost;
        entity.CostBehavior = request.CostBehavior; entity.Description = request.Description;
        CustomerFinanceSupport.Audit(db, this, "Finance Category Updated", entity.Id, request);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id }));
    }

    [HttpPost("{id:guid}/deactivate"), Authorize(Policy = AuthorizationPolicies.CanManageFinancialAccounts), Idempotent]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => SetActive(id, false, ct);

    [HttpPost("{id:guid}/activate"), Authorize(Policy = AuthorizationPolicies.CanManageFinancialAccounts), Idempotent]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct) => SetActive(id, true, ct);

    private async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken ct)
    {
        var entity = await db.FinanceCategories.FindAsync([id], ct);
        if (entity is null) return NotFound();
        entity.IsActive = active;
        CustomerFinanceSupport.Audit(db, this, active ? "Finance Category Activated" : "Finance Category Deactivated", id, new { active });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { entity.Id, entity.IsActive }));
    }

    private async Task<string?> Validate(FinanceCategoryRequest request, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name)) return "Kategori kodu ve adı zorunludur.";
        if (!Types.Contains(request.CategoryType)) return "Geçersiz kategori türü.";
        if (!Behaviors.Contains(request.CostBehavior)) return "Gider davranışı Fixed veya Variable olmalıdır.";
        if (request.ParentCategoryId == id) return "Kategori kendisinin üst kategorisi olamaz.";
        if (await db.FinanceCategories.AnyAsync(x => x.Code == request.Code.Trim().ToUpper() && x.Id != id, ct)) return "Kategori kodu zaten kullanılıyor.";
        if (request.ParentCategoryId.HasValue && !await db.FinanceCategories.AnyAsync(x => x.Id == request.ParentCategoryId && x.CategoryType == request.CategoryType, ct)) return "Üst kategori bulunamadı veya türü eşleşmiyor.";
        return null;
    }
}

public record FinanceCategoryRequest(string Code, string Name, string CategoryType, Guid? ParentCategoryId, string? CostCenter, bool IncludeInProductionCost, string CostBehavior, string? Description);
