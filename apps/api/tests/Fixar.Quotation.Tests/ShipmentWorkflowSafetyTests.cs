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

public sealed class ShipmentWorkflowSafetyTests
{
    [Fact]
    public async Task Shipping_box_updates_order_item_shipped_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedReadyBox(db, cut: 10, shipped: 5, boxPairs: 4);

        var result = await new ProductionBoxesController(db)
            .Ship(seed.Box.Id, new ShipBoxRequest("TEST-SEVK-1", null, null), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Shipped", seed.Box.Status);
        Assert.Equal(9, seed.Item.ShippedPairs);
        Assert.Contains(db.AuditLogs, x => x.EntityName == "Production Box Shipped");
    }

    [Fact]
    public async Task Shipping_box_cannot_exceed_cut_remaining_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedReadyBox(db, cut: 10, shipped: 8, boxPairs: 3);

        var result = await new ProductionBoxesController(db)
            .Ship(seed.Box.Id, new ShipBoxRequest("TEST-SEVK-2", null, null), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("ReadyForShipment", seed.Box.Status);
        Assert.Equal(8, seed.Item.ShippedPairs);
    }

    [Fact]
    public async Task Bulk_shipment_rejects_duplicate_box_selection()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var result = await new ProductionBoxesController(db)
            .BulkShip(new BulkShipRequest([id, id], "TEST-SEVK-3", null, null), default);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static async Task<(ProductionBox Box, OrderItem Item)> SeedReadyBox(
        ApplicationDbContext db, int cut, int shipped, int boxPairs)
    {
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = "TEST-C", Name = "TEST Müşteri" };
        var product = new Product { Id = Guid.NewGuid(), Code = "TEST-P", Name = "TEST Ürün" };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = "TEST-SO", Customer = customer, CustomerId = customer.Id, Product = product, ProductId = product.Id, Quantity = cut };
        var item = new OrderItem { Id = Guid.NewGuid(), Order = order, OrderId = order.Id, Product = product, ProductId = product.Id, QuantityPairs = cut, ProducedPairs = cut, CutPairs = cut, ShippedPairs = shipped, LineNumber = 1 };
        order.Items.Add(item);
        var box = new ProductionBox
        {
            Id = Guid.NewGuid(), BoxCode = "TEST-BOX", BoxNumber = "TEST-BOX", Barcode = "TEST-BOX",
            TraceabilityCode = Guid.NewGuid().ToString("N"), Order = order, OrderId = order.Id,
            OrderItem = item, OrderItemId = item.Id, Customer = customer, CustomerId = customer.Id,
            Product = product, ProductId = product.Id, PairCount = boxPairs, QuantityPairs = boxPairs,
            Status = "ReadyForShipment", CurrentStatus = "ReadyForShipment", IsActive = true
        };
        db.AddRange(customer, product, order, box);
        await db.SaveChangesAsync();
        return (box, item);
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
        public Guid? UserId => null; public string? Email => "shipment-test@fixar.local"; public string? UserName => "Shipment Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
