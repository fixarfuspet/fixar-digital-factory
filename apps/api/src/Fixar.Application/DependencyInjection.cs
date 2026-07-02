using Microsoft.Extensions.DependencyInjection;

namespace Fixar.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services. Currently there are no
    /// concrete services owned by this layer (business modules will add
    /// their own use cases here); the extension point is kept so
    /// Program.cs never has to change when that happens.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
