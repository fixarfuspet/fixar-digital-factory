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
public DbSet<MaterialConsumption> MaterialConsumptions => Set<MaterialConsumption>();
public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
public DbSet<CostSettings> CostSettings => Set<CostSettings>();
public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
public DbSet<WorkOrderCostSnapshot> WorkOrderCostSnapshots => Set<WorkOrderCostSnapshot>();
public DbSet<WorkOrderCostLine> WorkOrderCostLines => Set<WorkOrderCostLine>();
public DbSet<ProfitabilitySettings> ProfitabilitySettings => Set<ProfitabilitySettings>();
public DbSet<CustomerReceivable> CustomerReceivables => Set<CustomerReceivable>();
public DbSet<CustomerCollection> CustomerCollections => Set<CustomerCollection>();
public DbSet<CollectionAllocation> CollectionAllocations => Set<CollectionAllocation>();
public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();
public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
public DbSet<CustomerCheque> CustomerCheques => Set<CustomerCheque>();
public DbSet<ChequeEvent> ChequeEvents => Set<ChequeEvent>();
public DbSet<SupplierPayable> SupplierPayables=>Set<SupplierPayable>();public DbSet<SupplierPayment>SupplierPayments=>Set<SupplierPayment>();public DbSet<SupplierPaymentAllocation>SupplierPaymentAllocations=>Set<SupplierPaymentAllocation>();public DbSet<SupplierLedgerEntry>SupplierLedgerEntries=>Set<SupplierLedgerEntry>();public DbSet<ChequeEndorsement>ChequeEndorsements=>Set<ChequeEndorsement>();
public DbSet<MaintenanceAsset> MaintenanceAssets=>Set<MaintenanceAsset>(); public DbSet<MaintenanceRequest> MaintenanceRequests=>Set<MaintenanceRequest>(); public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders=>Set<MaintenanceWorkOrder>(); public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans=>Set<PreventiveMaintenancePlan>(); public DbSet<MaintenanceChecklistTemplate> MaintenanceChecklistTemplates=>Set<MaintenanceChecklistTemplate>(); public DbSet<MaintenanceChecklistTemplateItem> MaintenanceChecklistTemplateItems=>Set<MaintenanceChecklistTemplateItem>(); public DbSet<MaintenanceChecklistResult> MaintenanceChecklistResults=>Set<MaintenanceChecklistResult>(); public DbSet<MaintenancePartUsage> MaintenancePartUsages=>Set<MaintenancePartUsage>();
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
        builder.Entity<MaterialConsumption>().HasIndex(x => x.ConsumptionNumber).IsUnique(); builder.Entity<MaterialConsumption>().HasIndex(x => x.WorkOrderId); builder.Entity<MaterialConsumption>().HasIndex(x => x.StationAssignmentId); builder.Entity<MaterialConsumption>().HasIndex(x => x.StockReservationId); builder.Entity<MaterialConsumption>().HasIndex(x => x.StockReservationLineId); builder.Entity<MaterialConsumption>().HasIndex(x => x.MaterialId); builder.Entity<MaterialConsumption>().HasIndex(x => x.MaterialLotId); builder.Entity<MaterialConsumption>().HasIndex(x => x.MaterialContainerId); builder.Entity<MaterialConsumption>().HasIndex(x => x.ConsumptionDate); builder.Entity<MaterialConsumption>().HasIndex(x => x.ConsumptionType); builder.Entity<MaterialConsumption>().HasIndex(x => x.IsReversed);
        builder.Entity<MaterialConsumption>().HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.StationAssignment).WithMany().HasForeignKey(x => x.StationAssignmentId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.StockReservation).WithMany().HasForeignKey(x => x.StockReservationId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.StockReservationLine).WithMany().HasForeignKey(x => x.StockReservationLineId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.StockItem).WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.MaterialLot).WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict); builder.Entity<MaterialConsumption>().HasOne(x => x.MaterialContainer).WithMany().HasForeignKey(x => x.MaterialContainerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IdempotencyRecord>().HasIndex(x => new { x.IdempotencyKey, x.UserId, x.Endpoint }).IsUnique().AreNullsDistinct(false);
        builder.Entity<IdempotencyRecord>().HasIndex(x => x.ExpiresAt);
        builder.Entity<CostSettings>().HasIndex(x => new { x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        builder.Entity<ExchangeRate>().HasIndex(x => new { x.RateDate, x.BaseCurrency, x.QuoteCurrency }).IsUnique().HasFilter("\"IsActive\" = TRUE");
        builder.Entity<ExchangeRate>().HasIndex(x => x.IsActive);
        builder.Entity<WorkOrderCostSnapshot>().HasIndex(x => x.SnapshotNumber).IsUnique();
        builder.Entity<WorkOrderCostSnapshot>().HasIndex(x => new { x.WorkOrderId, x.SnapshotDate });
        builder.Entity<WorkOrderCostSnapshot>().HasIndex(x => new { x.IsFinal, x.CalculationType, x.ReportingCurrency });
        builder.Entity<WorkOrderCostSnapshot>().HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkOrderCostLine>().HasIndex(x => new { x.WorkOrderCostSnapshotId, x.CostCategory });
        builder.Entity<WorkOrderCostLine>().HasOne(x => x.WorkOrderCostSnapshot).WithMany(x => x.Lines).HasForeignKey(x => x.WorkOrderCostSnapshotId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProfitabilitySettings>().HasIndex(x => new { x.IsActive, x.EffectiveFrom, x.EffectiveTo });
        builder.Entity<CustomerReceivable>().HasIndex(x => x.ReceivableNumber).IsUnique();
        builder.Entity<CustomerReceivable>().HasIndex(x => x.OrderId).IsUnique().HasFilter("\"OrderId\" IS NOT NULL AND \"IsCancelled\" = FALSE");
        builder.Entity<CustomerReceivable>().HasIndex(x => new { x.CustomerId, x.Currency, x.Status, x.DueDate });
        builder.Entity<CustomerReceivable>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerReceivable>().HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerCollection>().HasIndex(x => x.CollectionNumber).IsUnique();
        builder.Entity<CustomerCollection>().HasIndex(x => new { x.CustomerId, x.Currency, x.Status, x.CollectionDate });
        builder.Entity<CustomerCollection>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CollectionAllocation>().HasIndex(x => new { x.CustomerCollectionId, x.CustomerReceivableId });
        builder.Entity<CollectionAllocation>().HasOne(x => x.CustomerCollection).WithMany(x => x.Allocations).HasForeignKey(x => x.CustomerCollectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CollectionAllocation>().HasOne(x => x.CustomerReceivable).WithMany(x => x.Allocations).HasForeignKey(x => x.CustomerReceivableId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerLedgerEntry>().HasIndex(x => x.EntryNumber).IsUnique();
        builder.Entity<CustomerLedgerEntry>().HasIndex(x => new { x.SourceType, x.SourceId, x.EntryType }).IsUnique();
        builder.Entity<CustomerLedgerEntry>().HasIndex(x => new { x.CustomerId, x.Currency, x.TransactionDate });
        builder.Entity<CustomerLedgerEntry>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FinancialAccount>().HasIndex(x=>x.AccountCode).IsUnique();
        builder.Entity<FinancialAccount>().HasIndex(x=>x.Iban).IsUnique().HasFilter("\"Iban\" IS NOT NULL AND \"IsActive\" = TRUE");
        builder.Entity<FinancialAccount>().HasIndex(x=>new{x.AccountType,x.Currency,x.IsActive});
        builder.Entity<FinancialTransaction>().HasIndex(x=>x.TransactionNumber).IsUnique();
        builder.Entity<FinancialTransaction>().HasIndex(x=>new{x.FinancialAccountId,x.Currency,x.TransactionDate});
        builder.Entity<FinancialTransaction>().HasIndex(x=>new{x.SourceType,x.SourceId,x.Direction}).IsUnique().HasFilter("\"SourceId\" IS NOT NULL AND \"IsReversed\" = FALSE AND \"SourceType\" <> 'AccountTransfer'");
        builder.Entity<FinancialTransaction>().HasOne(x=>x.FinancialAccount).WithMany().HasForeignKey(x=>x.FinancialAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerCheque>().HasIndex(x=>x.PortfolioNumber).IsUnique();
        builder.Entity<CustomerCheque>().HasIndex(x=>new{x.CustomerId,x.Currency,x.Status,x.DueDate});
        builder.Entity<CustomerCheque>().HasIndex(x=>x.CustomerCollectionId).IsUnique().HasFilter("\"CustomerCollectionId\" IS NOT NULL");
        builder.Entity<CustomerCheque>().HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ChequeEvent>().HasIndex(x=>new{x.CustomerChequeId,x.EventDate});
        builder.Entity<ChequeEvent>().HasOne(x=>x.CustomerCheque).WithMany(x=>x.Events).HasForeignKey(x=>x.CustomerChequeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierPayable>().HasIndex(x=>x.PayableNumber).IsUnique();builder.Entity<SupplierPayable>().HasIndex(x=>x.PurchaseOrderId).IsUnique().HasFilter("\"PurchaseOrderId\" IS NOT NULL AND \"IsCancelled\"=FALSE");builder.Entity<SupplierPayable>().HasIndex(x=>new{x.SupplierId,x.Currency,x.Status,x.DueDate});builder.Entity<SupplierPayable>().HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierPayment>().HasIndex(x=>x.PaymentNumber).IsUnique();builder.Entity<SupplierPayment>().HasIndex(x=>new{x.SupplierId,x.Currency,x.Status,x.PaymentDate});builder.Entity<SupplierPayment>().HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierPaymentAllocation>().HasIndex(x=>new{x.SupplierPaymentId,x.SupplierPayableId});builder.Entity<SupplierPaymentAllocation>().HasOne(x=>x.SupplierPayment).WithMany(x=>x.Allocations).HasForeignKey(x=>x.SupplierPaymentId).OnDelete(DeleteBehavior.Restrict);builder.Entity<SupplierPaymentAllocation>().HasOne(x=>x.SupplierPayable).WithMany(x=>x.Allocations).HasForeignKey(x=>x.SupplierPayableId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierLedgerEntry>().HasIndex(x=>x.EntryNumber).IsUnique();builder.Entity<SupplierLedgerEntry>().HasIndex(x=>new{x.SourceType,x.SourceId,x.EntryType}).IsUnique();builder.Entity<SupplierLedgerEntry>().HasIndex(x=>new{x.SupplierId,x.Currency,x.TransactionDate});builder.Entity<SupplierLedgerEntry>().HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ChequeEndorsement>().HasIndex(x=>x.EndorsementNumber).IsUnique();builder.Entity<ChequeEndorsement>().HasIndex(x=>x.CustomerChequeId).IsUnique().HasFilter("\"Status\"='Active'");builder.Entity<ChequeEndorsement>().HasOne(x=>x.CustomerCheque).WithMany().HasForeignKey(x=>x.CustomerChequeId).OnDelete(DeleteBehavior.Restrict);builder.Entity<ChequeEndorsement>().HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MaintenanceAsset>().HasIndex(x=>x.AssetCode).IsUnique();builder.Entity<MaintenanceAsset>().HasIndex(x=>x.MachineId).IsUnique().HasFilter("\"MachineId\" IS NOT NULL AND \"IsActive\"=TRUE");builder.Entity<MaintenanceAsset>().HasIndex(x=>x.InjectionStationId).IsUnique().HasFilter("\"InjectionStationId\" IS NOT NULL AND \"IsActive\"=TRUE");builder.Entity<MaintenanceAsset>().HasIndex(x=>x.CuttingMachineId).IsUnique().HasFilter("\"CuttingMachineId\" IS NOT NULL AND \"IsActive\"=TRUE");builder.Entity<MaintenanceAsset>().HasIndex(x=>x.MoldId).IsUnique().HasFilter("\"MoldId\" IS NOT NULL AND \"IsActive\"=TRUE");
        builder.Entity<MaintenanceRequest>().HasIndex(x=>x.RequestNumber).IsUnique();builder.Entity<MaintenanceRequest>().HasIndex(x=>new{x.MaintenanceAssetId,x.Status});builder.Entity<MaintenanceRequest>().HasOne(x=>x.MaintenanceAsset).WithMany().HasForeignKey(x=>x.MaintenanceAssetId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceRequest>().HasOne(x=>x.RelatedDowntime).WithMany().HasForeignKey(x=>x.RelatedDowntimeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MaintenanceWorkOrder>().HasIndex(x=>x.MaintenanceWorkOrderNumber).IsUnique();builder.Entity<MaintenanceWorkOrder>().HasIndex(x=>new{x.PreventiveMaintenancePlanId,x.PlannedStart}).IsUnique().HasFilter("\"PreventiveMaintenancePlanId\" IS NOT NULL AND \"Status\" <> 'Cancelled'");builder.Entity<MaintenanceWorkOrder>().HasIndex(x=>new{x.MaintenanceRequestId,x.Status});builder.Entity<MaintenanceWorkOrder>().HasOne(x=>x.MaintenanceAsset).WithMany().HasForeignKey(x=>x.MaintenanceAssetId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceWorkOrder>().HasOne(x=>x.MaintenanceRequest).WithMany().HasForeignKey(x=>x.MaintenanceRequestId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceWorkOrder>().HasOne(x=>x.Downtime).WithMany().HasForeignKey(x=>x.DowntimeId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceWorkOrder>().HasOne(x=>x.ExternalServiceSupplier).WithMany().HasForeignKey(x=>x.ExternalServiceSupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PreventiveMaintenancePlan>().HasIndex(x=>x.PlanCode).IsUnique();builder.Entity<PreventiveMaintenancePlan>().HasIndex(x=>new{x.IsActive,x.NextDueDate});builder.Entity<PreventiveMaintenancePlan>().HasOne(x=>x.MaintenanceAsset).WithMany().HasForeignKey(x=>x.MaintenanceAssetId).OnDelete(DeleteBehavior.Restrict);builder.Entity<PreventiveMaintenancePlan>().HasOne(x=>x.ChecklistTemplate).WithMany().HasForeignKey(x=>x.ChecklistTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MaintenanceChecklistTemplateItem>().HasIndex(x=>new{x.MaintenanceChecklistTemplateId,x.Sequence}).IsUnique();builder.Entity<MaintenanceChecklistTemplateItem>().HasOne(x=>x.Template).WithMany(x=>x.Items).HasForeignKey(x=>x.MaintenanceChecklistTemplateId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceChecklistResult>().HasIndex(x=>new{x.MaintenanceWorkOrderId,x.TemplateItemId}).IsUnique();builder.Entity<MaintenanceChecklistResult>().HasOne(x=>x.MaintenanceWorkOrder).WithMany().HasForeignKey(x=>x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenanceChecklistResult>().HasOne(x=>x.TemplateItem).WithMany().HasForeignKey(x=>x.TemplateItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MaintenancePartUsage>().HasIndex(x=>new{x.MaintenanceWorkOrderId,x.Status});builder.Entity<MaintenancePartUsage>().HasOne(x=>x.MaintenanceWorkOrder).WithMany().HasForeignKey(x=>x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenancePartUsage>().HasOne(x=>x.StockItem).WithMany().HasForeignKey(x=>x.StockItemId).OnDelete(DeleteBehavior.Restrict);builder.Entity<MaintenancePartUsage>().HasOne(x=>x.Material).WithMany().HasForeignKey(x=>x.MaterialId).OnDelete(DeleteBehavior.Restrict);

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
