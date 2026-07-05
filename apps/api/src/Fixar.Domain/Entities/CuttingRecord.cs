using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class CuttingRecord : BaseAuditableEntity
{
    public Guid CuttingMachineId { get; set; }
    public CuttingMachine CuttingMachine { get; set; } = default!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int CutPairs { get; set; }

    public string Status { get; set; } = "Kesimde";
}
