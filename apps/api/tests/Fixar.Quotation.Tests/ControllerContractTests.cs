using System.Reflection;
using Fixar.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fixar.Quotation.Tests;

public sealed class ControllerContractTests
{
    private static readonly Type[] Controllers = typeof(AuthController).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
        .OrderBy(type => type.Name)
        .ToArray();

    [Fact]
    public void Every_controller_has_api_route_and_explicit_authorization_contract()
    {
        Assert.Equal(55, Controllers.Length);
        foreach (var controller in Controllers)
        {
            Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
            Assert.NotNull(controller.GetCustomAttribute<RouteAttribute>());

            var classSecured = controller.IsDefined(typeof(AuthorizeAttribute), true) || controller.IsDefined(typeof(AllowAnonymousAttribute), true);
            foreach (var action in Actions(controller))
            {
                var actionSecured = action.IsDefined(typeof(AuthorizeAttribute), true) || action.IsDefined(typeof(AllowAnonymousAttribute), true);
                Assert.True(classSecured || actionSecured, $"Yetki sözleşmesi eksik: {controller.Name}.{action.Name}");
            }
        }
    }

    [Fact]
    public void Controller_http_routes_are_unique_per_verb()
    {
        var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var controller in Controllers)
        {
            var prefix = controller.GetCustomAttribute<RouteAttribute>()!.Template;
            foreach (var action in Actions(controller))
            {
                foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    foreach (var verb in http.HttpMethods)
                    {
                        var key = $"{verb} {prefix}/{http.Template}".Replace("//", "/");
                        Assert.False(routes.TryGetValue(key, out var previous), $"Route çakışması: {key} ({previous}, {controller.Name}.{action.Name})");
                        routes[key] = $"{controller.Name}.{action.Name}";
                    }
                }
            }
        }
        Assert.True(routes.Count > 150, $"Beklenenden az endpoint keşfedildi: {routes.Count}");
    }

    [Fact]
    public void Anonymous_access_is_limited_to_authentication_and_development_seed()
    {
        var anonymous = Controllers.SelectMany(controller => Actions(controller)
            .Where(action => action.IsDefined(typeof(AllowAnonymousAttribute), true))
            .Select(action => $"{controller.Name}.{action.Name}"))
            .ToArray();

        Assert.All(anonymous, action => Assert.True(
            action.StartsWith(nameof(AuthController), StringComparison.Ordinal) ||
            action.StartsWith(nameof(DevSeedController), StringComparison.Ordinal),
            $"Beklenmeyen anonim endpoint: {action}"));
    }

    private static IEnumerable<MethodInfo> Actions(Type controller) => controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());
}
