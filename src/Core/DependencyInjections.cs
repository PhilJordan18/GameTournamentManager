using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class DependencyInjections
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services;
    }
}