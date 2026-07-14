using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class QualityDefect : BaseAuditableEntity
{
    public Guid QualityInspectionId { get; set; }

    public QualityInspection QualityInspection { get; set; } = default!;

    public string DefectType { get; set; } = string.Empty;

    public string? DefectCode { get; set; }

    public string? Description { get; set; }

    public int DefectPairs { get; set; }

    public string Severity { get; set; } = "Minor";

    public bool IsFireRelated { get; set; }

    public Guid? StationAssignmentFireId { get; set; }

    public StationAssignmentFire? StationAssignmentFire { get; set; }

    public string? CorrectiveAction { get; set; }

    public int Sequence { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
