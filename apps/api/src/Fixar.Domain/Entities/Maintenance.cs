using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class MaintenanceAsset : BaseAuditableEntity
{
    public string AssetCode { get; set; } = ""; public string AssetName { get; set; } = ""; public string AssetType { get; set; } = "OtherEquipment";
    public Guid? MachineId { get; set; } public Machine? Machine { get; set; } public Guid? InjectionStationId { get; set; } public InjectionStation? InjectionStation { get; set; }
    public Guid? CuttingMachineId { get; set; } public CuttingMachine? CuttingMachine { get; set; } public Guid? MoldId { get; set; } public Mold? Mold { get; set; }
    public string? Manufacturer { get; set; } public string? Model { get; set; } public string? SerialNumber { get; set; } public DateTime? CommissioningDate { get; set; }
    public string? Location { get; set; } public string Criticality { get; set; } = "Medium"; public Guid? ResponsibleUserId { get; set; }
    public string MaintenanceStrategy { get; set; } = "Mixed"; public bool IsActive { get; set; } = true; public string? Notes { get; set; }
}

public class MaintenanceRequest : BaseAuditableEntity
{
    public string RequestNumber { get; set; } = ""; public Guid MaintenanceAssetId { get; set; } public MaintenanceAsset MaintenanceAsset { get; set; } = default!;
    public string RequestType { get; set; } = "Breakdown"; public string Priority { get; set; } = "Normal"; public string Title { get; set; } = ""; public string Description { get; set; } = "";
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow; public Guid? ReportedByUserId { get; set; } public string? ReportedByName { get; set; }
    public string ProductionImpact { get; set; } = "None"; public bool MachineStopped { get; set; } public DateTime? DowntimeStartedAt { get; set; }
    public Guid? RelatedDowntimeId { get; set; } public ProductionDowntime? RelatedDowntime { get; set; } public string Status { get; set; } = "Open";
    public Guid? AssignedToUserId { get; set; } public string? AssignedToName { get; set; } public Guid? WorkOrderId { get; set; }
    public string? ResolutionSummary { get; set; } public DateTime? CancelledAt { get; set; } public Guid? CancelledBy { get; set; } public string? CancellationReason { get; set; }
}

public class MaintenanceWorkOrder : BaseAuditableEntity
{
    public string MaintenanceWorkOrderNumber { get; set; } = ""; public Guid MaintenanceAssetId { get; set; } public MaintenanceAsset MaintenanceAsset { get; set; } = default!;
    public Guid? MaintenanceRequestId { get; set; } public MaintenanceRequest? MaintenanceRequest { get; set; } public Guid? PreventiveMaintenancePlanId { get; set; }
    public string WorkType { get; set; } = "Corrective"; public string Priority { get; set; } = "Normal"; public string Title { get; set; } = ""; public string Description { get; set; } = "";
    public string Status { get; set; } = "Draft"; public DateTime PlannedStart { get; set; } = DateTime.UtcNow; public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; } public DateTime? ActualEnd { get; set; } public Guid? AssignedToUserId { get; set; } public string? AssignedToName { get; set; }
    public Guid? ExternalServiceSupplierId { get; set; } public Supplier? ExternalServiceSupplier { get; set; } public bool RequiresProductionStop { get; set; }
    public Guid? DowntimeId { get; set; } public ProductionDowntime? Downtime { get; set; } public decimal DowntimeMinutes { get; set; } public decimal LaborMinutes { get; set; }
    public string? FailureCause { get; set; } public string? WorkPerformed { get; set; } public string? Resolution { get; set; } public string? VerificationNotes { get; set; }
    public Guid? VerifiedByUserId { get; set; } public DateTime? VerifiedAt { get; set; } public decimal? TotalPartsCost { get; set; } public decimal? TotalLaborCost { get; set; }
    public decimal? TotalExternalServiceCost { get; set; } public decimal? TotalMaintenanceCost { get; set; } public string? Currency { get; set; }
    public bool IsClosed { get; set; } public DateTime? ClosedAt { get; set; } public Guid? ClosedBy { get; set; }
}

