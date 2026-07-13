using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Machine : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string MachineType { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string? Manufacturer { get; set; }

    public string? SerialNumber { get; set; }

    public int? Year { get; set; }

    public int? StationCount { get; set; }

    public int? DefaultCycleTimeSeconds { get; set; }

    public int? MaximumDailyCapacity { get; set; }

    public decimal? WorkingHoursPerDay { get; set; }

    public decimal? EnergyConsumption { get; set; }

    public string? Location { get; set; }

    public string CurrentStatus { get; set; } = "Idle";

    public Guid? CurrentWorkOrderId { get; set; }

    public string? CurrentOperatorName { get; set; }

    public DateTime? LastMaintenanceDate { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public DateTime? LastCleaningDate { get; set; }

    public DateTime? NextCleaningDate { get; set; }

    public DateTime? LastCalibrationDate { get; set; }

    public DateTime? NextCalibrationDate { get; set; }

    public decimal TotalRunningHours { get; set; }

    public long TotalProducedPairs { get; set; }

    public decimal? AvailabilityPercent { get; set; }

    public decimal? PerformancePercent { get; set; }

    public decimal? QualityPercent { get; set; }

    public decimal? OEE { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
