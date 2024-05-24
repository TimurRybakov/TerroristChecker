using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TerroristChecker.Application.Behaviors;
using TerroristChecker.Application.Cqrs.Commands.UpdatePersonCache;
using TerroristChecker.Application.Dice.Services;
using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(
            serviceConfiguration =>
            {
                serviceConfiguration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                serviceConfiguration.AddOpenBehavior(typeof(LoggingBehavior<,>));
                serviceConfiguration.AddOpenBehavior(typeof(ValidationBehavior<,>));
                if (configuration.GetValue<bool>("Cache:Query"))
                {
                    serviceConfiguration.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
                }
            });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton<IWordStorageService, WordStorageService>(
            serviceProvider => ActivatorUtilities.CreateInstance<WordStorageService>(serviceProvider, 80 * 3));
        services.AddSingleton<IPersonSearcherService, PersonSearcherService>(
            serviceProvider => ActivatorUtilities.CreateInstance<PersonSearcherService>(serviceProvider, 80));

        return services;
    }

    public static IApplicationBuilder UsePersonCacheInitialization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

        using var scope = serviceScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdatePersonCacheCommand();

        mediator.Send(command)
            .GetAwaiter()
            .GetResult();

        return app;
    }
}
