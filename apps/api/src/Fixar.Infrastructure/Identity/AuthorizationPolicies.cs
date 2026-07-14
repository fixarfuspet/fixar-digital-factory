namespace Fixar.Infrastructure.Identity;

public static class AuthorizationPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanManageCustomers = nameof(CanManageCustomers);
    public const string CanManageSalesOrders = nameof(CanManageSalesOrders);
    public const string CanManagePurchases = nameof(CanManagePurchases);
    public const string CanManageMaterials = nameof(CanManageMaterials);
    public const string CanManageLots = nameof(CanManageLots);
    public const string CanManageContainers = nameof(CanManageContainers);
    public const string CanManageWorkOrders = nameof(CanManageWorkOrders);
    public const string CanPlanProduction = nameof(CanPlanProduction);
    public const string CanRecordProduction = nameof(CanRecordProduction);
    public const string CanRecordQuality = nameof(CanRecordQuality);
    public const string CanRecordCutting = nameof(CanRecordCutting);
    public const string CanManageBoxes = nameof(CanManageBoxes);
    public const string CanManageWarehouse = nameof(CanManageWarehouse);
    public const string CanManageShipments = nameof(CanManageShipments);
    public const string CanManageReservations = nameof(CanManageReservations);
    public const string CanRecordConsumption = nameof(CanRecordConsumption);
    public const string CanReverseConsumption = nameof(CanReverseConsumption);
    public const string CanViewCosts = nameof(CanViewCosts);
    public const string CanViewTraceability = nameof(CanViewTraceability);
    public const string CanOverrideProductionRules = nameof(CanOverrideProductionRules);
    public const string CanCalculateCosts = nameof(CanCalculateCosts);
    public const string CanManageCostSettings = nameof(CanManageCostSettings);
    public const string CanManageExchangeRates = nameof(CanManageExchangeRates);
    public const string CanFinalizeCosts = nameof(CanFinalizeCosts);
}
