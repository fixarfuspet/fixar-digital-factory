using System.Reflection;
using Fixar.API.Controllers;
using Fixar.Infrastructure;
using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class AuthenticationSecurityTests
{
    [Fact]
    public void Authentication_endpoints_are_rate_limited_and_logout_allows_expired_access_tokens()
    {
        var login = Action(nameof(AuthController.Login));
        var refresh = Action(nameof(AuthController.RefreshToken));
        var logout = Action(nameof(AuthController.Logout));

        Assert.Equal("authentication", login.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Equal("authentication", refresh.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Equal("authentication", logout.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.NotNull(logout.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Refresh_tokens_are_one_way_hashed_before_persistence()
    {
        const string rawToken = "TEST-refresh-token-that-must-not-be-stored";
        var method = typeof(AuthService).GetMethod("HashRefreshToken", BindingFlags.NonPublic | BindingFlags.Static);
        var hashed = Assert.IsType<string>(method?.Invoke(null, [rawToken]));

        Assert.StartsWith("sha256:", hashed);
        Assert.DoesNotContain(rawToken, hashed);
        Assert.Equal(hashed, method?.Invoke(null, [rawToken]));
    }

    [Fact]
    public void Identity_password_and_lockout_policy_meets_security_baseline()
    {
        using var provider = Services().BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.True(options.Password.RequiredLength >= 12);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.True(options.User.RequireUniqueEmail);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.True(options.Lockout.DefaultLockoutTimeSpan >= TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Operators_cannot_access_cost_or_profitability_policies()
    {
        using var provider = Services().BuildServiceProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var policyName in new[]
                 {
                     AuthorizationPolicies.CanViewCosts,
                     AuthorizationPolicies.CanViewProfitability,
                     AuthorizationPolicies.CanViewExecutiveDashboard
                 })
        {
            var policy = await policies.GetPolicyAsync(policyName);
            var roles = Assert.Single(policy!.Requirements.OfType<RolesAuthorizationRequirement>()).AllowedRoles;
            Assert.DoesNotContain(RoleNames.InjectionOperator, roles);
            Assert.DoesNotContain(RoleNames.CuttingOperator, roles);
            Assert.DoesNotContain(RoleNames.WarehouseOperator, roles);
        }
    }

    private static MethodInfo Action(string name) =>
        typeof(AuthController).GetMethod(name) ?? throw new InvalidOperationException($"Action bulunamadı: {name}");

    private static ServiceCollection Services()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=auth_policy_test;Username=test",
            ["Jwt:Secret"] = "TEST-only-secret-with-more-than-thirty-two-characters",
            ["Jwt:Issuer"] = "FixarOSTests",
            ["Jwt:Audience"] = "FixarOSTestClients",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services;
    }
}
