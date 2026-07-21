using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class DatabaseIntegrityTests
{
    [Fact]
    public void Historical_records_cannot_be_cascade_deleted_with_master_data()
    {
        using var db = CreateDb();
        var relationships = new (Type Dependent, Type Principal, string ForeignKey)[]
        {
            (typeof(Order), typeof(Customer), nameof(Order.CustomerId)),
            (typeof(Order), typeof(Product), nameof(Order.ProductId)),
            (typeof(CuttingRecord), typeof(CuttingMachine), nameof(CuttingRecord.CuttingMachineId)),
            (typeof(CuttingRecord), typeof(Order), nameof(CuttingRecord.OrderId)),
            (typeof(ProductionRecord), typeof(InjectionStation), nameof(ProductionRecord.InjectionStationId)),
            (typeof(ProductionRecord), typeof(Mold), nameof(ProductionRecord.MoldId)),
            (typeof(ProductionRecord), typeof(Order), nameof(ProductionRecord.OrderId)),
            (typeof(PurchaseOrderLine), typeof(StockItem), nameof(PurchaseOrderLine.StockItemId))
        };

        foreach (var expected in relationships)
        {
            var entity = db.Model.FindEntityType(expected.Dependent)!;
            var foreignKey = entity.GetForeignKeys().Single(fk =>
                fk.PrincipalEntityType.ClrType == expected.Principal &&
                fk.Properties.Any(property => property.Name == expected.ForeignKey));
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }
    }

    [Fact]
    public void Production_box_traceability_code_is_required_and_unique()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(ProductionBox))!;
        var property = entity.FindProperty(nameof(ProductionBox.TraceabilityCode))!;
        var index = entity.GetIndexes().Single(x => x.Properties.Select(p => p.Name)
            .SequenceEqual([nameof(ProductionBox.TraceabilityCode)]));

        Assert.False(property.IsNullable);
        Assert.True(index.IsUnique);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new AuditableEntitySaveChangesInterceptor(new TestUser(), new TestClock()));
    }

    private sealed class TestClock : IDateTimeService { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class TestUser : ICurrentUserService
    {
        public Guid? UserId => null; public string? Email => "database-test@fixar.local"; public string? UserName => "Database Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => [];
    }
}
