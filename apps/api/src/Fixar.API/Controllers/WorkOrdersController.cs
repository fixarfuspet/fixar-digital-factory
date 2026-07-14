using System.Text.Json;
using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Domain.Enums;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/work-orders")]
public class WorkOrdersController : ControllerBase
{
    private static readonly string[] Statuses = { "Draft", "Planned", "Ready", "InProduction", "Paused", "Completed", "Cancelled" };
    private static readonly string[] Priorities = { "Normal", "High", "Urgent" };

    private readonly ApplicationDbContext _db;

    public WorkOrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? orderItemId,
        [FromQuery] Guid? machineId,
        [FromQuery] string? priority,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] DateTime? plannedFrom,
        [FromQuery] DateTime? plannedTo,
        CancellationToken cancellationToken)
    {
        var query = QueryWorkOrders();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (customerId.HasValue)
            query = query.Where(x => x.OrderItem.Order.CustomerId == customerId.Value);
        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId.Value);
        if (orderItemId.HasValue)
            query = query.Where(x => x.OrderItemId == orderItemId.Value);
        if (machineId.HasValue)
            query = query.Where(x => x.AssignedMachineId == machineId.Value);
        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(x => x.Priority == priority);
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);
        if (plannedFrom.HasValue)
            query = query.Where(x => x.PlannedStartDate >= NormalizeUtc(plannedFrom.Value));
        if (plannedTo.HasValue)
            query = query.Where(x => x.PlannedStartDate <= NormalizeUtc(plannedTo.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.WorkOrderNumber.ToLower().Contains(term) ||
                (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.ToLower().Contains(term)) ||
                (x.ProductCodeSnapshot != null && x.ProductCodeSnapshot.ToLower().Contains(term)) ||
                (x.ProductNameSnapshot != null && x.ProductNameSnapshot.ToLower().Contains(term)) ||
                x.OrderItem.Order.Customer.Name.ToLower().Contains(term) ||
                (x.OrderItem.Order.Customer.CompanyName != null && x.OrderItem.Order.Customer.CompanyName.ToLower().Contains(term)) ||
                x.Product.Name.ToLower().Contains(term));
        }

        var workOrders = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var progress = await LoadProgress(workOrders.Select(x => x.Id), cancellationToken);
        var materialSummaries = await LoadMaterialPlanningSummaries(workOrders, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(workOrders.Select(x => ToListResponse(x, GetProgress(progress, x.Id), GetMaterialSummary(materialSummaries, x.Id))).ToList()));
    }

    [HttpGet("available-for-planning")]
    public async Task<IActionResult> AvailableForPlanning(CancellationToken cancellationToken)
    {
        var workOrders = await QueryWorkOrders()
            .Where(x => x.IsActive && !x.IsCancelled && (x.Status == "Planned" || x.Status == "Ready"))
            .OrderBy(x => x.PlannedStartDate)
            .ThenBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        var progress = await LoadProgress(workOrders.Select(x => x.Id), cancellationToken);
        var materialSummaries = await LoadMaterialPlanningSummaries(workOrders, cancellationToken);
        var response = workOrders
            .Select(x => ToAvailableResponse(x, GetProgress(progress, x.Id), GetMaterialSummary(materialSummaries, x.Id)))
            .Where(x => x.RemainingToAssignPairs > 0)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        var progress = await LoadProgress(new[] { id }, cancellationToken);
        var requirements = await CalculateRequirements(workOrder, cancellationToken);
        var materialSummary = requirements?.Summary ?? MaterialPlanningSummary.NoRecipe;
        var assignments = await _db.StationAssignments
            .Where(x => x.WorkOrderId == id)
            .OrderBy(x => x.StationNumberSnapshot)
            .Select(x => new
            {
                x.Id,
                x.StationNumberSnapshot,
                x.Status,
                x.OperatorName,
                x.ProducedPairs,
                x.FirePairs,
                x.StartedAt,
                x.FinishedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(workOrder, GetProgress(progress, id), materialSummary, requirements, assignments)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, null, null, cancellationToken);
        if (validation is not null)
            return validation;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var orderItem = await LoadOrderItem(request.OrderItemId, cancellationToken);
        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        var product = ResolveProduct(orderItem);
        var utcNow = DateTime.UtcNow;
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            WorkOrderNumber = await GenerateWorkOrderNumber(utcNow, cancellationToken),
            CreatedAt = utcNow
        };

        ApplyRequest(workOrder, request, orderItem, product, utcNow);
        _db.WorkOrders.Add(workOrder);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await QueryWorkOrders().FirstAsync(x => x.Id == workOrder.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created, WorkOrderProgress.Empty, MaterialPlanningSummary.NoRecipe, null, Array.Empty<object>()), "İş emri oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        if (workOrder.Status is "Completed" or "Cancelled")
            return BadRequest(ApiResponse<object>.Fail(workOrder.Status == "Completed" ? "Tamamlanmış iş emri düzenlenemez." : "İptal edilmiş iş emri düzenlenemez.", "WORK_ORDER_LOCKED"));

        if (workOrder.Status == "InProduction" && request.OrderItemId != workOrder.OrderItemId)
            return BadRequest(ApiResponse<object>.Fail("Bu iş emri üretimde olduğu için ürün veya sipariş kalemi değiştirilemez.", "WORK_ORDER_IN_PRODUCTION"));

        var progress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        if (request.PlannedPairs < progress.AssignedPairs || request.PlannedPairs < progress.ProducedPairs)
            return BadRequest(ApiResponse<object>.Fail("Planlanan miktar atanmış veya üretilmiş miktarın altına düşemez.", "PLANNED_BELOW_PROGRESS"));

        var validation = await ValidateRequest(request, id, workOrder, cancellationToken);
        if (validation is not null)
            return validation;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var orderItem = await LoadOrderItem(request.OrderItemId, cancellationToken);
        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        ApplyRequest(workOrder, request, orderItem, ResolveProduct(orderItem), DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updated = await QueryWorkOrders().FirstAsync(x => x.Id == id, cancellationToken);
        var updatedProgress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        var requirements = await CalculateRequirements(updated, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(updated, updatedProgress, requirements?.Summary ?? MaterialPlanningSummary.NoRecipe, requirements, Array.Empty<object>()), "İş emri güncellendi."));
    }

    [HttpPost("{id:guid}/plan")]
    public Task<IActionResult> Plan(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "Planned", cancellationToken);

    [HttpPost("{id:guid}/mark-ready")]
    public Task<IActionResult> MarkReady(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "Ready", cancellationToken);

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] StartWorkOrderRequest? request, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        if (!CanTransition(workOrder.Status, "InProduction"))
            return BadRequest(ApiResponse<object>.Fail("Bu iş emri için durum geçişi geçersiz.", "INVALID_STATUS_TRANSITION"));

        var requirements = await CalculateRequirements(workOrder, cancellationToken);
        if (requirements is null)
        {
            await AddAuditLog("WorkOrder Start Blocked by Shortage", workOrder.Id, new
            {
                Reason = "RecipeRequired",
                workOrder.WorkOrderNumber
            }, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return BadRequest(ApiResponse<object>.Fail("İş emrinin üretime başlaması için aktif reçete zorunludur.", "RECIPE_REQUIRED"));
        }

        await AddAuditLog("WorkOrder Material Check", workOrder.Id, new
        {
            workOrder.WorkOrderNumber,
            requirements.HasShortage,
            requirements.ShortageMaterialCount,
            requirements.CanStartProduction,
            requirements.Warnings
        }, cancellationToken);

        var hasBlockingIssue = requirements.Items.Count == 0 ||
            requirements.Items.Any(x => x.IsUnitMismatch || x.StockItemId is null) ||
            requirements.HasShortage;

        if (hasBlockingIssue)
        {
            if (request?.AllowMaterialShortage != true)
            {
                await AddAuditLog("WorkOrder Start Blocked by Shortage", workOrder.Id, new
                {
                    workOrder.WorkOrderNumber,
                    requirements.ShortageMaterialCount,
                    requirements.Warnings
                }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return BadRequest(ApiResponse<object>.Fail("İş emrinin üretime başlaması için yeterli hammadde bulunmuyor.", "MATERIAL_SHORTAGE"));
            }

            if (string.IsNullOrWhiteSpace(request.ShortageReason))
            {
                await _db.SaveChangesAsync(cancellationToken);
                return BadRequest(ApiResponse<object>.Fail("Hammadde eksikliği override nedeni zorunludur.", "SHORTAGE_REASON_REQUIRED"));
            }

            if (requirements.Items.Any(x => x.IsUnitMismatch))
            {
                await _db.SaveChangesAsync(cancellationToken);
                return BadRequest(ApiResponse<object>.Fail("Birim uyumsuzluğu bulunan iş emri üretime başlatılamaz.", "UNIT_CONVERSION_UNSUPPORTED"));
            }

            await AddAuditLog("WorkOrder Started with Material Shortage Override", workOrder.Id, new
            {
                workOrder.WorkOrderNumber,
                ShortageReason = request.ShortageReason.Trim(),
                requirements.ShortageMaterialCount,
                requirements.Warnings
            }, cancellationToken);
        }

        if (!requirements.IsFullyReserved)
        {
            if (request?.AllowStartWithoutReservation != true)
            {
                await AddAuditLog("WorkOrder Start Blocked By Reservation", workOrder.Id, new { workOrder.WorkOrderNumber, requirements.UnreservedMaterialCount, requirements.PartiallyReservedMaterialCount }, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return BadRequest(ApiResponse<object>.Fail("İş emrinin zorunlu hammaddeleri tam olarak rezerve edilmemiştir.", "MATERIAL_RESERVATION_REQUIRED"));
            }
            if (string.IsNullOrWhiteSpace(request.ReservationOverrideReason))
                return BadRequest(ApiResponse<object>.Fail("Rezervasyonsuz başlatma override gerekçesi zorunludur.", "RESERVATION_OVERRIDE_REASON_REQUIRED"));
            await AddAuditLog("WorkOrder Started Without Reservation Override", workOrder.Id, new { workOrder.WorkOrderNumber, Reason = request.ReservationOverrideReason.Trim() }, cancellationToken);
        }

        var utcNow = DateTime.UtcNow;
        workOrder.Status = "InProduction";
        workOrder.UpdatedAt = utcNow;
        workOrder.UpdatedBy = GetActor();
        workOrder.ActualStartDate ??= utcNow;

        await _db.SaveChangesAsync(cancellationToken);
        var progress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, progress, requirements.Summary), "İş emri durumu güncellendi."));
    }

    [HttpPost("{id:guid}/pause")]
    public Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "Paused", cancellationToken);

    [HttpPost("{id:guid}/resume")]
    public Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "InProduction", cancellationToken);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        if (workOrder.Status is "Completed" or "Cancelled")
            return BadRequest(ApiResponse<object>.Fail("Terminal durumdaki iş emri yeniden işleme alınamaz.", "WORK_ORDER_TERMINAL"));

        var progress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        if (progress.ProducedPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Üretim kaydı bulunmayan iş emri tamamlanamaz.", "NO_PRODUCTION"));

        var hasOpenAssignment = await _db.StationAssignments.AnyAsync(x => x.WorkOrderId == id && x.FinishedAt == null, cancellationToken);
        if (hasOpenAssignment)
            return BadRequest(ApiResponse<object>.Fail("Açık istasyon ataması bulunan iş emri tamamlanamaz.", "OPEN_ASSIGNMENT_EXISTS"));

        if (progress.ProducedPairs < workOrder.PlannedPairs && !request.AllowShortCompletion)
            return BadRequest(ApiResponse<object>.Fail("Planlanan miktarın altında tamamlama için onay gerekir.", "SHORT_COMPLETION_CONFIRMATION_REQUIRED"));

        workOrder.Status = "Completed";
        workOrder.ActualEndDate = DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;
        workOrder.UpdatedBy = GetActor();
        await _db.SaveChangesAsync(cancellationToken);

        var requirements = await CalculateRequirements(workOrder, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, progress, requirements?.Summary ?? MaterialPlanningSummary.NoRecipe), "İş emri tamamlandı."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CancellationReason))
            return BadRequest(ApiResponse<object>.Fail("İptal nedeni zorunludur.", "CANCELLATION_REASON_REQUIRED"));

        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        if (workOrder.Status == "Completed")
            return BadRequest(ApiResponse<object>.Fail("Tamamlanmış iş emri iptal edilemez.", "WORK_ORDER_COMPLETED"));

        if (workOrder.Status == "Cancelled")
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş iş emri tekrar iptal edilemez.", "WORK_ORDER_CANCELLED"));

        var hasOpenAssignment = await _db.StationAssignments.AnyAsync(x => x.WorkOrderId == id && x.FinishedAt == null, cancellationToken);
        if (hasOpenAssignment)
            return BadRequest(ApiResponse<object>.Fail("Açık istasyon ataması bulunan iş emri iptal edilemez.", "OPEN_ASSIGNMENT_EXISTS"));

        var utcNow = DateTime.UtcNow;
        workOrder.Status = "Cancelled";
        workOrder.IsCancelled = true;
        workOrder.IsActive = false;
        workOrder.CancellationReason = request.CancellationReason.Trim();
        workOrder.CancelledAt = utcNow;
        workOrder.CancelledBy = GetActor();
        workOrder.UpdatedAt = utcNow;
        workOrder.UpdatedBy = GetActor();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, WorkOrderProgress.Empty, MaterialPlanningSummary.NoRecipe), "İş emri iptal edildi."));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var source = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (source is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var duplicate = new WorkOrder
        {
            Id = Guid.NewGuid(),
            WorkOrderNumber = await GenerateWorkOrderNumber(utcNow, cancellationToken),
            OrderItemId = source.OrderItemId,
            ProductId = source.ProductId,
            RecipeId = source.RecipeId,
            CustomerNameSnapshot = source.CustomerNameSnapshot,
            ProductCodeSnapshot = source.ProductCodeSnapshot,
            ProductNameSnapshot = source.ProductNameSnapshot,
            PlannedPairs = source.PlannedPairs,
            Priority = source.Priority,
            Status = "Draft",
            PlannedStartDate = source.PlannedStartDate,
            PlannedEndDate = source.PlannedEndDate,
            AssignedMachineId = source.AssignedMachineId,
            Shift = source.Shift,
            Notes = source.Notes,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.WorkOrders.Add(duplicate);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var created = await QueryWorkOrders().FirstAsync(x => x.Id == duplicate.Id, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created, WorkOrderProgress.Empty, MaterialPlanningSummary.NoRecipe, null, Array.Empty<object>()), "İş emri kopyalandı."));
    }

    [HttpGet("{id:guid}/requirements")]
    public async Task<IActionResult> Requirements(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        var requirements = await CalculateRequirements(workOrder, cancellationToken);
        if (requirements is null)
            return BadRequest(ApiResponse<object>.Fail("İş emrine bağlı aktif reçete bulunamadı.", "RECIPE_REQUIRED"));

        return Ok(ApiResponse<object>.SuccessResponse(requirements));
    }

    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> Progress(Guid id, CancellationToken cancellationToken)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        var progress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        return Ok(ApiResponse<object>.SuccessResponse(ToProgressResponse(workOrder, progress)));
    }

    private IQueryable<WorkOrder> QueryWorkOrders()
    {
        return _db.WorkOrders
            .Include(x => x.OrderItem).ThenInclude(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.OrderItem).ThenInclude(x => x.Product)
            .Include(x => x.Product)
            .Include(x => x.Recipe)
            .Include(x => x.AssignedMachine);
    }

    private async Task<IActionResult?> ValidateRequest(SaveWorkOrderRequest request, Guid? id, WorkOrder? current, CancellationToken cancellationToken)
    {
        if (request.PlannedPairs <= 0)
            return BadRequest(ApiResponse<object>.Fail("Planlanan miktar sıfırdan büyük olmalıdır.", "INVALID_PLANNED_PAIRS"));

        if (!Priorities.Contains(request.Priority))
            return BadRequest(ApiResponse<object>.Fail("Öncelik değeri geçersiz.", "INVALID_PRIORITY"));

        if (request.Shift is not null && request.Shift is < 1 or > 3)
            return BadRequest(ApiResponse<object>.Fail("Vardiya 1, 2 veya 3 olmalıdır.", "INVALID_SHIFT"));

        if (request.PlannedStartDate.HasValue && request.PlannedEndDate.HasValue &&
            NormalizeUtc(request.PlannedEndDate.Value) < NormalizeUtc(request.PlannedStartDate.Value))
            return BadRequest(ApiResponse<object>.Fail("Planlanan bitiş tarihi başlangıçtan önce olamaz.", "INVALID_DATE_RANGE"));

        var orderItem = await LoadOrderItem(request.OrderItemId, cancellationToken);
        if (orderItem is null)
            return NotFound(ApiResponse<object>.Fail("Seçilen sipariş kalemi bulunamadı.", "ORDER_ITEM_NOT_FOUND"));

        if (orderItem.Status == "İptal")
            return BadRequest(ApiResponse<object>.Fail("İptal edilmiş sipariş kalemi için iş emri açılamaz.", "ORDER_ITEM_CANCELLED"));

        var product = ResolveProduct(orderItem);
        if (product is null)
            return BadRequest(ApiResponse<object>.Fail("Sipariş kalemine bağlı ürün bulunamadı.", "PRODUCT_NOT_FOUND"));

        if (request.RecipeId.HasValue)
        {
            var recipe = await _db.Recipes.FirstOrDefaultAsync(x => x.Id == request.RecipeId.Value, cancellationToken);
            if (recipe is null)
                return NotFound(ApiResponse<object>.Fail("Seçilen reçete bulunamadı.", "RECIPE_NOT_FOUND"));
            if (!recipe.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Pasif reçete iş emrine bağlanamaz.", "RECIPE_INACTIVE"));
            if (recipe.ProductId != product.Id)
                return BadRequest(ApiResponse<object>.Fail("Seçilen reçete bu ürüne ait değil.", "RECIPE_PRODUCT_MISMATCH"));
        }

        if (request.AssignedMachineId.HasValue)
        {
            var machine = await _db.Machines.FirstOrDefaultAsync(x => x.Id == request.AssignedMachineId.Value, cancellationToken);
            if (machine is null)
                return NotFound(ApiResponse<object>.Fail("Seçilen makine bulunamadı.", "MACHINE_NOT_FOUND"));
            if (!machine.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Pasif makine iş emrine atanamaz.", "MACHINE_INACTIVE"));
        }

        var orderRemainingPairs = Math.Max(orderItem.QuantityPairs - orderItem.ProducedPairs, 0);
        var otherOpenPlannedPairs = await _db.WorkOrders
            .Where(x => x.OrderItemId == request.OrderItemId &&
                x.Status != "Completed" &&
                x.Status != "Cancelled" &&
                (!id.HasValue || x.Id != id.Value))
            .SumAsync(x => x.PlannedPairs, cancellationToken);

        var availablePairs = orderRemainingPairs - otherOpenPlannedPairs;
        if (request.PlannedPairs > availablePairs)
            return BadRequest(ApiResponse<object>.Fail("Planlanan miktar siparişin kalan miktarını aşıyor.", "PLANNED_EXCEEDS_REMAINING"));

        return null;
    }

    private async Task<IActionResult> ChangeStatus(Guid id, string nextStatus, CancellationToken cancellationToken)
    {
        var workOrder = await QueryWorkOrders().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workOrder is null)
            return NotFound(ApiResponse<object>.Fail("İş emri bulunamadı.", "WORK_ORDER_NOT_FOUND"));

        if (!CanTransition(workOrder.Status, nextStatus))
            return BadRequest(ApiResponse<object>.Fail("Bu iş emri için durum geçişi geçersiz.", "INVALID_STATUS_TRANSITION"));

        var utcNow = DateTime.UtcNow;
        workOrder.Status = nextStatus;
        workOrder.UpdatedAt = utcNow;
        workOrder.UpdatedBy = GetActor();
        if (nextStatus == "InProduction" && workOrder.ActualStartDate is null)
            workOrder.ActualStartDate = utcNow;

        await _db.SaveChangesAsync(cancellationToken);
        var progress = GetProgress(await LoadProgress(new[] { id }, cancellationToken), id);
        var materialSummaries = await LoadMaterialPlanningSummaries(new[] { workOrder }, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, progress, GetMaterialSummary(materialSummaries, id)), "İş emri durumu güncellendi."));
    }

    private static WorkOrderProgress GetProgress(IReadOnlyDictionary<Guid, WorkOrderProgress> progress, Guid id)
        => progress.TryGetValue(id, out var value) ? value : WorkOrderProgress.Empty;

    private static MaterialPlanningSummary GetMaterialSummary(IReadOnlyDictionary<Guid, MaterialPlanningSummary> summaries, Guid id)
        => summaries.TryGetValue(id, out var value) ? value : MaterialPlanningSummary.NoRecipe;

    private async Task<Dictionary<Guid, MaterialPlanningSummary>> LoadMaterialPlanningSummaries(IEnumerable<WorkOrder> workOrders, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, MaterialPlanningSummary>();
        foreach (var workOrder in workOrders)
        {
            var requirements = await CalculateRequirements(workOrder, cancellationToken);
            result[workOrder.Id] = requirements?.Summary ?? MaterialPlanningSummary.NoRecipe;
        }

        return result;
    }

    private static bool CanTransition(string current, string next)
    {
        if (current is "Completed" or "Cancelled")
            return false;

        return (current, next) switch
        {
            ("Draft", "Planned") => true,
            ("Planned", "Ready") => true,
            ("Ready", "InProduction") => true,
            ("InProduction", "Paused") => true,
            ("Paused", "InProduction") => true,
            ("InProduction", "Completed") => true,
            ("Draft", "Cancelled") => true,
            ("Planned", "Cancelled") => true,
            ("Ready", "Cancelled") => true,
            ("Paused", "Cancelled") => true,
            _ => false
        };
    }

    private void ApplyRequest(WorkOrder workOrder, SaveWorkOrderRequest request, OrderItem orderItem, Product? product, DateTime utcNow)
    {
        workOrder.OrderItemId = request.OrderItemId;
        workOrder.ProductId = product?.Id ?? orderItem.Order.ProductId;
        workOrder.RecipeId = request.RecipeId;
        workOrder.CustomerNameSnapshot = orderItem.Order.Customer.CompanyName ?? orderItem.Order.Customer.Name;
        workOrder.ProductCodeSnapshot = product?.Code;
        workOrder.ProductNameSnapshot = product?.Name;
        workOrder.PlannedPairs = request.PlannedPairs;
        workOrder.Priority = request.Priority;
        workOrder.PlannedStartDate = request.PlannedStartDate.HasValue ? NormalizeUtc(request.PlannedStartDate.Value) : null;
        workOrder.PlannedEndDate = request.PlannedEndDate.HasValue ? NormalizeUtc(request.PlannedEndDate.Value) : null;
        workOrder.AssignedMachineId = request.AssignedMachineId;
        workOrder.Shift = request.Shift;
        workOrder.Notes = request.Notes;
        workOrder.IsActive = request.IsActive;
        workOrder.UpdatedAt = utcNow;
        workOrder.UpdatedBy = GetActor();
    }

    private async Task<OrderItem?> LoadOrderItem(Guid orderItemId, CancellationToken cancellationToken)
    {
        return await _db.OrderItems
            .Include(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.Order).ThenInclude(x => x.Product)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderItemId, cancellationToken);
    }

    private static Product? ResolveProduct(OrderItem orderItem) => orderItem.Product ?? orderItem.Order.Product;

    private async Task<string> GenerateWorkOrderNumber(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"WO-{utcNow:yyyyMMdd}-";
        var latest = await _db.WorkOrders
            .Where(x => x.WorkOrderNumber.StartsWith(prefix))
            .OrderByDescending(x => x.WorkOrderNumber)
            .Select(x => x.WorkOrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(latest) && int.TryParse(latest[prefix.Length..], out var parsed))
            sequence = parsed + 1;

        return $"{prefix}{sequence:0000}";
    }

    private async Task<Dictionary<Guid, WorkOrderProgress>> LoadProgress(IEnumerable<Guid> workOrderIds, CancellationToken cancellationToken)
    {
        var ids = workOrderIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, WorkOrderProgress>();

        var rows = await _db.StationAssignments
            .Where(x => x.WorkOrderId.HasValue && ids.Contains(x.WorkOrderId.Value))
            .GroupBy(x => x.WorkOrderId!.Value)
            .Select(x => new
            {
                WorkOrderId = x.Key,
                AssignedPairs = x.Sum(y => y.PlannedPairs),
                ProducedPairs = x.Sum(y => y.ProducedPairs),
                FirePairs = x.Sum(y => y.FirePairs)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.WorkOrderId, x =>
        {
            var good = Math.Max(x.ProducedPairs - x.FirePairs, 0);
            return new WorkOrderProgress(x.AssignedPairs, x.ProducedPairs, good, x.FirePairs);
        });
    }

    private async Task<WorkOrderRequirementsResponse?> CalculateRequirements(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (!workOrder.RecipeId.HasValue)
            return null;

        var recipe = await _db.Recipes
            .Include(x => x.Product)
            .Include(x => x.Items)
            .ThenInclude(x => x.Material)
            .FirstOrDefaultAsync(x => x.Id == workOrder.RecipeId.Value && x.IsActive, cancellationToken);

        if (recipe is null || recipe.Items.Count == 0)
            return null;

        var materialIds = recipe.Items.Select(x => x.MaterialId).ToList();
        var materialCodes = recipe.Items.Select(x => x.Material.Code).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var stockItems = await _db.StockItems
            .Where(x =>
                (x.MaterialId.HasValue && materialIds.Contains(x.MaterialId.Value)) ||
                (x.Code != null && materialCodes.Contains(x.Code)))
            .ToListAsync(cancellationToken);

        var purchasePrices = await _db.PurchaseOrderLines
            .Include(x => x.PurchaseOrder)
            .Include(x => x.StockItem)
            .Where(x => x.StockItem.MaterialId.HasValue && materialIds.Contains(x.StockItem.MaterialId.Value) && x.PurchaseOrder.Status != "Cancelled")
            .OrderByDescending(x => x.PurchaseOrder.OrderDate)
            .ToListAsync(cancellationToken);

        var activeReservations = await _db.StockReservationLines.AsNoTracking()
            .Where(x => x.StockReservation.Status == "Active" && materialIds.Contains(x.MaterialId))
            .GroupBy(x => new { x.StockReservation.WorkOrderId, x.MaterialId })
            .Select(x => new { x.Key.WorkOrderId, x.Key.MaterialId, Quantity = x.Sum(y => y.ReservedQuantity - y.ReleasedQuantity) })
            .ToListAsync(cancellationToken);
        var availableLots = await _db.MaterialLots.AsNoTracking().Where(x => materialIds.Contains(x.MaterialId) && x.IsActive && !x.IsBlocked && x.QualityStatus != "Rejected" && (x.ExpiryDate == null || x.ExpiryDate >= DateTime.UtcNow)).GroupBy(x => x.MaterialId).Select(x => new { MaterialId = x.Key, Quantity = x.Sum(y => y.CurrentQuantity - y.ReservedQuantity) }).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, cancellationToken);
        var availableContainers = await _db.MaterialContainers.AsNoTracking().Where(x => materialIds.Contains(x.MaterialId) && x.IsActive && !x.IsBlocked && !x.IsDamaged && x.Status != "Empty" && x.Status != "Cancelled").GroupBy(x => x.MaterialId).Select(x => new { MaterialId = x.Key, Quantity = x.Sum(y => y.CurrentQuantity - y.ReservedQuantity) }).ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, cancellationToken);

        var warnings = new List<string>();
        var items = recipe.Items
            .OrderBy(x => x.Sequence)
            .Select(item =>
            {
                var rowWarnings = new List<string>();
                var matchedStocks = stockItems
                    .Where(x => x.MaterialId == item.MaterialId ||
                        (!x.MaterialId.HasValue && string.Equals(x.Code, item.Material.Code, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (matchedStocks.Count > 1)
                    rowWarnings.Add($"{item.Material.Code} malzemesi için birden fazla stok kartı bulundu.");

                var stock = matchedStocks.Count == 1 ? matchedStocks[0] : null;
                if (stock is null)
                    rowWarnings.Add("Malzeme için bağlı stok kartı bulunamadı.");

                var stockUnit = stock?.Unit ?? item.Material.Unit;
                var conversion = ResolveUnitConversion(item.Unit, stockUnit);
                if (!conversion.IsSupported)
                    rowWarnings.Add("Reçete birimi ile stok birimi arasında desteklenen bir dönüşüm bulunamadı.");

                var netRequired = recipe.OutputQuantity > 0
                    ? item.Quantity * workOrder.PlannedPairs / recipe.OutputQuantity
                    : 0;
                var wasteQuantity = netRequired * item.WastePercent / 100;
                var totalRequiredRecipeUnit = netRequired + wasteQuantity;
                var totalRequiredStockUnit = conversion.IsSupported ? totalRequiredRecipeUnit * conversion.Factor : totalRequiredRecipeUnit;
                var availableStock = stock?.CurrentQuantity ?? 0;
                var reservedForThisWorkOrder = activeReservations.Where(x => x.WorkOrderId == workOrder.Id && x.MaterialId == item.MaterialId).Sum(x => x.Quantity);
                var reservedForOtherWorkOrders = activeReservations.Where(x => x.WorkOrderId != workOrder.Id && x.MaterialId == item.MaterialId).Sum(x => x.Quantity);
                var reservedQuantity = reservedForThisWorkOrder + reservedForOtherWorkOrders;
                var freeStock = Math.Max(availableStock - reservedQuantity, 0);
                var shortageQuantity = conversion.IsSupported ? Math.Max(totalRequiredStockUnit - freeStock, 0) : totalRequiredStockUnit;
                var coveragePercent = totalRequiredStockUnit > 0 && conversion.IsSupported
                    ? Math.Min(100, freeStock / totalRequiredStockUnit * 100)
                    : 0;
                var price = ResolveMaterialPrice(item.Material, stock, purchasePrices);
                if (price.UnitPrice is null)
                    rowWarnings.Add("Malzeme için güncel alış fiyatı bulunamadı.");

                warnings.AddRange(rowWarnings.Select(x => $"{item.Material.Code}: {x}"));
                return new WorkOrderRequirementItemResponse(
                    item.MaterialId,
                    item.Material.Code,
                    item.Material.Name,
                    Round(item.Quantity),
                    item.Unit,
                    Round(item.WastePercent),
                    Round(netRequired),
                    Round(wasteQuantity),
                    Round(totalRequiredStockUnit),
                    stock?.Id,
                    stock?.Code,
                    Round(availableStock),
                    Round(reservedQuantity),
                    Round(freeStock),
                    stockUnit,
                    conversion.Applied,
                    conversion.IsSupported ? Round(conversion.Factor) : null,
                    Round(shortageQuantity),
                    Round(coveragePercent),
                    conversion.IsSupported && stock is not null && shortageQuantity <= 0,
                    price.UnitPrice,
                    price.Currency,
                    price.UnitPrice.HasValue ? Round(totalRequiredStockUnit * price.UnitPrice.Value) : null,
                    rowWarnings.Count > 0 ? string.Join(" ", rowWarnings) : null,
                    !conversion.IsSupported,
                    Round(reservedForThisWorkOrder),
                    Round(reservedForOtherWorkOrders),
                    Round(reservedQuantity),
                    Round(freeStock),
                    Round(availableLots.GetValueOrDefault(item.MaterialId)),
                    Round(availableContainers.GetValueOrDefault(item.MaterialId)),
                    reservedForThisWorkOrder <= 0 ? "Unreserved" : reservedForThisWorkOrder < totalRequiredStockUnit ? "Partial" : "FullyReserved",
                    reservedForThisWorkOrder >= totalRequiredStockUnit,
                    Round(Math.Max(totalRequiredStockUnit - reservedForThisWorkOrder, 0)),
                    conversion.IsSupported && stock is not null && reservedForThisWorkOrder < totalRequiredStockUnit);
            })
            .ToList();

        var totalsByCurrency = items
            .Where(x => x.EstimatedMaterialCost.HasValue)
            .GroupBy(x => x.Currency)
            .ToDictionary(x => x.Key, x => Round(x.Sum(y => y.EstimatedMaterialCost!.Value)));
        var sufficientCount = items.Count(x => x.IsSufficient);
        var shortageCount = items.Count(x => x.ShortageQuantity > 0 || x.StockItemId is null || x.IsUnitMismatch);
        var totalCoverage = items.Count > 0 ? Round(items.Average(x => x.CoveragePercent)) : 0;
        var hasShortage = shortageCount > 0;
        var canStart = items.Count > 0 && !hasShortage && items.All(x => !x.IsUnitMismatch);
        var summary = new MaterialPlanningSummary(
            HasRecipe: true,
            HasMaterialShortage: hasShortage,
            ShortageMaterialCount: shortageCount,
            MaterialCoveragePercent: totalCoverage,
            EstimatedMaterialCostByCurrency: totalsByCurrency,
            CanStartProduction: canStart,
            MaterialWarnings: warnings);

        var reservationCount = await _db.StockReservations.CountAsync(x => x.WorkOrderId == workOrder.Id && x.Status == "Active", cancellationToken);
        var fullyReservedCount = items.Count(x => x.IsFullyReserved); var partiallyReservedCount = items.Count(x => x.ReservedForThisWorkOrder > 0 && !x.IsFullyReserved); var unreservedCount = items.Count - fullyReservedCount - partiallyReservedCount;
        return new WorkOrderRequirementsResponse(
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.ProductId,
            workOrder.ProductCodeSnapshot ?? workOrder.Product.Code,
            workOrder.ProductNameSnapshot ?? workOrder.Product.Name,
            recipe.Id,
            recipe.Code,
            recipe.Name,
            workOrder.PlannedPairs,
            Round(recipe.OutputQuantity),
            recipe.OutputUnit,
            items.Count,
            sufficientCount,
            shortageCount,
            hasShortage,
            canStart,
            totalsByCurrency,
            DateTime.UtcNow,
            warnings,
            items,
            items
                .Where(x => x.ShortageQuantity > 0)
                .Select(x => new PurchaseSuggestionResponse(
                    x.MaterialId,
                    x.MaterialCode,
                    x.MaterialName,
                    x.ShortageQuantity,
                    x.StockUnit,
                    recipe.Items.First(y => y.MaterialId == x.MaterialId).Material.DefaultSupplierId,
                    recipe.Items.First(y => y.MaterialId == x.MaterialId).Material.DefaultSupplierName,
                    x.MaterialUnitPrice,
                    x.Currency))
                .ToList(),
            summary,
            reservationCount > 0,
            reservationCount,
            fullyReservedCount,
            partiallyReservedCount,
            unreservedCount,
            items.Count > 0 && fullyReservedCount == items.Count,
            canStart && items.Count > 0 && fullyReservedCount == items.Count);
    }

    private static UnitConversionResult ResolveUnitConversion(string recipeUnit, string stockUnit)
    {
        var from = NormalizeUnit(recipeUnit);
        var to = NormalizeUnit(stockUnit);

        if (from == to)
            return new UnitConversionResult(true, false, 1);

        return (from, to) switch
        {
            ("g", "kg") => new UnitConversionResult(true, true, 0.001m),
            ("kg", "g") => new UnitConversionResult(true, true, 1000m),
            ("ml", "litre") => new UnitConversionResult(true, true, 0.001m),
            ("litre", "ml") => new UnitConversionResult(true, true, 1000m),
            _ => new UnitConversionResult(false, false, 1)
        };
    }

    private static string NormalizeUnit(string? unit)
    {
        var normalized = (unit ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "gr" or "gram" or "gramaj" => "g",
            "g" => "g",
            "kg" or "kilogram" => "kg",
            "lt" or "l" or "liter" or "litre" => "litre",
            "ml" => "ml",
            "adet" => "adet",
            "çift" or "cift" => "çift",
            "metre" or "m" => "metre",
            _ => normalized
        };
    }

    private static MaterialPriceResult ResolveMaterialPrice(Material material, StockItem? stock, List<PurchaseOrderLine> purchasePrices)
    {
        if (material.LastPurchasePrice.HasValue)
            return new MaterialPriceResult(material.LastPurchasePrice.Value, string.IsNullOrWhiteSpace(material.Currency) ? stock?.Currency ?? "TRY" : material.Currency);

        if (stock?.LastPurchasePrice.HasValue == true)
            return new MaterialPriceResult(stock.LastPurchasePrice.Value, string.IsNullOrWhiteSpace(stock.Currency) ? material.Currency ?? "TRY" : stock.Currency);

        var purchaseLine = purchasePrices.FirstOrDefault(x => x.StockItem.MaterialId == material.Id);
        if (purchaseLine is not null)
            return new MaterialPriceResult(purchaseLine.UnitPrice, string.IsNullOrWhiteSpace(purchaseLine.PurchaseOrder.Currency) ? stock?.Currency ?? material.Currency ?? "TRY" : purchaseLine.PurchaseOrder.Currency);

        return new MaterialPriceResult(null, string.IsNullOrWhiteSpace(material.Currency) ? stock?.Currency ?? "TRY" : material.Currency);
    }

    private async Task AddAuditLog(string eventName, Guid workOrderId, object payload, CancellationToken cancellationToken)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserName = GetActor(),
            Action = AuditAction.Update,
            EntityName = eventName,
            EntityId = workOrderId.ToString(),
            NewValues = JsonSerializer.Serialize(payload),
            Timestamp = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await Task.CompletedTask;
    }

    private static decimal Round(decimal value) => Math.Round(value, 4);

    private static object ToListResponse(WorkOrder workOrder, WorkOrderProgress progress, MaterialPlanningSummary materialSummary)
    {
        return new
        {
            workOrder.Id,
            workOrder.WorkOrderNumber,
            OrderNumber = $"ORD-{workOrder.OrderItem.OrderId.ToString()[..8].ToUpper()}",
            workOrder.OrderItemId,
            CustomerId = workOrder.OrderItem.Order.CustomerId,
            CustomerName = workOrder.CustomerNameSnapshot ?? workOrder.OrderItem.Order.Customer.CompanyName ?? workOrder.OrderItem.Order.Customer.Name,
            workOrder.ProductId,
            ProductCode = workOrder.ProductCodeSnapshot ?? workOrder.Product.Code,
            ProductName = workOrder.ProductNameSnapshot ?? workOrder.Product.Name,
            workOrder.RecipeId,
            RecipeCode = workOrder.Recipe?.Code,
            RecipeName = workOrder.Recipe?.Name,
            workOrder.PlannedPairs,
            progress.AssignedPairs,
            progress.ProducedPairs,
            progress.GoodPairs,
            progress.FirePairs,
            RemainingPairs = Math.Max(workOrder.PlannedPairs - progress.ProducedPairs, 0),
            ProgressPercent = workOrder.PlannedPairs > 0 ? Math.Min(100, Math.Round((decimal)progress.ProducedPairs / workOrder.PlannedPairs * 100, 2)) : 0,
            workOrder.Priority,
            workOrder.Status,
            workOrder.PlannedStartDate,
            workOrder.PlannedEndDate,
            workOrder.ActualStartDate,
            workOrder.ActualEndDate,
            workOrder.AssignedMachineId,
            AssignedMachineCode = workOrder.AssignedMachine?.Code,
            workOrder.Shift,
            workOrder.IsActive,
            workOrder.IsCancelled,
            materialSummary.HasRecipe,
            materialSummary.HasMaterialShortage,
            materialSummary.ShortageMaterialCount,
            materialSummary.MaterialCoveragePercent,
            EstimatedMaterialCostByCurrency = materialSummary.EstimatedMaterialCostByCurrency,
            materialSummary.CanStartProduction,
            MaterialWarnings = materialSummary.MaterialWarnings,
            workOrder.CreatedAt,
            workOrder.UpdatedAt
        };
    }

    private static object ToDetailResponse(WorkOrder workOrder, WorkOrderProgress progress, MaterialPlanningSummary materialSummary, object? requirements, object assignments)
    {
        var list = ToListResponse(workOrder, progress, materialSummary);
        return new
        {
            WorkOrder = list,
            workOrder.Notes,
            workOrder.CancellationReason,
            Requirements = requirements,
            StationAssignments = assignments
        };
    }

    private static AvailableWorkOrderResponse ToAvailableResponse(WorkOrder workOrder, WorkOrderProgress progress, MaterialPlanningSummary materialSummary)
    {
        return new AvailableWorkOrderResponse(
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.CustomerNameSnapshot ?? workOrder.OrderItem.Order.Customer.CompanyName ?? workOrder.OrderItem.Order.Customer.Name,
            workOrder.ProductId,
            workOrder.ProductCodeSnapshot ?? workOrder.Product.Code,
            workOrder.ProductNameSnapshot ?? workOrder.Product.Name,
            workOrder.OrderItemId,
            workOrder.PlannedPairs,
            progress.AssignedPairs,
            progress.ProducedPairs,
            Math.Max(workOrder.PlannedPairs - progress.AssignedPairs, 0),
            workOrder.Priority,
            workOrder.PlannedStartDate,
            workOrder.PlannedEndDate,
            workOrder.Status,
            workOrder.RecipeId,
            workOrder.Recipe?.Code,
            workOrder.Recipe?.Name,
            materialSummary.HasRecipe,
            materialSummary.HasMaterialShortage,
            materialSummary.ShortageMaterialCount,
            materialSummary.CanStartProduction,
            materialSummary.MaterialCoveragePercent);
    }

    private static object ToProgressResponse(WorkOrder workOrder, WorkOrderProgress progress)
    {
        return new
        {
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.PlannedPairs,
            progress.AssignedPairs,
            progress.ProducedPairs,
            progress.GoodPairs,
            progress.FirePairs,
            RemainingPairs = Math.Max(workOrder.PlannedPairs - progress.ProducedPairs, 0),
            ProgressPercent = workOrder.PlannedPairs > 0 ? Math.Min(100, Math.Round((decimal)progress.ProducedPairs / workOrder.PlannedPairs * 100, 2)) : 0
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private string GetActor()
    {
        return User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name)
            ? User.Identity.Name
            : "system";
    }
}

public record SaveWorkOrderRequest(
    Guid OrderItemId,
    Guid? RecipeId,
    int PlannedPairs,
    string Priority,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    Guid? AssignedMachineId,
    int? Shift,
    string? Notes,
    bool IsActive = true
);

public record StartWorkOrderRequest(bool AllowMaterialShortage = false, string? ShortageReason = null, bool AllowStartWithoutReservation = false, string? ReservationOverrideReason = null);

public record CompleteWorkOrderRequest(bool AllowShortCompletion, string? Reason);

public record CancelWorkOrderRequest(string CancellationReason);

public record WorkOrderRequirementItemResponse(
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal RecipeQuantity,
    string RecipeUnit,
    decimal WastePercent,
    decimal NetRequiredQuantity,
    decimal WasteQuantity,
    decimal TotalRequiredQuantity,
    Guid? StockItemId,
    string? StockCode,
    decimal AvailableStock,
    decimal ReservedQuantity,
    decimal FreeStock,
    string StockUnit,
    bool ConversionApplied,
    decimal? ConversionFactor,
    decimal ShortageQuantity,
    decimal CoveragePercent,
    bool IsSufficient,
    decimal? MaterialUnitPrice,
    string Currency,
    decimal? EstimatedMaterialCost,
    string? Warning,
    bool IsUnitMismatch,
    decimal ReservedForThisWorkOrder,
    decimal ReservedForOtherWorkOrders,
    decimal TotalReserved,
    decimal FreeStockAfterReservations,
    decimal AvailableLotQuantity,
    decimal AvailableContainerQuantity,
    string ReservationStatus,
    bool IsFullyReserved,
    decimal RemainingToReserve,
    bool CanReserve
);

public record PurchaseSuggestionResponse(
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal ShortageQuantity,
    string Unit,
    Guid? PreferredSupplierId,
    string? PreferredSupplierName,
    decimal? LastPurchasePrice,
    string Currency
);

public record WorkOrderRequirementsResponse(
    Guid WorkOrderId,
    string WorkOrderNumber,
    Guid ProductId,
    string? ProductCode,
    string? ProductName,
    Guid RecipeId,
    string RecipeCode,
    string RecipeName,
    int PlannedPairs,
    decimal RecipeOutputQuantity,
    string RecipeOutputUnit,
    int MaterialCount,
    int SufficientMaterialCount,
    int ShortageMaterialCount,
    bool HasShortage,
    bool CanStartProduction,
    Dictionary<string, decimal> TotalsByCurrency,
    DateTime CalculatedAt,
    List<string> Warnings,
    List<WorkOrderRequirementItemResponse> Items,
    List<PurchaseSuggestionResponse> PurchaseSuggestions,
    MaterialPlanningSummary Summary,
    bool HasActiveReservation,
    int ReservationCount,
    int FullyReservedMaterialCount,
    int PartiallyReservedMaterialCount,
    int UnreservedMaterialCount,
    bool IsFullyReserved,
    bool CanStartWithReservation
);

public record MaterialPlanningSummary(
    bool HasRecipe,
    bool HasMaterialShortage,
    int ShortageMaterialCount,
    decimal MaterialCoveragePercent,
    Dictionary<string, decimal> EstimatedMaterialCostByCurrency,
    bool CanStartProduction,
    List<string> MaterialWarnings
)
{
    public static MaterialPlanningSummary NoRecipe => new(
        false,
        true,
        0,
        0,
        new Dictionary<string, decimal>(),
        false,
        new List<string> { "İş emrine bağlı aktif reçete bulunamadı." });
}

public record UnitConversionResult(bool IsSupported, bool Applied, decimal Factor);

public record MaterialPriceResult(decimal? UnitPrice, string Currency);

public record WorkOrderMaterialPlanningFields(
    bool HasRecipe,
    bool HasMaterialShortage,
    int ShortageMaterialCount,
    bool CanStartProduction,
    decimal MaterialCoveragePercent
);

public record AvailableWorkOrderResponse(
    Guid Id,
    string WorkOrderNumber,
    string? CustomerName,
    Guid ProductId,
    string? ProductCode,
    string? ProductName,
    Guid OrderItemId,
    int PlannedPairs,
    int AssignedPairs,
    int ProducedPairs,
    int RemainingToAssignPairs,
    string Priority,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    string Status,
    Guid? RecipeId,
    string? RecipeCode,
    string? RecipeName,
    bool HasRecipe,
    bool HasMaterialShortage,
    int ShortageMaterialCount,
    bool CanStartProduction,
    decimal MaterialCoveragePercent
);

public record WorkOrderProgress(int AssignedPairs, int ProducedPairs, int GoodPairs, int FirePairs)
{
    public static WorkOrderProgress Empty => new(0, 0, 0, 0);
}
