using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TerroristChecker.Api;
using TerroristChecker.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace TerroristsChecker.Application.IntegrationTests;

public class TestWebApplicationFactory: WebApplicationFactory<AssemblyHint>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16.3-alpine3.20")
        .WithVolumeMount("terroristchecker_database", "/var/lib/postgresql/data")
        .WithDatabase("terrorists")
        .WithUsername("admin")
        .WithPassword("123")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.2.5")
        .Build();

    private static readonly object _lock = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (_lock)
            return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //builder.UseEnvironment("IntegrationTests");

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options
                    .UseNpgsql(_postgreSqlContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention();
            });

            descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(RedisCacheOptions));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddStackExchangeRedisCache(options => options.Configuration = _redisContainer.GetConnectionString());
        });
    }
    public Task InitializeAsync()
    {
        return Task.WhenAll(_postgreSqlContainer.StartAsync(), _redisContainer.StartAsync());
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Task.WhenAll(_postgreSqlContainer.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
    }
}
