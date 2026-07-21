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

public sealed class PurchaseReceiptSafetyTests
{
    [Fact]
    public async Task Material_purchase_does_not_increase_stock_before_lot_receipt()
    {
        await using var db = CreateDb();
        var material = new Material { Id = Guid.NewGuid(), Code = "TEST-M", Name = "TEST Malzeme", Unit = "kg" };
        var stock = new StockItem { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id, Code = "TEST-S", Name = "TEST Stok", Unit = "kg", CurrentQuantity = 0 };
        db.AddRange(material, stock);
        await db.SaveChangesAsync();

        var result = await new PurchasesController(db).Create(Request(stock.Id, 10), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0, stock.CurrentQuantity);
        Assert.Empty(db.StockMovements);
        Assert.Single(db.PurchaseOrderLines);
    }

    [Fact]
    public async Task General_purchase_keeps_direct_stock_receipt_behavior()
    {
        await using var db = CreateDb();
        var stock = new StockItem { Id = Guid.NewGuid(), Code = "TEST-G", Name = "TEST Genel", Unit = "adet", CurrentQuantity = 0 };
        db.Add(stock);
        await db.SaveChangesAsync();

        var result = await new PurchasesController(db).Create(Request(stock.Id, 10), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(10, stock.CurrentQuantity);
        Assert.Single(db.StockMovements);
    }

    [Fact]
    public async Task Lot_receipts_cannot_exceed_purchase_line_quantity()
    {
        await using var db = CreateDb();
        var material = new Material { Id = Guid.NewGuid(), Code = "TEST-M", Name = "TEST Malzeme", Unit = "kg" };
        var stock = new StockItem { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id, Code = "TEST-S", Name = "TEST Stok", Unit = "kg" };
        var purchase = new PurchaseOrder { Id = Guid.NewGuid(), SupplierName = "TEST Tedarikçi", Currency = "TRY" };
        var line = new PurchaseOrderLine { Id = Guid.NewGuid(), PurchaseOrder = purchase, PurchaseOrderId = purchase.Id, StockItem = stock, StockItemId = stock.Id, StockName = stock.Name, Quantity = 10, Unit = "kg", UnitPrice = 1 };
        purchase.Lines.Add(line);
        var existing = new MaterialLot { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id, StockItem = stock, StockItemId = stock.Id, PurchaseOrder = purchase, PurchaseOrderId = purchase.Id, PurchaseOrderLine = line, PurchaseOrderLineId = line.Id, LotNumber = "TEST-LOT-1", InitialQuantity = 8, CurrentQuantity = 8, Unit = "kg" };
        db.AddRange(material, stock, purchase, existing);
        await db.SaveChangesAsync();

        var request = new MaterialLotRequest(material.Id, stock.Id, null, purchase.Id, line.Id, "TEST-LOT-2", null, null, null, null, null, 3, null, null, "kg", 1, "TRY", null, null, null, "Approved", null);
        var result = await new MaterialLotsController(db).Create(request, default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.MaterialLots);
        Assert.Equal(0, stock.CurrentQuantity);
    }

    private static CreatePurchaseOrderRequest Request(Guid stockId, decimal quantity) => new(
        "TEST Tedarikçi", "TEST-T", "TEST-IRS", null, null, null, "Cari Hesap", "TRY", 20,
        null, null, null, "Oluşturuldu", null,
        [new CreatePurchaseOrderLineRequest(stockId, null, quantity, null, 1, null, null)]);

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
        public Guid? UserId => null; public string? Email => "purchase-test@fixar.local"; public string? UserName => "Purchase Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
