using MediatR;

using Serilog;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

using TerroristChecker.Api.Middleware;
using TerroristChecker.Application;
using TerroristChecker.Application.Cqrs.Queries.GetTerrorists;
using TerroristChecker.Application.Cqrs.Queries.SearchTerrorists;
using TerroristChecker.Domain.Dice.Models;
using TerroristChecker.Persistence;
using TerroristChecker.InfrastructureServices;

IConfiguration prefetchedConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT_PROFILE_NAME")}.json", optional: true, reloadOnChange: false)
    .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(prefetchedConfig)
    .CreateBootstrapLogger();

try
{
    Run(args);
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed!");
    return -1;
}
finally
{
    Log.CloseAndFlush();
}

static void Run(string[] strings)
{
    var builder = WebApplication.CreateBuilder(strings);
    builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TerroristChecker API",
            Version = "v1",
            Description = "API provides person search in the terrorists lists"
        });
    });
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddCaching(builder.Configuration, builder.Environment.IsDevelopment());

    Log.Information("Building...");
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    //app.UseHttpsRedirection();
    app.UsePersonCacheInitialization();

    app.MapGet(
            "/terrorists/search", async (
                [FromQuery] string input,
                [FromQuery] DateTime? birthday,
                [FromQuery] int? yearOfBirth,
                [FromQuery] double? minCoefficient,
                [FromQuery] double? minAverageCoefficient,
                [FromQuery] bool? averageByInputCount,
                [FromQuery] int? count,
                IMediator mediator) =>
            {
                DateOnly? birthdayDateOnly = birthday is null ? null : DateOnly.FromDateTime(birthday.Value);
                var searchOptions = new SearchOptions(birthdayDateOnly, yearOfBirth, minCoefficient, minAverageCoefficient, averageByInputCount);
                var query = new SearchTerroristsQuery(input, count, searchOptions);

                var result  = await mediator.Send(query);

                return result.IsSuccess ? result.Value : null;
            })
        .WithName("SearchTerrorist")
        .WithOpenApi();

    app.MapGet(
            "/terrorists", async (IMediator mediator) =>
            {
                var query = new GetTerroristsQuery();

                var result  = await mediator.Send(query);

                return result.Value.Take(5).ToList();
            })
        .WithName("GetTerrorists")
        .WithOpenApi();

    Log.Information("Starting up...");
    app.Run();
}
