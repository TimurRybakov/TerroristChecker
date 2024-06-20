using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TerroristChecker.Application.Abstractions.Cache;
using TerroristChecker.InfrastructureServices.Cache;

namespace TerroristChecker.InfrastructureServices;

public static class DependencyInjection
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        string connectionString = configuration.GetConnectionString("Cache") ??
                                    throw new Exception("Cache connection string is not defined in configuration file");
        services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);

        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }
}
