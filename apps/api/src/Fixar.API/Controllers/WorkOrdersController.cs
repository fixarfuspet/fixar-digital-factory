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
        return Ok(ApiResponse<object>.SuccessResponse(workOrders.Select(x => ToListResponse(x, GetProgress(progress, x.Id))).ToList()));
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
        var response = workOrders
            .Select(x => ToAvailableResponse(x, GetProgress(progress, x.Id)))
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

        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(workOrder, GetProgress(progress, id), requirements, assignments)));
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
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created, WorkOrderProgress.Empty, null, Array.Empty<object>()), "İş emri oluşturuldu."));
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
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(updated, updatedProgress, null, Array.Empty<object>()), "İş emri güncellendi."));
    }

    [HttpPost("{id:guid}/plan")]
    public Task<IActionResult> Plan(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "Planned", cancellationToken);

    [HttpPost("{id:guid}/mark-ready")]
    public Task<IActionResult> MarkReady(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "Ready", cancellationToken);

    [HttpPost("{id:guid}/start")]
    public Task<IActionResult> Start(Guid id, CancellationToken cancellationToken) => ChangeStatus(id, "InProduction", cancellationToken);

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

        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, progress), "İş emri tamamlandı."));
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

        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, WorkOrderProgress.Empty), "İş emri iptal edildi."));
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
        return Ok(ApiResponse<object>.SuccessResponse(ToDetailResponse(created, WorkOrderProgress.Empty, null, Array.Empty<object>()), "İş emri kopyalandı."));
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
        return Ok(ApiResponse<object>.SuccessResponse(ToListResponse(workOrder, progress), "İş emri durumu güncellendi."));
    }

    private static WorkOrderProgress GetProgress(IReadOnlyDictionary<Guid, WorkOrderProgress> progress, Guid id)
        => progress.TryGetValue(id, out var value) ? value : WorkOrderProgress.Empty;

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

    private async Task<object?> CalculateRequirements(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        if (!workOrder.RecipeId.HasValue)
            return null;

        var recipe = await _db.Recipes
            .Include(x => x.Items)
            .ThenInclude(x => x.Material)
            .FirstOrDefaultAsync(x => x.Id == workOrder.RecipeId.Value, cancellationToken);

        if (recipe is null)
            return null;

        var materialIds = recipe.Items.Select(x => x.MaterialId).ToList();
        var stocks = await _db.StockItems
            .Where(x => x.MaterialId.HasValue && materialIds.Contains(x.MaterialId.Value))
            .GroupBy(x => x.MaterialId!.Value)
            .Select(x => new { MaterialId = x.Key, Quantity = x.Sum(y => y.CurrentQuantity) })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, cancellationToken);

        var items = recipe.Items
            .OrderBy(x => x.Sequence)
            .Select(item =>
            {
                var totalQuantity = item.Quantity + item.Quantity * item.WastePercent / 100;
                var requiredQuantity = recipe.OutputQuantity > 0
                    ? totalQuantity * workOrder.PlannedPairs / recipe.OutputQuantity
                    : 0;
                stocks.TryGetValue(item.MaterialId, out var availableStock);
                var unitPrice = item.Material.LastPurchasePrice ?? 0;
                var currency = string.IsNullOrWhiteSpace(item.Material.Currency) ? "TRY" : item.Material.Currency;
                return new RequirementItemResponse(
                    item.MaterialId,
                    item.Material.Code,
                    item.Material.Name,
                    Math.Round(requiredQuantity, 4),
                    item.Unit,
                    availableStock,
                    Math.Max(requiredQuantity - availableStock, 0),
                    currency,
                    Math.Round(requiredQuantity * unitPrice, 4));
            })
            .ToList();

        var totalsByCurrency = items
            .GroupBy(x => x.Currency)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.EstimatedMaterialCost));

        return new
        {
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.RecipeId,
            recipe.Code,
            recipe.Name,
            workOrder.PlannedPairs,
            Items = items,
            TotalsByCurrency = totalsByCurrency
        };
    }

    private static object ToListResponse(WorkOrder workOrder, WorkOrderProgress progress)
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
            workOrder.CreatedAt,
            workOrder.UpdatedAt
        };
    }

    private static object ToDetailResponse(WorkOrder workOrder, WorkOrderProgress progress, object? requirements, object assignments)
    {
        var list = ToListResponse(workOrder, progress);
        return new
        {
            WorkOrder = list,
            workOrder.Notes,
            workOrder.CancellationReason,
            Requirements = requirements,
            StationAssignments = assignments
        };
    }

    private static AvailableWorkOrderResponse ToAvailableResponse(WorkOrder workOrder, WorkOrderProgress progress)
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
            workOrder.Recipe?.Name);
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

public record CompleteWorkOrderRequest(bool AllowShortCompletion, string? Reason);

public record CancelWorkOrderRequest(string CancellationReason);

public record RequirementItemResponse(
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal RequiredQuantity,
    string Unit,
    decimal AvailableStock,
    decimal ShortageQuantity,
    string Currency,
    decimal EstimatedMaterialCost
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
    string? RecipeName
);

public record WorkOrderProgress(int AssignedPairs, int ProducedPairs, int GoodPairs, int FirePairs)
{
    public static WorkOrderProgress Empty => new(0, 0, 0, 0);
}
