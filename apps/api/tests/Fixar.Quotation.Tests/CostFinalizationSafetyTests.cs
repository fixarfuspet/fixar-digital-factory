using Fixar.API.Controllers;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class CostFinalizationSafetyTests
{
    [Fact]
    public async Task Work_order_cannot_have_two_final_cost_snapshots()
    {
        await using var db = CreateDb();
        var workOrderId = Guid.NewGuid();
        var first = new WorkOrderCostSnapshot { Id = Guid.NewGuid(), WorkOrderId = workOrderId, SnapshotNumber = "COST-1", SnapshotDate = DateTime.UtcNow, IsFinal = true, CalculationType = "Final" };
        var second = new WorkOrderCostSnapshot { Id = Guid.NewGuid(), WorkOrderId = workOrderId, SnapshotNumber = "COST-2", SnapshotDate = DateTime.UtcNow, IsFinal = false };
        db.AddRange(first, second);
        await db.SaveChangesAsync();

        var result = await new WorkOrderCostsController(db, null!).Finalize(second.Id, default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.False(second.IsFinal);
        Assert.DoesNotContain(db.AuditLogs, x => x.EntityName == "Work Order Cost Finalized");
    }

    [Fact]
    public async Task Finalizing_cost_snapshot_creates_audit_log()
    {
        await using var db = CreateDb();
        var snapshot = new WorkOrderCostSnapshot { Id = Guid.NewGuid(), WorkOrderId = Guid.NewGuid(), SnapshotNumber = "COST-1", SnapshotDate = DateTime.UtcNow };
        db.Add(snapshot);
        await db.SaveChangesAsync();

        var result = await new WorkOrderCostsController(db, null!).Finalize(snapshot.Id, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(snapshot.IsFinal);
        Assert.Equal("Final", snapshot.CalculationType);
        Assert.Contains(db.AuditLogs, x => x.EntityName == "Work Order Cost Finalized");
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options, new AuditableEntitySaveChangesInterceptor(new TestUser(), new TestClock()));
    }
    private sealed class TestClock : IDateTimeService { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class TestUser : ICurrentUserService
    {
        public Guid? UserId => null; public string? Email => "cost-test@fixar.local"; public string? UserName => "Cost Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
