using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjections
{
    public static IServiceCollection AddInfrastructures(this IServiceCollection services)
    {
        return services;
    }
}