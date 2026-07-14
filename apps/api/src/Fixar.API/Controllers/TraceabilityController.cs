using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), AllowAnonymous]
[Route("api/v{version:apiVersion}/traceability")]
public class TraceabilityController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("{traceabilityCode}")]
    public Task<IActionResult> ByCode(string traceabilityCode, CancellationToken ct) => Detail(x => x.TraceabilityCode == traceabilityCode, ct);

    [HttpGet("by-barcode/{barcode}")]
    public Task<IActionResult> ByBarcode(string barcode, CancellationToken ct) => Detail(x => x.Barcode == barcode, ct);

    [HttpGet("by-box-number/{boxNumber}")]
    public Task<IActionResult> ByBoxNumber(string boxNumber, CancellationToken ct) => Detail(x => x.BoxNumber == boxNumber || x.BoxCode == boxNumber, ct);

    [HttpGet("search")]
    public async Task<IActionResult> Search(string? search, Guid? customerId, Guid? orderId, Guid? workOrderId, Guid? productId, Guid? stationAssignmentId, Guid? cuttingRecordId, string? status, string? shipmentReference, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var q = db.ProductionBoxes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(x => (x.TraceabilityCode != null && x.TraceabilityCode.ToLower().Contains(s)) || (x.Barcode != null && x.Barcode.ToLower().Contains(s)) || (x.BoxNumber != null && x.BoxNumber.ToLower().Contains(s)) || (x.WorkOrderNumberSnapshot != null && x.WorkOrderNumberSnapshot.ToLower().Contains(s)) || (x.ShipmentReference != null && x.ShipmentReference.ToLower().Contains(s)) || (x.CustomerNameSnapshot != null && x.CustomerNameSnapshot.ToLower().Contains(s)) || (x.ProductNameSnapshot != null && x.ProductNameSnapshot.ToLower().Contains(s)) || (x.Order != null && x.Order.OrderNumber.ToLower().Contains(s))); }
        if (customerId.HasValue) q = q.Where(x => x.CustomerId == customerId); if (orderId.HasValue) q = q.Where(x => x.OrderId == orderId); if (workOrderId.HasValue) q = q.Where(x => x.WorkOrderId == workOrderId); if (productId.HasValue) q = q.Where(x => x.ProductId == productId); if (stationAssignmentId.HasValue) q = q.Where(x => x.StationAssignmentId == stationAssignmentId); if (cuttingRecordId.HasValue) q = q.Where(x => x.CuttingRecordId == cuttingRecordId); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status); if (!string.IsNullOrWhiteSpace(shipmentReference)) q = q.Where(x => x.ShipmentReference == shipmentReference); if (dateFrom.HasValue) q = q.Where(x => x.CreatedAt >= dateFrom); if (dateTo.HasValue) q = q.Where(x => x.CreatedAt <= dateTo);
        var rows = await q.OrderByDescending(x => x.UpdatedAt).Select(x => new TraceabilityListDto(x.TraceabilityCode!, x.Barcode ?? x.BoxCode, x.BoxNumber ?? x.BoxCode, x.CustomerNameSnapshot ?? x.CustomerName ?? (x.Customer != null ? x.Customer.CompanyName ?? x.Customer.Name : null), x.Order != null ? x.Order.OrderNumber : null, x.WorkOrderNumberSnapshot ?? (x.WorkOrder != null ? x.WorkOrder.WorkOrderNumber : null), x.ProductCodeSnapshot ?? (x.Product != null ? x.Product.Code : null), x.ProductNameSnapshot ?? (x.Product != null ? x.Product.Name : null), x.PairCount > 0 ? x.PairCount : x.QuantityPairs, x.Status, x.WarehouseLocation, x.RackCode, x.ShipmentReference, x.UpdatedAt, x.IsCancelled)).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpGet("{traceabilityCode}/timeline")]
    public async Task<IActionResult> Timeline(string traceabilityCode, CancellationToken ct, string order = "asc")
    {
        var box = await db.ProductionBoxes.AsNoTracking().Where(x => x.TraceabilityCode == traceabilityCode).Select(x => new { x.Id, x.OrderId, x.WorkOrderId, x.StationAssignmentId, x.OrderItemId, x.CuttingRecordId, x.CreatedAt, x.PackedAt, x.ReceivedToWarehouseAt, x.ReadyForShipmentAt, x.ShippedAt, x.ShipmentReference, x.IsCancelled, x.CancelledAt }).FirstOrDefaultAsync(ct);
        if (box is null) return NotFound(ApiResponse<object>.Fail("İzlenebilirlik kaydı bulunamadı.", "TRACEABILITY_NOT_FOUND"));
        var events = new List<TraceEventDto>();
        void Add(DateTime? date, string module, string type, string title, string? description = null, string? previous = null, string? next = null, string? user = null) { if (date.HasValue) events.Add(new(type, date.Value, module, title, description, previous, next, user)); }
        if (box.OrderId.HasValue) { var o = await db.Orders.AsNoTracking().Where(x => x.Id == box.OrderId).Select(x => new { x.Created, x.OrderNumber, x.Status, x.LastModified }).FirstOrDefaultAsync(ct); if (o != null) { Add(o.Created, "Order", "Created", "Sipariş oluşturuldu", o.OrderNumber); if (o.Status != "Draft" && o.Status != "Aktif") Add(o.LastModified, "Order", o.Status, "Sipariş durumu güncellendi", o.OrderNumber, null, o.Status); } }
        if (box.WorkOrderId.HasValue) { var w = await db.WorkOrders.AsNoTracking().Where(x => x.Id == box.WorkOrderId).Select(x => new { x.CreatedAt, x.WorkOrderNumber, x.PlannedStartDate, x.ActualStartDate, x.ActualEndDate, x.Status, x.UpdatedBy }).FirstOrDefaultAsync(ct); if (w != null) { Add(w.CreatedAt, "WorkOrder", "Created", "İş emri oluşturuldu", w.WorkOrderNumber); Add(w.ActualStartDate ?? w.PlannedStartDate, "WorkOrder", "Started", "İş emri üretime başladı", w.WorkOrderNumber); Add(w.ActualEndDate, "WorkOrder", "Completed", "İş emri tamamlandı", w.WorkOrderNumber, null, w.Status, w.UpdatedBy); } }
        if (box.StationAssignmentId.HasValue) { var stationEvents = await db.StationAssignmentEvents.AsNoTracking().Where(x => x.StationAssignmentId == box.StationAssignmentId).Select(x => new TraceEventDto(x.EventType, x.EventTime, "Production", "Üretim olayı", x.Note ?? x.Reason, null, null, x.RecordedBy)).ToListAsync(ct); events.AddRange(stationEvents); var fires = await db.StationAssignmentFires.AsNoTracking().Where(x => x.StationAssignmentId == box.StationAssignmentId && !x.IsCancelled).Select(x => new TraceEventDto("Fire", x.RecordedAt, "Production", "Fire kaydı", x.FirePairs + " çift - " + (x.Reason ?? x.ReasonType), null, null, x.RecordedBy)).ToListAsync(ct); events.AddRange(fires); var downs = await db.StationAssignmentDowntimes.AsNoTracking().Where(x => x.StationAssignmentId == box.StationAssignmentId && !x.IsCancelled).Select(x => new TraceEventDto("Downtime", x.StartedAt, "Production", "Duruş başladı", x.Reason ?? x.DowntimeType, x.PreviousAssignmentStatus, "Beklemede", x.StartedBy)).ToListAsync(ct); events.AddRange(downs); }
        if (box.OrderItemId.HasValue) { events.AddRange(await db.QualityInspections.AsNoTracking().Where(x => x.OrderItemId == box.OrderItemId && x.IsActive).Select(x => new TraceEventDto("QualityInspection", x.InspectionDate, "Quality", "Kalite kontrolü", x.InspectionNumber + " - " + x.Result, null, x.Result, x.CompletedBy ?? x.CreatedByName)).ToListAsync(ct)); }
        if (box.CuttingRecordId.HasValue) { var c = await db.CuttingRecords.AsNoTracking().Where(x => x.Id == box.CuttingRecordId).Select(x => new { x.RecordDate, x.EndTime, x.RecordNumber, x.Status, x.CreatedByName }).FirstOrDefaultAsync(ct); if (c != null) { Add(c.RecordDate, "Cutting", "Created", "Kesim kaydı oluşturuldu", c.RecordNumber, null, c.Status, c.CreatedByName); Add(c.EndTime, "Cutting", "Completed", "Kesim tamamlandı", c.RecordNumber); } }
        events.AddRange(await db.ProductionBoxEvents.AsNoTracking().Where(x => x.ProductionBoxId == box.Id).Select(x => new TraceEventDto(x.EventType, x.EventTime, "ProductionBox", "Koli olayı", x.Note, x.FromLocation, x.ToLocation, x.OperatorName)).ToListAsync(ct));
        Add(box.CreatedAt, "ProductionBox", "Created", "Koli oluşturuldu"); Add(box.PackedAt, "ProductionBox", "Packed", "Koli paketlendi"); Add(box.ReceivedToWarehouseAt, "Warehouse", "Received", "Depoya kabul edildi"); Add(box.ReadyForShipmentAt, "Warehouse", "ReadyForShipment", "Sevkiyata hazırlandı"); Add(box.ShippedAt, "Shipment", "Shipped", "Sevk edildi", box.ShipmentReference); Add(box.CancelledAt, "ProductionBox", "Cancelled", "Koli iptal edildi");
        var distinct = events.GroupBy(x => new { x.EventDate, x.SourceModule, x.EventType, x.Title }).Select(x => x.First()); var result = order.Equals("desc", StringComparison.OrdinalIgnoreCase) ? distinct.OrderByDescending(x => x.EventDate) : distinct.OrderBy(x => x.EventDate);
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpGet("{traceabilityCode}/label-data")]
    public async Task<IActionResult> Label(string traceabilityCode, CancellationToken ct)
    {
        var x = await db.ProductionBoxes.AsNoTracking().Where(x => x.TraceabilityCode == traceabilityCode).Select(x => new LabelDataDto("FIXAR", x.BoxNumber ?? x.BoxCode, x.Barcode ?? x.BoxCode, x.TraceabilityCode!, x.CustomerNameSnapshot ?? x.CustomerName ?? (x.Customer != null ? x.Customer.CompanyName ?? x.Customer.Name : null), x.Order != null ? x.Order.OrderNumber : null, x.WorkOrderNumberSnapshot ?? (x.WorkOrder != null ? x.WorkOrder.WorkOrderNumber : null), x.ProductCodeSnapshot ?? (x.Product != null ? x.Product.Code : null), x.ProductNameSnapshot ?? (x.Product != null ? x.Product.Name : null), x.OrderItem != null ? x.OrderItem.SizeRange : null, x.OrderItem != null ? x.OrderItem.Color ?? x.OrderItem.FabricColor : x.FabricColor, x.PairCount > 0 ? x.PairCount : x.QuantityPairs, x.StationAssignment != null ? x.StationAssignment.StartedAt : x.CreatedAt, x.PackedAt ?? x.FilledAt, "/traceability/" + x.TraceabilityCode)).FirstOrDefaultAsync(ct);
        return x is null ? NotFound(ApiResponse<object>.Fail("İzlenebilirlik kaydı bulunamadı.", "TRACEABILITY_NOT_FOUND")) : Ok(ApiResponse<object>.SuccessResponse(x));
    }

    private async Task<IActionResult> Detail(System.Linq.Expressions.Expression<Func<Fixar.Domain.Entities.ProductionBox, bool>> predicate, CancellationToken ct)
    {
        var x = await db.ProductionBoxes.AsNoTracking().Where(predicate).Select(b => new TraceabilityDetailDto(b.TraceabilityCode!, b.Barcode ?? b.BoxCode, b.BoxNumber ?? b.BoxCode, b.Status, b.IsCancelled, b.PairCount > 0 ? b.PairCount : b.QuantityPairs, b.CreatedAt, b.PackedAt ?? b.FilledAt, b.ReceivedToWarehouseAt, b.ReadyForShipmentAt, b.ShippedAt, b.ShipmentReference,
            new CustomerTraceDto(b.CustomerId, b.Customer != null ? b.Customer.CustomerCode : null, b.CustomerNameSnapshot ?? b.CustomerName ?? (b.Customer != null ? b.Customer.CompanyName ?? b.Customer.Name : null), b.Customer != null ? b.Customer.ContactName : null, b.Customer != null ? b.Customer.Country : null),
            b.Order == null ? null : new OrderTraceDto(b.Order.Id, b.Order.OrderNumber, b.Order.OrderDate, b.Order.CustomerReference, b.Order.DueDate, b.Order.Currency, b.Order.ExpectedPaymentMethod, b.Order.Status),
            b.OrderItem == null ? null : new OrderItemTraceDto(b.OrderItem.Id, b.OrderItem.ProductId, b.ProductCodeSnapshot ?? (b.Product != null ? b.Product.Code : null), b.ProductNameSnapshot ?? (b.Product != null ? b.Product.Name : null), b.OrderItem.SizeRange, b.OrderItem.Color, b.OrderItem.ProductionType, b.OrderItem.FabricColor, b.OrderItem.QuantityPairs, b.OrderItem.ProducedPairs, b.OrderItem.CutPairs, b.OrderItem.ShippedPairs),
            b.WorkOrder == null ? null : new WorkOrderTraceDto(b.WorkOrder.Id, b.WorkOrder.WorkOrderNumber, b.WorkOrder.PlannedPairs, b.WorkOrder.Priority, b.WorkOrder.Status, b.WorkOrder.PlannedStartDate, b.WorkOrder.ActualStartDate, b.WorkOrder.ActualEndDate, b.WorkOrder.RecipeId, b.WorkOrder.Recipe != null ? b.WorkOrder.Recipe.Code : null, b.WorkOrder.Recipe != null ? b.WorkOrder.Recipe.Name : null),
            b.StationAssignment == null ? null : new ProductionTraceDto(b.StationAssignment.Id, b.StationAssignment.StationNumberSnapshot, b.Mold != null ? b.Mold.Code : null, b.Mold != null ? b.Mold.Name : null, b.StationAssignment.OperatorName, b.StationAssignment.StartedAt, b.StationAssignment.FinishedAt, b.StationAssignment.ProducedPairs, b.StationAssignment.ProducedPairs - b.StationAssignment.FirePairs, b.StationAssignment.FirePairs, b.StationAssignment.TotalTurns, b.StationAssignment.LastTurnAt),
            b.CuttingRecord == null ? null : new CuttingTraceDto(b.CuttingRecord.Id, b.CuttingRecord.RecordNumber, b.CuttingRecord.RecordDate, b.CuttingRecord.CuttingMachine.Name, b.CuttingRecord.Operator != null ? b.CuttingRecord.Operator.FullName : null, b.CuttingRecord.InputPairs, b.CuttingRecord.GoodPairs, b.CuttingRecord.RejectedPairs, b.CuttingRecord.ReworkPairs, b.CuttingRecord.Status),
            new WarehouseTraceDto(b.WarehouseLocation, b.RackCode, b.ReceivedToWarehouseAt, b.ReadyForShipmentAt), new ShipmentTraceDto(b.ShippedAt, b.ShipmentReference, b.ShipmentNotes))).FirstOrDefaultAsync(ct);
        if (x is null) return NotFound(ApiResponse<object>.Fail("İzlenebilirlik kaydı bulunamadı.", "TRACEABILITY_NOT_FOUND"));
        var inspections = x.OrderItem?.OrderItemId is Guid itemId ? await db.QualityInspections.AsNoTracking().Where(q => q.OrderItemId == itemId && q.IsActive).OrderByDescending(q => q.InspectionDate).Select(q => new QualityInspectionTraceDto(q.InspectionNumber, q.InspectionDate, q.Result, q.CheckedPairs, q.RejectedPairs, q.FirePairs ?? 0)).ToListAsync(ct) : [];
        return Ok(ApiResponse<object>.SuccessResponse(new { Detail = x, Quality = new { InspectionCount = inspections.Count, Latest = inspections.FirstOrDefault(), TotalCheckedPairs = inspections.Sum(i => i.CheckedPairs), TotalRejectedPairs = inspections.Sum(i => i.RejectedPairs), LinkedFirePairs = inspections.Sum(i => i.LinkedFirePairs), Inspections = inspections } }));
    }
}

