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

public sealed class QualityWorkflowSafetyTests
{
    [Fact]
    public async Task Checked_quantity_cannot_exceed_sample_size()
    {
        await using var db = CreateDb();
        var result = await new QualityInspectionsController(db).Create(Request(Guid.NewGuid(), 5, 6), default);
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.QualityInspections);
    }

    [Fact]
    public async Task Checked_quantity_cannot_exceed_station_production()
    {
        await using var db = CreateDb();
        var assignment = await SeedAssignment(db, produced: 4);
        var result = await new QualityInspectionsController(db).Create(Request(assignment.Id, 5, 5), default);
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(db.QualityInspections);
    }

    [Fact]
    public async Task Inspection_with_active_linked_fire_cannot_be_cancelled_silently()
    {
        await using var db = CreateDb();
        var assignment = await SeedAssignment(db, produced: 10);
        var inspection = new QualityInspection
        {
            Id = Guid.NewGuid(), InspectionNumber = "TEST-QI-001", InspectionType = "Final",
            StationAssignment = assignment, StationAssignmentId = assignment.Id, OrderItem = assignment.OrderItem,
            OrderItemId = assignment.OrderItemId, SampleSizePairs = 2, CheckedPairs = 2, RejectedPairs = 1,
            AcceptedPairs = 1, Status = "Completed", Result = "Failed"
        };
        var fire = new StationAssignmentFire
        {
            Id = Guid.NewGuid(), StationAssignment = assignment, StationAssignmentId = assignment.Id,
            OrderItemId = assignment.OrderItemId, InjectionStationId = assignment.InjectionStationId,
            StationNumberSnapshot = assignment.StationNumberSnapshot, FirePairs = 1, ReasonType = "Diğer",
            Note = "Kalite kontrol bağlantısı: TEST-QI-001"
        };
        db.AddRange(inspection, fire);
        await db.SaveChangesAsync();

        var result = await new QualityInspectionsController(db)
            .Cancel(inspection.Id, new CancelQualityInspectionRequest("Hatalı kontrol"), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.False(inspection.IsCancelled);
        Assert.False(fire.IsCancelled);
    }

    private static UpsertQualityInspectionRequest Request(Guid assignmentId, int sample, int checkedPairs) => new(
        "Final", assignmentId, sample, checkedPairs, checkedPairs, 0, 0, null, null,
        null, null, null, null, null, null, null, null, null, null, null, null, null,
        "Passed", "Passed", "Passed", "Passed", null, null, false, false, null, 0, []);

    private static async Task<StationAssignment> SeedAssignment(ApplicationDbContext db, int produced)
    {
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = "TEST-C", Name = "TEST Müşteri" };
        var product = new Product { Id = Guid.NewGuid(), Code = "TEST-P", Name = "TEST Ürün" };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "TEST-SO", Customer = customer, CustomerId = customer.Id, Product = product, ProductId = product.Id, Quantity = produced };
        var item = new OrderItem { Id = Guid.NewGuid(), Order = order, OrderId = order.Id, Product = product, ProductId = product.Id, QuantityPairs = produced, ProducedPairs = produced, LineNumber = 1 };
        order.Items.Add(item);
        var station = new InjectionStation { Id = Guid.NewGuid(), StationNumber = 1, Name = "TEST İstasyon" };
        var assignment = new StationAssignment { Id = Guid.NewGuid(), InjectionStation = station, InjectionStationId = station.Id, OrderItem = item, OrderItemId = item.Id, StationNumberSnapshot = 1, PlannedPairs = produced, ProducedPairs = produced };
        db.AddRange(customer, product, order, station, assignment);
        await db.SaveChangesAsync();
        return assignment;
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
        public Guid? UserId => null; public string? Email => "quality-test@fixar.local"; public string? UserName => "Quality Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
