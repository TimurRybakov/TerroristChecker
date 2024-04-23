using MediatR;

using Serilog;

using Microsoft.AspNetCore.Mvc;

using TerroristChecker.Application;
using TerroristChecker.Application.Cqrs.Queries;
using TerroristChecker.Domain.Dice.Abstractions;
using TerroristChecker.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UsePersonCacheInitialization();

app.MapGet(
        "/terrorists/search", async (
            [FromQuery] string input,
            [FromQuery] DateTime? birthday,
            [FromQuery] int? yearOfBirth,
            [FromQuery] double? minCoefficient,
            [FromQuery] double? minAverageCoefficient,
            [FromQuery] int? count,
            IMediator mediator) =>
        {
            DateOnly? birthdayDateOnly = birthday is null ? null : DateOnly.FromDateTime(birthday.Value);
            var searchOptions = new SearchOptions(birthdayDateOnly, yearOfBirth, minCoefficient, minAverageCoefficient);
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

app.Run();
