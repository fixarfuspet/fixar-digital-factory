using Fixar.API.Controllers;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class MaintenanceDashboardTests
{
    [Fact]
    public async Task Empty_database_returns_zero_value_dashboard()
    {
        await using var db = CreateDb();
        var result = await new MaintenanceDashboardController(db).Dashboard(default);
        var response = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<object>>(response.Value);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.All(
            new[] { "OpenRequestCount", "CriticalOpenRequestCount", "InProgressWorkOrderCount", "CompletedWorkOrdersThisMonth" },
            name => Assert.Equal(0, Convert.ToInt32(envelope.Data.GetType().GetProperty(name)!.GetValue(envelope.Data))));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options, new AuditableEntitySaveChangesInterceptor(new TestUser(), new TestClock()));
    }

    private sealed class TestClock : IDateTimeService { public DateTime UtcNow => DateTime.UtcNow; }
    private sealed class TestUser : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111"); public string? Email => "test@fixar.local"; public string? UserName => "Test"; public string? IpAddress => "127.0.0.1"; public bool IsAuthenticated => true; public IReadOnlyList<string> Roles => ["CEO"];
    }
}
