using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using TerroristChecker.Application.Behaviors;
using TerroristChecker.Application.Cqrs.Commands;
using TerroristChecker.Application.Dice.Services;
using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddSingleton<IWordStorageService, WordStorageService>(
            serviceProvider => ActivatorUtilities.CreateInstance<WordStorageService>(serviceProvider, 80 * 3));
        services.AddSingleton<IPersonCacheService, PersonCacheService>(
            serviceProvider => ActivatorUtilities.CreateInstance<PersonCacheService>(serviceProvider, 80));

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
