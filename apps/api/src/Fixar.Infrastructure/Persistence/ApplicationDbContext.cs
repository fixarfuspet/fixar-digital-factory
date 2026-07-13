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

public DbSet<Mold> Molds => Set<Mold>();

public DbSet<InjectionStation> InjectionStations => Set<InjectionStation>();

public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();

public DbSet<CuttingMachine> CuttingMachines => Set<CuttingMachine>();

public DbSet<Machine> Machines => Set<Machine>();

public DbSet<Operator> Operators => Set<Operator>();

public DbSet<CuttingRecord> CuttingRecords => Set<CuttingRecord>();

public DbSet<ProductionBox> ProductionBoxes => Set<ProductionBox>();

public DbSet<ProductionBoxEvent> ProductionBoxEvents => Set<ProductionBoxEvent>();
public DbSet<OrderItem> OrderItems => Set<OrderItem>();
public DbSet<StationAssignment> StationAssignments => Set<StationAssignment>();
public DbSet<StationAssignmentFire> StationAssignmentFires => Set<StationAssignmentFire>();
public DbSet<StationAssignmentDowntime> StationAssignmentDowntimes => Set<StationAssignmentDowntime>();
public DbSet<StationAssignmentEvent> StationAssignmentEvents => Set<StationAssignmentEvent>();
public DbSet<StockItem> StockItems => Set<StockItem>();

public DbSet<StockMovement> StockMovements => Set<StockMovement>();
public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
public DbSet<Supplier> Suppliers => Set<Supplier>();
public DbSet<Material> Materials => Set<Material>();
public DbSet<ProductionSession> ProductionSessions => Set<ProductionSession>();
public DbSet<ProductionStation> ProductionStations => Set<ProductionStation>();
public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();
public DbSet<ProductionDowntime> ProductionDowntimes => Set<ProductionDowntime>();
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

        builder.Entity<Material>()
            .HasMany(x => x.StockItems)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasMany(x => x.Molds)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Operator>()
            .HasOne(x => x.DefaultMachine)
            .WithMany(x => x.DefaultOperators)
            .HasForeignKey(x => x.DefaultMachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Operator>()
            .HasOne(x => x.CurrentMachine)
            .WithMany(x => x.CurrentOperators)
            .HasForeignKey(x => x.CurrentMachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasMany(x => x.Stations)
            .WithOne(x => x.ProductionSession)
            .HasForeignKey(x => x.ProductionSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasMany(x => x.Events)
            .WithOne(x => x.ProductionSession)
            .HasForeignKey(x => x.ProductionSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasMany(x => x.Downtimes)
            .WithOne(x => x.ProductionSession)
            .HasForeignKey(x => x.ProductionSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionStation>()
            .HasMany(x => x.Events)
            .WithOne(x => x.ProductionStation)
            .HasForeignKey(x => x.ProductionStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionStation>()
            .HasMany(x => x.Downtimes)
            .WithOne(x => x.ProductionStation)
            .HasForeignKey(x => x.ProductionStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasOne(x => x.Machine)
            .WithMany()
            .HasForeignKey(x => x.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionSession>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionStation>()
            .HasOne(x => x.Mold)
            .WithMany()
            .HasForeignKey(x => x.MoldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionStation>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionStation>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StationAssignment>()
            .HasMany(x => x.Fires)
            .WithOne(x => x.StationAssignment)
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StationAssignment>()
            .HasMany(x => x.Downtimes)
            .WithOne(x => x.StationAssignment)
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StationAssignment>()
            .HasMany(x => x.Events)
            .WithOne(x => x.StationAssignment)
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StationAssignmentFire>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<StationAssignmentFire>()
            .HasIndex(x => x.RecordedAt);

        builder.Entity<StationAssignmentDowntime>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<StationAssignmentDowntime>()
            .HasIndex(x => x.IsOpen);

        builder.Entity<StationAssignmentDowntime>()
            .HasIndex(x => x.StationAssignmentId)
            .IsUnique()
            .HasFilter("\"IsOpen\" = true AND \"IsCancelled\" = false");

        builder.Entity<StationAssignmentEvent>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<StationAssignmentEvent>()
            .HasIndex(x => x.EventTime);

        builder.Entity<StationAssignmentEvent>()
            .HasIndex(x => x.EventType);
    }
}
