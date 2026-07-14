using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class QualityInspection : BaseAuditableEntity
{
    public string InspectionNumber { get; set; } = string.Empty;

    public string InspectionType { get; set; } = "InProcess";

    public Guid StationAssignmentId { get; set; }
    public StationAssignment StationAssignment { get; set; } = default!;

    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = default!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? MoldId { get; set; }
    public Mold? Mold { get; set; }

    public Guid? MachineId { get; set; }
    public Machine? Machine { get; set; }

    public Guid? OperatorId { get; set; }
    public Operator? Operator { get; set; }

    public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

    public int? Shift { get; set; }

    public string Status { get; set; } = "Draft";

    public string Result { get; set; } = "Pending";

    public int SampleSizePairs { get; set; }

    public int CheckedPairs { get; set; }

    public int AcceptedPairs { get; set; }

    public int RejectedPairs { get; set; }

    public int ConditionalAcceptedPairs { get; set; }

    public decimal? TargetWeightGrams { get; set; }

    public decimal? MeasuredWeightGrams { get; set; }

    public decimal? WeightToleranceMinus { get; set; }

    public decimal? WeightTolerancePlus { get; set; }

    public string WeightResult { get; set; } = "NotChecked";

    public decimal? TargetDensity { get; set; }

    public decimal? MeasuredDensity { get; set; }

    public decimal? DensityMinimum { get; set; }

    public decimal? DensityMaximum { get; set; }

    public string DensityResult { get; set; } = "NotChecked";

    public decimal? TargetX { get; set; }

    public decimal? MeasuredX { get; set; }

    public decimal? TargetY { get; set; }

    public decimal? MeasuredY { get; set; }

    public decimal? DimensionTolerance { get; set; }

    public string DimensionResult { get; set; } = "NotChecked";

    public string VisualResult { get; set; } = "NotChecked";

    public string ColorResult { get; set; } = "NotChecked";

    public string SurfaceResult { get; set; } = "NotChecked";

    public string FabricBondingResult { get; set; } = "NotChecked";

    public string? GeneralNotes { get; set; }

    public string? CorrectiveAction { get; set; }

    public bool HoldProduction { get; set; }

    public bool CreateFireRecord { get; set; }

    public string? FireReason { get; set; }

    public int? FirePairs { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsCancelled { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CompletedBy { get; set; }

    public string? CreatedByName { get; set; }

    public string? UpdatedByName { get; set; }

    public string? ProductCodeSnapshot { get; set; }

    public string? ProductNameSnapshot { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? WorkOrderNumberSnapshot { get; set; }

    public string? MoldCodeSnapshot { get; set; }

    public string? OperatorNameSnapshot { get; set; }

    public ICollection<QualityDefect> Defects { get; set; } = new List<QualityDefect>();
}
