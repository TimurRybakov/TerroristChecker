using System.Diagnostics;
using System.Globalization;

using Humanizer;

using MediatR;

using Microsoft.Extensions.Logging;

using TerroristChecker.Application.Abstractions;
using TerroristChecker.Application.Abstractions.Cqrs;

namespace TerroristChecker.Application.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandOrQuery
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = request.GetType().Name;

        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Executing command or query {Command}", name);

                var process = Process.GetCurrentProcess();
                var stopWatch = new Stopwatch();

                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                stopWatch.Start();

                var result = await next();

                stopWatch.Stop();

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                var cpuUsagePercentage = Math.Round(cpuUsageTotal * 100, 4);

                logger.LogDebug(
                    "Command or query {Command} processed successfully (CPU: {CPU}%, {Elapsed})",
                    name, cpuUsagePercentage, stopWatch.Elapsed.Humanize(culture: CultureInfo.InvariantCulture));

                return result;
            }
            else
            {
                return await next();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Command or query {Command} processing failed", name);

            throw;
        }
    }
}
