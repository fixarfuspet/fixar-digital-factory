using Fixar.Application.Features.Costing;
using Fixar.Domain.Entities;

namespace Fixar.Application.Common.Interfaces;

public interface IWorkOrderCostService
{
    Task<CostPreviewDto> CalculateEstimatedCostAsync(Guid workOrderId, string? reportingCurrency, DateTime? at, CancellationToken ct);
    Task<CostPreviewDto> CalculateActualCostAsync(Guid workOrderId, string? reportingCurrency, DateTime? at, CancellationToken ct);
    Task<WorkOrderCostSnapshot> CreateSnapshotAsync(Guid workOrderId, CreateCostSnapshotRequest request, CancellationToken ct);
    Task<WorkOrderCostSnapshot?> GetLatestSnapshotAsync(Guid workOrderId, CancellationToken ct);
    Task<WorkOrderCostDetailDto?> GetCostBreakdownAsync(Guid snapshotId, CancellationToken ct);
    Task<IReadOnlyList<string>> ValidateCostInputsAsync(Guid workOrderId, string? reportingCurrency, DateTime? at, CancellationToken ct);
}
