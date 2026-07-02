using Fixar.Infrastructure.Persistence;

namespace Fixar.API.Extensions;

public static class DatabaseInitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.MigrateAsync();
        await initialiser.SeedAsync();
    }
}
