using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fixar.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds the fixed RBAC role list during
/// application startup, before the API begins accepting requests.
/// </summary>
public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task MigrateAsync()
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            _logger.LogInformation(
                "Applying database migrations to {Database} on {DataSource}",
                connection.Database,
                connection.DataSource);

            await _context.Database.MigrateAsync();

            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToArray();
            if (pendingMigrations.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Database migration did not complete. Pending migrations: {string.Join(", ", pendingMigrations)}");
            }

            var appliedMigrationCount = (await _context.Database.GetAppliedMigrationsAsync()).Count();
            _logger.LogInformation(
                "Database migration completed successfully; {AppliedMigrationCount} migrations are applied",
                appliedMigrationCount);
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
                    EnsureSucceeded(
                        await _roleManager.CreateAsync(new ApplicationRole(roleName)),
                        $"Role '{roleName}' could not be created");
                }
            }

            await SeedBootstrapAdminAsync();
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
            else if (user.Email?.EndsWith("@fixar.test", StringComparison.OrdinalIgnoreCase) == true)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await _userManager.ResetPasswordAsync(user, resetToken, password);
                if (!reset.Succeeded) throw new InvalidOperationException($"Development test user password could not be refreshed: {definition.Name}");
            }
            if (!user.IsActive) { user.IsActive = true; await _userManager.UpdateAsync(user); }
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(definition.Role)) await _userManager.AddToRoleAsync(user, definition.Role);
        }
        _logger.LogInformation("Development test users are ready (password not logged).");
    }

    private async Task SeedBootstrapAdminAsync()
    {
        var email = _configuration["BootstrapAdmin:Email"]?.Trim();
        var password = _configuration["BootstrapAdmin:Password"];
        var firstName = _configuration["BootstrapAdmin:FirstName"]?.Trim();
        var lastName = _configuration["BootstrapAdmin:LastName"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin:Email and BootstrapAdmin:Password must be configured in Production.");
            }

            _logger.LogInformation("Bootstrap admin seed skipped because it is not configured.");
            return;
        }

        var admin = await _userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? "FIXAR" : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? "Administrator" : lastName,
                EmailConfirmed = true,
                IsActive = true
            };

            EnsureSucceeded(
                await _userManager.CreateAsync(admin, password),
                "Bootstrap admin user could not be created");
        }
        else
        {
            admin.UserName = email;
            admin.Email = email;
            admin.EmailConfirmed = true;
            admin.IsActive = true;
            if (!string.IsNullOrWhiteSpace(firstName)) admin.FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName)) admin.LastName = lastName;

            EnsureSucceeded(await _userManager.UpdateAsync(admin), "Bootstrap admin user could not be updated");

            var passwordToken = await _userManager.GeneratePasswordResetTokenAsync(admin);
            EnsureSucceeded(
                await _userManager.ResetPasswordAsync(admin, passwordToken, password),
                "Bootstrap admin password could not be synchronized");
        }

        if (!await _userManager.IsInRoleAsync(admin, RoleNames.CEO))
        {
            EnsureSucceeded(
                await _userManager.AddToRoleAsync(admin, RoleNames.CEO),
                "Bootstrap admin CEO role could not be assigned");
        }

        _logger.LogInformation("Bootstrap admin {AdminEmail} is active and ready", email);
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded) return;

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"{message}. {errors}");
    }
}
