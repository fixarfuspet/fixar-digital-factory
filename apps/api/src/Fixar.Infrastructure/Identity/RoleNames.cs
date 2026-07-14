namespace Fixar.Infrastructure.Identity;

/// <summary>
/// Roles defined in docs/06_USER_ROLES.md. Seeded on startup so RBAC can be
/// wired to these role names before any business module exists.
/// </summary>
public static class RoleNames
{
    public const string CEO = "CEO";
    public const string FactoryManager = "Factory Manager";
    public const string ProductionManager = "Production Manager";
    public const string WarehouseManager = "Warehouse Manager";
    public const string PurchasingManager = "Purchasing Manager";
    public const string FinanceManager = "Finance Manager";
    public const string QualityManager = "Quality Manager";
    public const string MaintenanceManager = "Maintenance Manager";
    public const string HRManager = "HR Manager";
    public const string SalesManager = "Sales Manager";
    public const string ProductionSupervisor = "Production Supervisor";
    public const string WarehouseOperator = "Warehouse Operator";
    public const string ProductionOperator = "Production Operator";
    public const string QualityInspector = "Quality Inspector";
    public const string MaintenanceTechnician = "Maintenance Technician";
    public const string Guest = "Guest";
    public const string InjectionOperator = "InjectionOperator";
    public const string CuttingOperator = "CuttingOperator";
    public const string QualityOperator = "QualityOperator";
    public const string Purchasing = "Purchasing";
    public const string Finance = "Finance";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[]
    {
        CEO, FactoryManager, ProductionManager, WarehouseManager, PurchasingManager,
        FinanceManager, QualityManager, MaintenanceManager, HRManager, SalesManager,
        ProductionSupervisor, WarehouseOperator, ProductionOperator, QualityInspector,
        MaintenanceTechnician, Guest, InjectionOperator, CuttingOperator,
        QualityOperator, Purchasing, Finance, Viewer
    };
}
