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

public sealed class ProductionQuantitySafetyTests
{
    [Fact]
    public async Task Manual_production_cannot_exceed_assignment_or_order_item_remaining_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedActiveAssignment(db, produced: 9, planned: 10, ordered: 10);

        var result = await new StationAssignmentsController(db)
            .AddProduction(new AddProductionRequest(seed.Assignment.Id, 2), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(9, seed.Assignment.ProducedPairs);
        Assert.Equal(9, seed.Item.ProducedPairs);
        Assert.Empty(db.StationAssignmentEvents);
    }

    [Fact]
    public async Task Turn_cannot_exceed_assignment_or_order_item_remaining_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedActiveAssignment(db, produced: 9, planned: 10, ordered: 10);

        var result = await new StationAssignmentsController(db)
            .AddTurn(new AddTurnRequest(2, null, "turn-over-limit"), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(9, seed.Assignment.ProducedPairs);
        Assert.Equal(9, seed.Item.ProducedPairs);
        Assert.Empty(db.StationAssignmentEvents);
    }

    [Fact]
    public async Task Turn_at_remaining_quantity_updates_assignment_order_item_and_audit_event()
    {
        await using var db = CreateDb();
        var seed = await SeedActiveAssignment(db, produced: 9, planned: 10, ordered: 10);

        var result = await new StationAssignmentsController(db)
            .AddTurn(new AddTurnRequest(1, "Son tur", "turn-final"), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(10, seed.Assignment.ProducedPairs);
        Assert.Equal(10, seed.Item.ProducedPairs);
        var auditEvent = Assert.Single(db.StationAssignmentEvents);
        Assert.Equal("Tur Eklendi", auditEvent.EventType);
        Assert.Equal(1, auditEvent.Quantity);
        Assert.Contains("turn-final", auditEvent.MetadataJson);
    }

    private static async Task<(StationAssignment Assignment, OrderItem Item)> SeedActiveAssignment(
        ApplicationDbContext db, int produced, int planned, int ordered)
    {
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = $"TEST-C-{Guid.NewGuid():N}", Name = "TEST Müşteri" };
        var product = new Product { Id = Guid.NewGuid(), Code = $"TEST-P-{Guid.NewGuid():N}", Name = "TEST Ürün" };
        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = $"TEST-SO-{Guid.NewGuid():N}", Customer = customer,
            CustomerId = customer.Id, Product = product, ProductId = product.Id, Quantity = ordered, Status = "Aktif"
        };
        var item = new OrderItem
        {
            Id = Guid.NewGuid(), Order = order, OrderId = order.Id, Product = product, ProductId = product.Id,
            LineNumber = 1, QuantityPairs = ordered, ProducedPairs = produced, Status = "Üretimde"
        };
        order.Items.Add(item);
        var station = new InjectionStation { Id = Guid.NewGuid(), StationNumber = 1, Name = "TEST İstasyon", Status = "Üretimde" };
        var assignment = new StationAssignment
        {
            Id = Guid.NewGuid(), InjectionStation = station, InjectionStationId = station.Id, OrderItem = item,
            OrderItemId = item.Id, StationNumberSnapshot = 1, PlannedPairs = planned, ProducedPairs = produced,
            Status = "Üretimde"
        };
        db.AddRange(customer, product, order, station, assignment);
        await db.SaveChangesAsync();
        return (assignment, item);
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
        public Guid? UserId => null; public string? Email => "production-test@fixar.local"; public string? UserName => "Production Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