public record TraceabilityListDto(string TraceabilityCode, string Barcode, string BoxNumber, string? CustomerName, string? OrderNumber, string? WorkOrderNumber, string? ProductCode, string? ProductName, int PairCount, string Status, string? WarehouseLocation, string? RackCode, string? ShipmentReference, DateTime LastMovementAt, bool IsCancelled);
public record TraceabilityDetailDto(string TraceabilityCode, string Barcode, string BoxNumber, string CurrentStatus, bool IsCancelled, int PairCount, DateTime CreatedAt, DateTime? PackedAt, DateTime? ReceivedToWarehouseAt, DateTime? ReadyForShipmentAt, DateTime? ShippedAt, string? ShipmentReference, CustomerTraceDto Customer, OrderTraceDto? Order, OrderItemTraceDto? OrderItem, WorkOrderTraceDto? WorkOrder, ProductionTraceDto? Production, CuttingTraceDto? Cutting, WarehouseTraceDto Warehouse, ShipmentTraceDto Shipment);
public record CustomerTraceDto(Guid? CustomerId, string? CustomerCode, string? CustomerName, string? ContactName, string? Country);
public record OrderTraceDto(Guid OrderId, string OrderNumber, DateTime OrderDate, string? CustomerReference, DateTime? DueDate, string Currency, string ExpectedPaymentMethod, string Status);
public record OrderItemTraceDto(Guid OrderItemId, Guid? ProductId, string? ProductCode, string? ProductName, string? SizeRange, string? Color, string? ProductionType, string? FabricColor, int QuantityPairs, int ProducedPairs, int CutPairs, int ShippedPairs);
public record WorkOrderTraceDto(Guid WorkOrderId, string WorkOrderNumber, int PlannedPairs, string Priority, string Status, DateTime? PlannedStartDate, DateTime? ActualStartDate, DateTime? ActualEndDate, Guid? RecipeId, string? RecipeCode, string? RecipeName);
public record ProductionTraceDto(Guid StationAssignmentId, int StationNumber, string? MoldCode, string? MoldName, string? OperatorName, DateTime StartedAt, DateTime? CompletedAt, int ProducedPairs, int GoodPairs, int FirePairs, int TotalTurns, DateTime? LastTurnAt);
public record CuttingTraceDto(Guid CuttingRecordId, string? RecordNumber, DateTime RecordDate, string CuttingMachineName, string? OperatorName, int InputPairs, int GoodPairs, int RejectedPairs, int ReworkPairs, string Status);
public record WarehouseTraceDto(string? WarehouseLocation, string? RackCode, DateTime? ReceivedAt, DateTime? ReadyForShipmentAt);
public record ShipmentTraceDto(DateTime? ShippedAt, string? ShipmentReference, string? ShipmentNotes);
public record QualityInspectionTraceDto(string InspectionNumber, DateTime InspectionDate, string Result, int CheckedPairs, int RejectedPairs, int LinkedFirePairs);
public record TraceEventDto(string EventType, DateTime EventDate, string SourceModule, string Title, string? Description, string? PreviousStatus, string? NewStatus, string? User);
public record LabelDataDto(string CompanyName, string BoxNumber, string Barcode, string TraceabilityCode, string? CustomerName, string? OrderNumber, string? WorkOrderNumber, string? ProductCode, string? ProductName, string? SizeRange, string? Color, int PairCount, DateTime ProductionDate, DateTime? PackedAt, string QrPayload);
