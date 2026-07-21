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

public sealed class CuttingWorkflowSafetyTests
{
    [Fact]
    public async Task Completing_cutting_updates_order_item_cut_quantity_once()
    {
        await using var db = CreateDb();
        var seed = await SeedDraftCutting(db, produced: 10, alreadyCut: 0, goodPairs: 8);

        var result = await new CuttingRecordsController(db).Complete(seed.Record.Id, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Completed", seed.Record.Status);
        Assert.Equal(8, seed.Item.CutPairs);
        Assert.Contains(db.AuditLogs, x => x.EntityName == "Cutting Record Completed");
    }

    [Fact]
    public async Task Completing_cutting_cannot_exceed_order_item_produced_remaining_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedDraftCutting(db, produced: 10, alreadyCut: 7, goodPairs: 4);

        var result = await new CuttingRecordsController(db).Complete(seed.Record.Id, default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Draft", seed.Record.Status);
        Assert.Equal(7, seed.Item.CutPairs);
    }

    [Fact]
    public async Task Cancelling_completed_cutting_reverses_order_item_cut_quantity()
    {
        await using var db = CreateDb();
        var seed = await SeedDraftCutting(db, produced: 10, alreadyCut: 8, goodPairs: 8);
        seed.Record.Status = "Completed";
        await db.SaveChangesAsync();

        var result = await new CuttingRecordsController(db)
            .Cancel(seed.Record.Id, new CancelCuttingRecordRequest("Hatalı kayıt"), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(seed.Record.IsCancelled);
        Assert.Equal(0, seed.Item.CutPairs);
        Assert.Contains(db.AuditLogs, x => x.EntityName == "Cutting Record Cancelled");
    }

    private static async Task<(CuttingRecord Record, OrderItem Item)> SeedDraftCutting(
        ApplicationDbContext db, int produced, int alreadyCut, int goodPairs)
    {
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = $"TEST-C-{Guid.NewGuid():N}", Name = "TEST Müşteri" };
        var product = new Product { Id = Guid.NewGuid(), Code = $"TEST-P-{Guid.NewGuid():N}", Name = "TEST Ürün" };
        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = $"TEST-SO-{Guid.NewGuid():N}", Customer = customer,
            CustomerId = customer.Id, Product = product, ProductId = product.Id, Quantity = produced, Status = "Aktif"
        };
        var item = new OrderItem
        {
            Id = Guid.NewGuid(), Order = order, OrderId = order.Id, Product = product, ProductId = product.Id,
            LineNumber = 1, QuantityPairs = produced, ProducedPairs = produced, CutPairs = alreadyCut, Status = "Üretimde"
        };
        order.Items.Add(item);
        var station = new InjectionStation { Id = Guid.NewGuid(), StationNumber = 1, Name = "TEST İstasyon" };
        var assignment = new StationAssignment
        {
            Id = Guid.NewGuid(), InjectionStation = station, InjectionStationId = station.Id, OrderItem = item,
            OrderItemId = item.Id, StationNumberSnapshot = 1, PlannedPairs = produced, ProducedPairs = produced, Status = "Tamamlandı"
        };
        var machine = new CuttingMachine { Id = Guid.NewGuid(), Name = "TEST Gezer Kafa", MachineType = "Gezer Kafa", IsActive = true };
        var record = new CuttingRecord
        {
            Id = Guid.NewGuid(), RecordNumber = $"TEST-CUT-{Guid.NewGuid():N}", CuttingMachine = machine,
            CuttingMachineId = machine.Id, Order = order, OrderId = order.Id, OrderItem = item, OrderItemId = item.Id,
            StationAssignment = assignment, StationAssignmentId = assignment.Id, Product = product, ProductId = product.Id,
            InputPairs = goodPairs, GoodPairs = goodPairs, CutPairs = goodPairs, Status = "Draft", IsActive = true,
            RecordDate = DateTime.UtcNow, StartTime = DateTime.UtcNow
        };
        db.AddRange(customer, product, order, station, assignment, machine, record);
        await db.SaveChangesAsync();
        return (record, item);
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
        public Guid? UserId => null; public string? Email => "cutting-test@fixar.local"; public string? UserName => "Cutting Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
