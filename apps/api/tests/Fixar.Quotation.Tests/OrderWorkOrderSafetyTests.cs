using Fixar.API.Controllers;
using Fixar.Application.Common.Interfaces;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class OrderWorkOrderSafetyTests
{
    [Fact]
    public async Task Order_quantity_cannot_be_reduced_below_shipped_quantity()
    {
        await using var db = CreateDb();
        var seed = SeedOrder(db, quantity: 10, shipped: 8);
        await db.SaveChangesAsync();
        var request = new OrderRequest(seed.Customer.Id, DateTime.UtcNow, null, null, "TRY", "OpenAccount", 0, 0, 20, null,
            [new OrderItemRequest(seed.Item.Id, seed.Product.Id, null, null, null, null, null, 7, 10, 0, 20, null, null)]);

        var result = await new OrdersController(db).Update(seed.Order.Id, request, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(10, seed.Item.QuantityPairs);
    }

    [Fact]
    public async Task Duplicate_work_order_cannot_overplan_order_remaining_quantity()
    {
        await using var db = CreateDb();
        var seed = SeedOrder(db, quantity: 10, shipped: 0);
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(), WorkOrderNumber = "TEST-WO-001", OrderItemId = seed.Item.Id, OrderItem = seed.Item,
            ProductId = seed.Product.Id, Product = seed.Product, PlannedPairs = 10, Priority = "Normal", Status = "Draft"
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrdersController(db).Duplicate(workOrder.Id, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.WorkOrders);
    }

    private static (Customer Customer, Product Product, Order Order, OrderItem Item) SeedOrder(ApplicationDbContext db, int quantity, int shipped)
    {
        var customer = new Customer { Id = Guid.NewGuid(), CustomerCode = $"TEST-C-{Guid.NewGuid():N}", Name = "TEST Müşteri", IsActive = true };
        var product = new Product { Id = Guid.NewGuid(), Code = $"TEST-P-{Guid.NewGuid():N}", Name = "TEST Ürün", IsActive = true };
        var order = new Order { Id = Guid.NewGuid(), OrderNumber = $"TEST-SO-{Guid.NewGuid():N}", Customer = customer, CustomerId = customer.Id, Product = product, ProductId = product.Id, Quantity = quantity, Status = "Draft" };
        var item = new OrderItem { Id = Guid.NewGuid(), Order = order, OrderId = order.Id, Product = product, ProductId = product.Id, LineNumber = 1, QuantityPairs = quantity, ShippedPairs = shipped, IsActive = true };
        order.Items.Add(item);
        db.AddRange(customer, product, order);
        return (customer, product, order, item);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new AuditableEntitySaveChangesInterceptor(new TestUser(), new TestClock()));
    }
    private sealed class TestClock : IDateTimeService { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class TestUser : ICurrentUserService
    {
        public Guid? UserId => null; public string? Email => "order-test@fixar.local"; public string? UserName => "Order Test";
        public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => false; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
