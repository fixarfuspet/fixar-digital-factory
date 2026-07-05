using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/dev-seed")]
public class DevSeedController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DevSeedController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 24; i++)
        {
            var exists = await _db.InjectionStations
                .AnyAsync(x => x.StationNumber == i, cancellationToken);

            if (!exists)
            {
                _db.InjectionStations.Add(new InjectionStation
                {
                    StationNumber = i,
                    Name = $"İstasyon {i}",
                    Status = "Aktif",
                    IsActive = true
                });
            }
        }

        var icemen = await GetOrCreateCustomer("Icemen", cancellationToken);
        var dogo = await GetOrCreateCustomer("Dogo", cancellationToken);

        var memory10900 = await GetOrCreateProduct("10900 Memory Foam", "10900", cancellationToken);
        var comfyLight = await GetOrCreateProduct("Comfy Light", "CL", cancellationToken);

        var ice3945 = await GetOrCreateMold("ICE 39-45", "ICE-39-45", "39-45", cancellationToken);
        var cl4041 = await GetOrCreateMold("CL 40-41", "CL-40-41", "40-41", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var icemenOrderExists = await _db.Orders.AnyAsync(x => x.CustomerId == icemen.Id, cancellationToken);

        if (!icemenOrderExists)
        {
            var order = new Order
            {
                CustomerId = icemen.Id,
                ProductId = memory10900.Id,
                SizeRange = "39-45",
                Color = "Siyah",
                Quantity = 50000,
                ProducedQuantity = 0,
                CutQuantity = 0,
                ShippedQuantity = 0,
                DueDate = DateTime.UtcNow.AddDays(14),
                Status = "Aktif"
            };

            order.Items.Add(new OrderItem
            {
                ProductId = memory10900.Id,
                MoldId = ice3945.Id,
                ProductionType = "Kumaşlı",
                FabricColor = "Siyah",
                QuantityPairs = 22000,
                ProducedPairs = 0,
                CutPairs = 0,
                ShippedPairs = 0,
                Status = "Bekliyor",
                Note = "Icemen 39-45 siyah kumaşlı"
            });

            _db.Orders.Add(order);
        }

        var dogoOrderExists = await _db.Orders.AnyAsync(x => x.CustomerId == dogo.Id, cancellationToken);

        if (!dogoOrderExists)
        {
            var order = new Order
            {
                CustomerId = dogo.Id,
                ProductId = comfyLight.Id,
                SizeRange = "40-41",
                Color = "Beyaz",
                Quantity = 8000,
                ProducedQuantity = 0,
                CutQuantity = 0,
                ShippedQuantity = 0,
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = "Aktif"
            };

            order.Items.Add(new OrderItem
            {
                ProductId = comfyLight.Id,
                MoldId = cl4041.Id,
                ProductionType = "Kumaşlı",
                FabricColor = "Beyaz",
                QuantityPairs = 8000,
                ProducedPairs = 0,
                CutPairs = 0,
                ShippedPairs = 0,
                Status = "Bekliyor",
                Note = "Dogo Comfy Light 40-41 beyaz"
            });

            _db.Orders.Add(order);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = "Test verileri oluşturuldu.",
            customers = await _db.Customers.CountAsync(cancellationToken),
            products = await _db.Products.CountAsync(cancellationToken),
            molds = await _db.Molds.CountAsync(cancellationToken),
            orders = await _db.Orders.CountAsync(cancellationToken),
            orderItems = await _db.OrderItems.CountAsync(cancellationToken),
            stations = await _db.InjectionStations.CountAsync(cancellationToken)
        }));
    }

    private async Task<Customer> GetOrCreateCustomer(string name, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (customer is not null)
            return customer;

        customer = new Customer
        {
            Name = name,
            CompanyName = name,
            IsActive = true
        };

        _db.Customers.Add(customer);
        return customer;
    }

    private async Task<Product> GetOrCreateProduct(string name, string code, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (product is not null)
            return product;

        product = new Product
        {
            Name = name,
            Code = code,
            IsActive = true
        };

        _db.Products.Add(product);
        return product;
    }

    private async Task<Mold> GetOrCreateMold(string name, string code, string sizeRange, CancellationToken cancellationToken)
    {
        var mold = await _db.Molds.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (mold is not null)
            return mold;

        mold = new Mold
        {
            Name = name,
            Code = code,
            SizeRange = sizeRange,
            IsActive = true
        };

        _db.Molds.Add(mold);
        return mold;
    }
}