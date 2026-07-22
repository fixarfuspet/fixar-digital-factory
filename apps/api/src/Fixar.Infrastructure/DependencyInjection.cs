using System.Text;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Identity;
using Fixar.Infrastructure.Persistence;
using Fixar.Infrastructure.Persistence.Interceptors;
using Fixar.Infrastructure.Persistence.Repositories;
using Fixar.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Fixar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("The 'Jwt' configuration section is not configured.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException("Jwt:Secret must be configured (e.g. via the Jwt__Secret environment variable).");
        }

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            static void Roles(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder p, params string[] roles) => p.RequireRole(roles);
            var ceo = RoleNames.CEO;
            options.AddPolicy(AuthorizationPolicies.CanManageUsers, p => Roles(p, ceo));
            options.AddPolicy(AuthorizationPolicies.CanViewSystemHealth, p => Roles(p, ceo));
            options.AddPolicy(AuthorizationPolicies.CanManageCustomers, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.SalesManager));
            options.AddPolicy(AuthorizationPolicies.CanManageSalesOrders, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.SalesManager));
            options.AddPolicy(AuthorizationPolicies.CanViewQuotes, p => Roles(p, ceo, RoleNames.SalesManager, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanManageQuotes, p => Roles(p, ceo, RoleNames.SalesManager));
            options.AddPolicy(AuthorizationPolicies.CanManagePurchases, p => Roles(p, ceo, RoleNames.PurchasingManager, RoleNames.Purchasing));
            options.AddPolicy(AuthorizationPolicies.CanManageMaterials, p => Roles(p, ceo, RoleNames.PurchasingManager, RoleNames.Purchasing));
            options.AddPolicy(AuthorizationPolicies.CanManageLots, p => Roles(p, ceo, RoleNames.PurchasingManager, RoleNames.Purchasing, RoleNames.QualityManager, RoleNames.QualityInspector, RoleNames.QualityOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageContainers, p => Roles(p, ceo, RoleNames.WarehouseManager, RoleNames.WarehouseOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageWorkOrders, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.FactoryManager));
            options.AddPolicy(AuthorizationPolicies.CanPlanProduction, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.ProductionSupervisor));
            options.AddPolicy(AuthorizationPolicies.CanRecordProduction, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.ProductionSupervisor, RoleNames.ProductionOperator, RoleNames.InjectionOperator));
            options.AddPolicy(AuthorizationPolicies.CanRecordQuality, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.QualityManager, RoleNames.QualityInspector, RoleNames.QualityOperator));
            options.AddPolicy(AuthorizationPolicies.CanRecordCutting, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.CuttingOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageBoxes, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.CuttingOperator, RoleNames.WarehouseManager, RoleNames.WarehouseOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageWarehouse, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.WarehouseManager, RoleNames.WarehouseOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageShipments, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.WarehouseManager, RoleNames.WarehouseOperator));
            options.AddPolicy(AuthorizationPolicies.CanManageReservations, p => Roles(p, ceo, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanRecordConsumption, p => Roles(p, ceo, RoleNames.ProductionManager, RoleNames.ProductionOperator, RoleNames.InjectionOperator, RoleNames.QualityOperator));
            options.AddPolicy(AuthorizationPolicies.CanReverseConsumption, p => Roles(p, ceo, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanViewCosts, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewTraceability, p => p.RequireAuthenticatedUser());
            options.AddPolicy(AuthorizationPolicies.CanOverrideProductionRules, p => Roles(p, ceo, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanCalculateCosts, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanManageCostSettings, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanManageExchangeRates, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanFinalizeCosts, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanViewExecutiveDashboard, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewProfitability, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance, RoleNames.ProductionManager));
            options.AddPolicy(AuthorizationPolicies.CanManageProfitabilitySettings, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewCustomerFinance, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanManageReceivables, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanRecordCollections, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanAllocateCollections, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanReverseCollections, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewCustomerLedger, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanExportCustomerStatement, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewFinancialAccounts, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanManageFinancialAccounts, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewCashFlow, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanRecordFinancialTransactions, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanReverseFinancialTransactions, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewChequePortfolio, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanManageChequePortfolio, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanCollectCheque, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanBounceCheque, p => Roles(p, ceo, RoleNames.FinanceManager, RoleNames.Finance));
            options.AddPolicy(AuthorizationPolicies.CanViewSupplierFinance,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance,RoleNames.PurchasingManager,RoleNames.Purchasing));options.AddPolicy(AuthorizationPolicies.CanManageSupplierPayables,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance));options.AddPolicy(AuthorizationPolicies.CanRecordSupplierPayments,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance));options.AddPolicy(AuthorizationPolicies.CanAllocateSupplierPayments,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance));options.AddPolicy(AuthorizationPolicies.CanReverseSupplierPayments,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance));options.AddPolicy(AuthorizationPolicies.CanViewSupplierLedger,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance,RoleNames.PurchasingManager,RoleNames.Purchasing));options.AddPolicy(AuthorizationPolicies.CanManageChequeEndorsements,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance));options.AddPolicy(AuthorizationPolicies.CanViewPaymentForecast,p=>Roles(p,ceo,RoleNames.FinanceManager,RoleNames.Finance,RoleNames.PurchasingManager,RoleNames.Purchasing));
            var maintenance=new[]{ceo,RoleNames.ProductionManager,RoleNames.FactoryManager,RoleNames.MaintenanceManager,RoleNames.MaintenanceTechnician};var requesters=maintenance.Concat(new[]{RoleNames.InjectionOperator,RoleNames.CuttingOperator,RoleNames.QualityOperator}).ToArray();options.AddPolicy(AuthorizationPolicies.CanViewMaintenance,p=>Roles(p,requesters.Concat(new[]{RoleNames.FinanceManager,RoleNames.Finance,RoleNames.PurchasingManager,RoleNames.Purchasing}).ToArray()));options.AddPolicy(AuthorizationPolicies.CanCreateMaintenanceRequest,p=>Roles(p,requesters));options.AddPolicy(AuthorizationPolicies.CanManageMaintenanceRequests,p=>Roles(p,maintenance));options.AddPolicy(AuthorizationPolicies.CanManageMaintenanceWorkOrders,p=>Roles(p,maintenance));options.AddPolicy(AuthorizationPolicies.CanManagePreventiveMaintenance,p=>Roles(p,ceo,RoleNames.ProductionManager,RoleNames.MaintenanceManager));options.AddPolicy(AuthorizationPolicies.CanManageMaintenanceChecklists,p=>Roles(p,maintenance));options.AddPolicy(AuthorizationPolicies.CanUseMaintenanceParts,p=>Roles(p,ceo,RoleNames.ProductionManager,RoleNames.MaintenanceManager,RoleNames.MaintenanceTechnician,RoleNames.WarehouseManager,RoleNames.WarehouseOperator));options.AddPolicy(AuthorizationPolicies.CanVerifyMaintenance,p=>Roles(p,ceo,RoleNames.ProductionManager,RoleNames.MaintenanceManager));options.AddPolicy(AuthorizationPolicies.CanViewMaintenanceCosts,p=>Roles(p,ceo,RoleNames.ProductionManager,RoleNames.MaintenanceManager,RoleNames.FinanceManager,RoleNames.Finance));
        });
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWorkOrderCostService, WorkOrderCostService>();
        services.AddScoped<IProfitabilityReportService, ProfitabilityReportService>();
        services.AddScoped<IFinancialCashFlowService, FinancialCashFlowService>();
        services.AddScoped<IAtomicFinanceService, AtomicFinanceService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "ready" });

        return services;
    }
}
