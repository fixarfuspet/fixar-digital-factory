using Asp.Versioning;
using Fixar.API.Security;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Application.Features.Costing;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize(Policy = AuthorizationPolicies.CanViewCosts)]
[Route("api/v{version:apiVersion}/work-order-costs")]
public sealed class WorkOrderCostsController(ApplicationDbContext db, IWorkOrderCostService costs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid? workOrderId, Guid? customerId, Guid? productId, string? calculationType, DateTime? dateFrom, DateTime? dateTo, bool? isFinal, string? currency, string? search, CancellationToken ct)
    {
        var q = Query(); if (workOrderId.HasValue) q = q.Where(x => x.WorkOrderId == workOrderId); if (customerId.HasValue) q = q.Where(x => x.WorkOrder.OrderItem.Order.CustomerId == customerId); if (productId.HasValue) q = q.Where(x => x.WorkOrder.ProductId == productId); if (!string.IsNullOrWhiteSpace(calculationType)) q = q.Where(x => x.CalculationType == calculationType); if (dateFrom.HasValue) q = q.Where(x => x.SnapshotDate >= dateFrom); if (dateTo.HasValue) q = q.Where(x => x.SnapshotDate <= dateTo); if (isFinal.HasValue) q = q.Where(x => x.IsFinal == isFinal); if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.ReportingCurrency == currency.ToUpper()); if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.SnapshotNumber.Contains(search) || x.WorkOrder.WorkOrderNumber.Contains(search));
        return Ok(ApiResponse<object>.SuccessResponse((await q.OrderByDescending(x => x.SnapshotDate).ToListAsync(ct)).Select(WorkOrderCostService.ToListDto)));
    }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) { var x = await costs.GetCostBreakdownAsync(id, ct); return x is null ? NotFound(ApiResponse<object>.Fail("Maliyet snapshot'ı bulunamadı.", "COST_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x)); }
    [HttpGet("work-order/{workOrderId:guid}/preview")] public async Task<IActionResult> Preview(Guid workOrderId, string? currency, DateTime? at, CancellationToken ct) { try { return Ok(ApiResponse<object>.SuccessResponse(await costs.CalculateActualCostAsync(workOrderId, currency, at, ct))); } catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, "WORK_ORDER_NOT_FOUND")); } catch (InvalidOperationException ex) { return UnprocessableEntity(ApiResponse<object>.Fail(ex.Message, "COST_INPUT_MISSING")); } }
    [HttpPost("work-order/{workOrderId:guid}/calculate"), Authorize(Policy = AuthorizationPolicies.CanCalculateCosts), Idempotent]
    public async Task<IActionResult> Calculate(Guid workOrderId, CreateCostSnapshotRequest request, CancellationToken ct) { try { var x = await costs.CreateSnapshotAsync(workOrderId, request, ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id, x.SnapshotNumber }, "İş emri maliyeti hesaplandı.")); } catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, "WORK_ORDER_NOT_FOUND")); } catch (InvalidOperationException ex) { return UnprocessableEntity(ApiResponse<object>.Fail(ex.Message, "COST_INPUT_MISSING")); } }
    [HttpPost("{id:guid}/finalize"), Authorize(Policy = AuthorizationPolicies.CanFinalizeCosts), Idempotent]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct) { var x = await db.WorkOrderCostSnapshots.FirstOrDefaultAsync(x => x.Id == id, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("Maliyet snapshot'ı bulunamadı.", "COST_NOT_FOUND")); if (x.IsFinal) return Conflict(ApiResponse<object>.Fail("Bu maliyet snapshot’ı daha önce kesinleştirilmiş.", "ALREADY_FINAL")); x.IsFinal = true; x.CalculationType = "Final"; await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { x.Id, x.IsFinal }, "Maliyet snapshot'ı kesinleştirildi.")); }
    [HttpGet("work-order/{workOrderId:guid}/latest")] public async Task<IActionResult> Latest(Guid workOrderId, CancellationToken ct) { var x = await costs.GetLatestSnapshotAsync(workOrderId, ct); if (x is null) return NotFound(ApiResponse<object>.Fail("İş emri için maliyet snapshot'ı bulunamadı.", "COST_NOT_FOUND")); return Ok(ApiResponse<object>.SuccessResponse(WorkOrderCostService.ToListDto(await Query().FirstAsync(y => y.Id == x.Id, ct)))); }
    [HttpGet("summary")] public async Task<IActionResult> Summary(string? currency, CancellationToken ct) { var q = Query(); if (!string.IsNullOrWhiteSpace(currency)) q = q.Where(x => x.ReportingCurrency == currency.ToUpper()); var latestIds = await q.GroupBy(x => x.WorkOrderId).Select(g => g.OrderByDescending(x => x.SnapshotDate).Select(x => x.Id).First()).ToListAsync(ct); var rows = await q.Where(x => latestIds.Contains(x.Id)).ToListAsync(ct); return Ok(ApiResponse<object>.SuccessResponse(new { SnapshotCount = rows.Count, TotalEstimatedCost = rows.Sum(x => x.TotalEstimatedCost), TotalActualCost = rows.Sum(x => x.TotalActualCost), AverageCostPerGoodPair = rows.Where(x => x.ActualCostPerGoodPair.HasValue).Select(x => x.ActualCostPerGoodPair!.Value).DefaultIfEmpty().Average(), TotalSalesRevenue = rows.Sum(x => x.SalesRevenue), GrossProfit = rows.Sum(x => x.GrossProfit), GrossMarginPercent = rows.Sum(x => x.SalesRevenue) == 0 ? (decimal?)null : rows.Sum(x => x.GrossProfit) / rows.Sum(x => x.SalesRevenue) * 100, VarianceAmount = rows.Sum(x => x.VarianceAmount), Currency = currency?.ToUpperInvariant() })); }
    private IQueryable<Fixar.Domain.Entities.WorkOrderCostSnapshot> Query() => db.WorkOrderCostSnapshots.AsNoTracking().Include(x => x.WorkOrder).ThenInclude(x => x.Product).Include(x => x.WorkOrder).ThenInclude(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer);
}