public class PreventiveMaintenancePlan : BaseAuditableEntity
{
    public string PlanCode { get; set; } = ""; public Guid MaintenanceAssetId { get; set; } public MaintenanceAsset MaintenanceAsset { get; set; } = default!;
    public string Name { get; set; } = ""; public string? Description { get; set; } public string FrequencyType { get; set; } = "Monthly"; public int FrequencyValue { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date; public DateTime? LastGeneratedDate { get; set; } public DateTime? LastCompletedDate { get; set; } public DateTime NextDueDate { get; set; }
    public int EstimatedDurationMinutes { get; set; } public bool RequiresProductionStop { get; set; } public string DefaultPriority { get; set; } = "Normal";
    public Guid? AssignedToUserId { get; set; } public Guid? ChecklistTemplateId { get; set; } public MaintenanceChecklistTemplate? ChecklistTemplate { get; set; }
    public bool IsActive { get; set; } = true; public bool AutoCreateWorkOrder { get; set; } = true; public int AdvanceCreateDays { get; set; } = 7; public string? Notes { get; set; }
}

public class MaintenanceChecklistTemplate : BaseAuditableEntity
{
    public string Name { get; set; } = ""; public string? AssetType { get; set; } public string? WorkType { get; set; } public string? Description { get; set; }
    public bool IsActive { get; set; } = true; public ICollection<MaintenanceChecklistTemplateItem> Items { get; set; } = new List<MaintenanceChecklistTemplateItem>();
}
public class MaintenanceChecklistTemplateItem : BaseAuditableEntity
{
    public Guid MaintenanceChecklistTemplateId { get; set; } public MaintenanceChecklistTemplate Template { get; set; } = default!; public int Sequence { get; set; }
    public string ItemText { get; set; } = ""; public string ItemType { get; set; } = "Checkbox"; public bool IsRequired { get; set; }
    public string? ExpectedValue { get; set; } public decimal? MinimumValue { get; set; } public decimal? MaximumValue { get; set; } public string? Unit { get; set; } public string? Instructions { get; set; }
}
public class MaintenanceChecklistResult : BaseAuditableEntity
{
    public Guid MaintenanceWorkOrderId { get; set; } public MaintenanceWorkOrder MaintenanceWorkOrder { get; set; } = default!;
    public Guid TemplateItemId { get; set; } public MaintenanceChecklistTemplateItem TemplateItem { get; set; } = default!; public bool IsCompleted { get; set; }
    public bool? PassFail { get; set; } public string? TextValue { get; set; } public decimal? NumericValue { get; set; } public string? Unit { get; set; } public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; } public Guid? CompletedByUserId { get; set; } public string? CompletedByName { get; set; }
}
public class MaintenancePartUsage : BaseAuditableEntity
{
    public Guid MaintenanceWorkOrderId { get; set; } public MaintenanceWorkOrder MaintenanceWorkOrder { get; set; } = default!; public Guid StockItemId { get; set; } public StockItem StockItem { get; set; } = default!;
    public Guid? MaterialId { get; set; } public Material? Material { get; set; } public decimal Quantity { get; set; } public string Unit { get; set; } = "Adet";
    public decimal? UnitCost { get; set; } public string? Currency { get; set; } public decimal? TotalCost { get; set; } public string Status { get; set; } = "Draft";
    public Guid? StockMovementId { get; set; } public DateTime? PostedAt { get; set; } public Guid? PostedBy { get; set; } public bool IsReversed { get; set; }
    public Guid? ReversalStockMovementId { get; set; } public DateTime? ReversedAt { get; set; } public Guid? ReversedBy { get; set; } public string? ReversalReason { get; set; } public string? Notes { get; set; }
}
