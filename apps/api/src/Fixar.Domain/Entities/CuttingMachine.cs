using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class CuttingMachine : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string MachineType { get; set; } = string.Empty;

    public string OperatorName { get; set; } = string.Empty;

    public string Status { get; set; } = "Çalışıyor";

    public bool IsActive { get; set; } = true;

    public ICollection<CuttingRecord> CuttingRecords { get; set; } = new List<CuttingRecord>();
}
