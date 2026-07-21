using System.Reflection;
using Fixar.API.Controllers;
using Fixar.Infrastructure;
using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class RoleAuthorizationTests
{
    [Theory]
    [InlineData(RoleNames.CEO, AuthorizationPolicies.CanManageSalesOrders, true)]
    [InlineData(RoleNames.ProductionManager, AuthorizationPolicies.CanManageSalesOrders, true)]
    [InlineData(RoleNames.InjectionOperator, AuthorizationPolicies.CanManageSalesOrders, false)]
    [InlineData(RoleNames.CuttingOperator, AuthorizationPolicies.CanManageSalesOrders, false)]
    [InlineData(RoleNames.WarehouseOperator, AuthorizationPolicies.CanManageSalesOrders, false)]
    [InlineData(RoleNames.InjectionOperator, AuthorizationPolicies.CanRecordProduction, true)]
    [InlineData(RoleNames.CuttingOperator, AuthorizationPolicies.CanRecordCutting, true)]
    [InlineData(RoleNames.WarehouseOperator, AuthorizationPolicies.CanManageWarehouse, true)]
    [InlineData(RoleNames.InjectionOperator, AuthorizationPolicies.CanViewCosts, false)]
    [InlineData(RoleNames.CuttingOperator, AuthorizationPolicies.CanViewProfitability, false)]
    [InlineData(RoleNames.WarehouseOperator, AuthorizationPolicies.CanViewExecutiveDashboard, false)]
    [InlineData(RoleNames.ProductionManager, AuthorizationPolicies.CanOverrideProductionRules, true)]
    [InlineData(RoleNames.WarehouseOperator, AuthorizationPolicies.CanOverrideProductionRules, false)]
    public async Task Role_matrix_matches_operational_boundaries(string role, string policyName, bool expected)
    {
        using var provider = Services().BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync(policyName);
        var allowedRoles = policy!.Requirements.OfType<RolesAuthorizationRequirement>().SelectMany(x => x.AllowedRoles);
        Assert.Equal(expected, allowedRoles.Contains(role));
    }

    [Theory]
    [InlineData(typeof(StationAssignmentsController), "CancelFire", AuthorizationPolicies.CanOverrideProductionRules)]
    [InlineData(typeof(StationAssignmentsController), "Finish", AuthorizationPolicies.CanPlanProduction)]
    [InlineData(typeof(ProductionBoxesController), "Update", AuthorizationPolicies.CanOverrideProductionRules)]
    [InlineData(typeof(ProductionBoxesController), "Cancel", AuthorizationPolicies.CanOverrideProductionRules)]
    public void Correction_actions_require_manager_policy(Type controller, string methodName, string expectedPolicy)
    {
        var method = controller.GetMethods().Single(x => x.Name == methodName);
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Equal(expectedPolicy, authorize.Policy);
    }

    private static ServiceCollection Services()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=role_policy_test;Username=test",
            ["Jwt:Secret"] = "TEST-only-secret-with-more-than-thirty-two-characters",
            ["Jwt:Issuer"] = "FixarOSTests",
            ["Jwt:Audience"] = "FixarOSTestClients"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services;
    }
}
