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
public DbSet<Recipe> Recipes => Set<Recipe>();
public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
public DbSet<ProductionSession> ProductionSessions => Set<ProductionSession>();
public DbSet<ProductionStation> ProductionStations => Set<ProductionStation>();
public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();
public DbSet<ProductionDowntime> ProductionDowntimes => Set<ProductionDowntime>();
public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
public DbSet<QualityDefect> QualityDefects => Set<QualityDefect>();
public DbSet<MaterialLot> MaterialLots => Set<MaterialLot>();
public DbSet<MaterialContainer> MaterialContainers => Set<MaterialContainer>();
public DbSet<StockReservation> StockReservations => Set<StockReservation>();
public DbSet<StockReservationLine> StockReservationLines => Set<StockReservationLine>();
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
        builder.Entity<MaterialLot>().HasIndex(x=>x.LotNumber).IsUnique();
        builder.Entity<MaterialLot>().HasIndex(x=>x.MaterialId); builder.Entity<MaterialLot>().HasIndex(x=>x.StockItemId); builder.Entity<MaterialLot>().HasIndex(x=>x.SupplierId); builder.Entity<MaterialLot>().HasIndex(x=>x.PurchaseOrderLineId); builder.Entity<MaterialLot>().HasIndex(x=>x.ReceivedDate); builder.Entity<MaterialLot>().HasIndex(x=>x.ExpiryDate); builder.Entity<MaterialLot>().HasIndex(x=>x.Status); builder.Entity<MaterialLot>().HasIndex(x=>x.QualityStatus); builder.Entity<MaterialLot>().HasIndex(x=>x.IsBlocked); builder.Entity<MaterialLot>().HasIndex(x=>x.IsActive);
        builder.Entity<MaterialLot>().HasOne(x=>x.Material).WithMany().HasForeignKey(x=>x.MaterialId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialLot>().HasOne(x=>x.StockItem).WithMany().HasForeignKey(x=>x.StockItemId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialLot>().HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialLot>().HasOne(x=>x.PurchaseOrder).WithMany().HasForeignKey(x=>x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialLot>().HasOne(x=>x.PurchaseOrderLine).WithMany().HasForeignKey(x=>x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MaterialContainer>().HasIndex(x=>x.ContainerCode).IsUnique(); builder.Entity<MaterialContainer>().HasIndex(x=>x.MaterialLotId); builder.Entity<MaterialContainer>().HasIndex(x=>x.MaterialId); builder.Entity<MaterialContainer>().HasIndex(x=>x.StockItemId); builder.Entity<MaterialContainer>().HasIndex(x=>x.Status); builder.Entity<MaterialContainer>().HasIndex(x=>x.OpenedAt); builder.Entity<MaterialContainer>().HasIndex(x=>x.IsDamaged); builder.Entity<MaterialContainer>().HasIndex(x=>x.IsBlocked); builder.Entity<MaterialContainer>().HasIndex(x=>x.IsActive);
        builder.Entity<MaterialContainer>().HasOne(x=>x.MaterialLot).WithMany(x=>x.Containers).HasForeignKey(x=>x.MaterialLotId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialContainer>().HasOne(x=>x.Material).WithMany().HasForeignKey(x=>x.MaterialId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialContainer>().HasOne(x=>x.StockItem).WithMany().HasForeignKey(x=>x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockReservation>().HasIndex(x => x.ReservationNumber).IsUnique();
        builder.Entity<StockReservation>().HasIndex(x => x.WorkOrderId); builder.Entity<StockReservation>().HasIndex(x => x.Status); builder.Entity<StockReservation>().HasIndex(x => x.ReservationDate); builder.Entity<StockReservation>().HasIndex(x => x.IsActive);
        builder.Entity<StockReservation>().HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockReservationLine>().HasIndex(x => x.StockReservationId); builder.Entity<StockReservationLine>().HasIndex(x => x.MaterialId); builder.Entity<StockReservationLine>().HasIndex(x => x.StockItemId); builder.Entity<StockReservationLine>().HasIndex(x => x.MaterialLotId); builder.Entity<StockReservationLine>().HasIndex(x => x.MaterialContainerId);
        builder.Entity<StockReservationLine>().HasIndex(x => new { x.StockReservationId, x.MaterialContainerId }).IsUnique().HasFilter("\"MaterialContainerId\" IS NOT NULL");
        builder.Entity<StockReservationLine>().HasOne(x => x.StockReservation).WithMany(x => x.Lines).HasForeignKey(x => x.StockReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<StockReservationLine>().HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict); builder.Entity<StockReservationLine>().HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict); builder.Entity<StockReservationLine>().HasOne(x => x.MaterialLot).WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict); builder.Entity<StockReservationLine>().HasOne(x => x.MaterialContainer).WithMany().HasForeignKey(x => x.MaterialContainerId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasMany(x => x.Molds)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasMany(x => x.Recipes)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Recipe>()
            .HasIndex(x => x.Code)
            .IsUnique();

        builder.Entity<Recipe>()
            .HasIndex(x => x.ProductId);

        builder.Entity<Recipe>()
            .HasIndex(x => x.IsActive);

        builder.Entity<Recipe>()
            .HasIndex(x => new { x.ProductId, x.Version })
            .IsUnique();

        builder.Entity<Recipe>()
            .HasIndex(x => new { x.ProductId, x.IsDefault })
            .IsUnique()
            .HasDatabaseName("IX_Recipes_ProductId_IsDefault_Active")
            .HasFilter("\"IsDefault\" = true AND \"IsActive\" = true");

        builder.Entity<Recipe>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Recipe)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Material>()
            .HasMany(x => x.RecipeItems)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RecipeItem>()
            .HasIndex(x => x.RecipeId);

        builder.Entity<RecipeItem>()
            .HasIndex(x => x.MaterialId);

        builder.Entity<RecipeItem>()
            .HasIndex(x => new { x.RecipeId, x.MaterialId })
            .IsUnique();

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.WorkOrderNumber)
            .IsUnique();

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.OrderItemId);

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.ProductId);

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.RecipeId);

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.Status);

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.IsActive);

        builder.Entity<WorkOrder>()
            .HasIndex(x => x.PlannedStartDate);

        builder.Entity<WorkOrder>()
            .HasOne(x => x.OrderItem)
            .WithMany(x => x.WorkOrders)
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        builder.Entity<Customer>().HasIndex(x => x.IsActive);
        builder.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();
        builder.Entity<Order>().HasIndex(x => new { x.CustomerId, x.Status });
        builder.Entity<Order>().HasIndex(x => x.OrderDate);
        builder.Entity<OrderItem>().HasIndex(x => new { x.OrderId, x.LineNumber }).IsUnique();
        builder.Entity<OrderItem>().HasIndex(x => x.IsActive);

        builder.Entity<WorkOrder>()
            .HasOne(x => x.Product)
            .WithMany(x => x.WorkOrders)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkOrder>()
            .HasOne(x => x.Recipe)
            .WithMany(x => x.WorkOrders)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkOrder>()
            .HasOne(x => x.AssignedMachine)
            .WithMany(x => x.WorkOrders)
            .HasForeignKey(x => x.AssignedMachineId)
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

        builder.Entity<StationAssignment>()
            .HasOne(x => x.WorkOrder)
            .WithMany(x => x.StationAssignments)
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StationAssignment>()
            .HasIndex(x => x.WorkOrderId);

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

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.InspectionNumber)
            .IsUnique();

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.WorkOrderId);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.OrderItemId);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.ProductId);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.Result);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.InspectionType);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.InspectionDate);

        builder.Entity<QualityInspection>()
            .HasIndex(x => x.IsActive);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.StationAssignment)
            .WithMany()
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.WorkOrder)
            .WithMany()
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.Mold)
            .WithMany()
            .HasForeignKey(x => x.MoldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.Machine)
            .WithMany()
            .HasForeignKey(x => x.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityInspection>()
            .HasMany(x => x.Defects)
            .WithOne(x => x.QualityInspection)
            .HasForeignKey(x => x.QualityInspectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QualityDefect>()
            .HasIndex(x => x.QualityInspectionId);

        builder.Entity<QualityDefect>()
            .HasIndex(x => x.StationAssignmentFireId);

        builder.Entity<QualityDefect>()
            .HasIndex(x => x.DefectType);

        builder.Entity<QualityDefect>()
            .HasOne(x => x.StationAssignmentFire)
            .WithMany()
            .HasForeignKey(x => x.StationAssignmentFireId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.RecordNumber)
            .IsUnique();

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.WorkOrderId);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.OrderItemId);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.ProductId);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.CuttingMachineId);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.RecordDate);

        builder.Entity<CuttingRecord>()
            .HasIndex(x => x.Status);

        builder.Entity<CuttingRecord>()
            .HasOne(x => x.StationAssignment)
            .WithMany()
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CuttingRecord>()
            .HasOne(x => x.WorkOrder)
            .WithMany()
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CuttingRecord>()
            .HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CuttingRecord>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CuttingRecord>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.BoxNumber)
            .IsUnique();

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.Barcode);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.TraceabilityCode)
            .IsUnique();

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.CuttingRecordId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.StationAssignmentId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.WorkOrderId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.OrderItemId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.ProductId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.CustomerId);

        builder.Entity<ProductionBox>()
            .HasIndex(x => x.Status);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.StationAssignment)
            .WithMany()
            .HasForeignKey(x => x.StationAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.CuttingRecord)
            .WithMany()
            .HasForeignKey(x => x.CuttingRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.WorkOrder)
            .WithMany()
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductionBox>()
            .HasOne(x => x.PackedByOperator)
            .WithMany()
            .HasForeignKey(x => x.PackedByOperatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
