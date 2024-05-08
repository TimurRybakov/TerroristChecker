using MediatR;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using TerroristChecker.Api;
using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Persistence;

namespace TerroristsChecker.Application.IntegrationTests;

[SetUpFixture]
public class Testing
{
    private static readonly WebApplicationFactory<AssemblyHint> Factory = new();

    [OneTimeSetUp]
    public static void RunBeforeAnyTests()
    {
    }

    [OneTimeTearDown]
    public static void RunAfterAllTests()
    {
        Factory.Dispose();
    }

    public static async Task AddAsync<TEntity>(TEntity entity) where TEntity : class
    {
        using var scope = Factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.AddAsync(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> request)
    {
        using var scope = Factory.Services.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(request);
    }

    public static async Task<Result<TResponse>> SendAsync<TResponse>(IQuery<TResponse> request)
    {
        using var scope = Factory.Services.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(request);
    }
}
