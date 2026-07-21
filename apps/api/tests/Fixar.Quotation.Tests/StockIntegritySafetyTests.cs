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

public sealed class StockIntegritySafetyTests
{
    [Fact]
    public async Task Material_linked_stock_rejects_direct_movement()
    {
        await using var db = CreateDb();
        var material = new Material { Id = Guid.NewGuid(), Code = "TEST-MAT", Name = "TEST Malzeme", Unit = "kg" };
        var stock = new StockItem { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id, Code = "TEST-STK", Name = "TEST Stok", Unit = "kg", CurrentQuantity = 10 };
        db.AddRange(material, stock);
        await db.SaveChangesAsync();

        var result = await new StocksController(db).AddMovement(
            new CreateStockMovementRequest(stock.Id, "Çıkış", 1, null, null, null, null, "Doğrudan çıkış"), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(10, stock.CurrentQuantity);
        Assert.Empty(db.StockMovements);
    }

    [Fact]
    public async Task Material_linked_stock_rejects_quantity_edit()
    {
        await using var db = CreateDb();
        var material = new Material { Id = Guid.NewGuid(), Code = "TEST-MAT", Name = "TEST Malzeme", Unit = "kg" };
        var stock = new StockItem { Id = Guid.NewGuid(), Material = material, MaterialId = material.Id, Code = "TEST-STK", Name = "TEST Stok", Unit = "kg", CurrentQuantity = 10 };
        db.AddRange(material, stock);
        await db.SaveChangesAsync();

        var result = await new StocksController(db).Update(stock.Id, Request(stock, 9, "Düzeltme"), default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(10, stock.CurrentQuantity);
    }

    [Fact]
    public async Task General_stock_adjustment_requires_reason_and_creates_movement()
    {
        await using var db = CreateDb();
        var stock = new StockItem { Id = Guid.NewGuid(), Code = "TEST-GEN", Name = "TEST Genel", Unit = "adet", CurrentQuantity = 10 };
        db.Add(stock);
        await db.SaveChangesAsync();

        var missingReason = await new StocksController(db).Update(stock.Id, Request(stock, 8, null), default);
        Assert.IsType<BadRequestObjectResult>(missingReason);

        var result = await new StocksController(db).Update(stock.Id, Request(stock, 8, "Sayım farkı"), default);
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(8, stock.CurrentQuantity);
        var movement = Assert.Single(db.StockMovements);
        Assert.Equal("Sayım Çıkışı", movement.MovementType);
        Assert.Equal(2, movement.Quantity);
    }

    private static CreateStockItemRequest Request(StockItem stock, decimal quantity, string? note) => new(
        stock.Name, stock.Code, stock.Category, stock.Unit, quantity, stock.CriticalQuantity, stock.MinimumQuantity,
        stock.MaximumQuantity, stock.LastPurchasePrice, stock.Currency, stock.VatRate, stock.SupplierName,
        stock.SupplierCode, stock.LeadTimeDays, stock.WarehouseName, stock.LocationCode, stock.LotNumber,
        stock.ExpiryDate, stock.RecipeUsageAmount, stock.WasteRate, stock.SafetyInfo, note);

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
        public Guid? UserId => null; public string? Email => "stock-test@fixar.local"; public string? UserName => "Stock Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
