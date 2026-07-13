using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class Operator : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public string? EmployeeNumber { get; set; }

    public string Department { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTime? HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    public int Shift { get; set; } = 1;

    public Guid? DefaultMachineId { get; set; }

    public Machine? DefaultMachine { get; set; }

    public string? DefaultMachineCode { get; set; }

    public string? DefaultMachineName { get; set; }

    public bool CanUseInjectionMachine { get; set; }

    public bool CanUseGezerKafa { get; set; }

    public bool CanUseDonerKafa { get; set; }

    public bool CanUseDtfMachine { get; set; }

    public bool CanPerformQualityControl { get; set; }

    public bool CanPerformMaintenance { get; set; }

    public bool CanApproveWorkOrder { get; set; }

    public Guid? CurrentMachineId { get; set; }

    public Machine? CurrentMachine { get; set; }

    public string? CurrentMachineCode { get; set; }

    public string? CurrentMachineName { get; set; }

    public Guid? CurrentWorkOrderId { get; set; }

    public string? CurrentWorkOrderNumber { get; set; }

    public int? CurrentStationNumber { get; set; }

    public string CurrentStatus { get; set; } = "Available";

    public long TotalProducedPairs { get; set; }

    public decimal TotalWorkingHours { get; set; }

    public long TotalFirePairs { get; set; }

    public decimal? AverageFirePercent { get; set; }

    public decimal? PerformancePercent { get; set; }

    public decimal? QualityScore { get; set; }

    public DateTime? LastPerformanceUpdate { get; set; }

    public string? PhotoPath { get; set; }

    public string? QrCode { get; set; }

    public string? Barcode { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
