using Fixar.Domain.Entities;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fixar.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
        : base(options)
    {
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
public DbSet<Customer> Customers => Set<Customer>();

public DbSet<Product> Products => Set<Product>();

public DbSet<Order> Orders => Set<Order>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Rename the default Identity tables to plain, readable names.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
