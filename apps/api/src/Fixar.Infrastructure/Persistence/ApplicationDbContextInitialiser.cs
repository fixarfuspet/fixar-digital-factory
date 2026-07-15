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
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
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

    public async Task SeedDevelopmentTestUsersAsync()
    {
        var password = Environment.GetEnvironmentVariable("FIXAR_DEV_TEST_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Development test user seed skipped: FIXAR_DEV_TEST_PASSWORD is not configured.");
            return;
        }
        var definitions = new (string Name, string Role)[]
        {
            ("test.ceo", RoleNames.CEO), ("test.manager", RoleNames.ProductionManager),
            ("test.injection", RoleNames.InjectionOperator), ("test.cutting", RoleNames.CuttingOperator),
            ("test.warehouse", RoleNames.WarehouseOperator), ("test.finance", RoleNames.Finance),
            ("test.viewer", RoleNames.Viewer)
        };
        foreach (var definition in definitions)
        {
            var email = $"{definition.Name}@fixar.test";
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, FirstName = "TEST", LastName = definition.Name, EmailConfirmed = true, IsActive = true };
                var created = await _userManager.CreateAsync(user, password);
                if (!created.Succeeded) throw new InvalidOperationException($"Development test user could not be created: {definition.Name}");
            }
            if (!user.IsActive) { user.IsActive = true; await _userManager.UpdateAsync(user); }
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(definition.Role)) await _userManager.AddToRoleAsync(user, definition.Role);
        }
        _logger.LogInformation("Development test users are ready (password not logged).");
    }
}
