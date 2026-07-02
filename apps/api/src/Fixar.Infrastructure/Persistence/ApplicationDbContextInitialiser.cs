using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fixar.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds the fixed RBAC role list. Intended
/// to run once at startup in Development; in Production, migrations
/// should instead be applied via CI/CD (see apps/api/README.md).
/// </summary>
public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
    }

    public async Task MigrateAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            foreach (var roleName in RoleNames.All)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
