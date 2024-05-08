using MediatR;

using Microsoft.Extensions.Logging;

using TerroristChecker.Application.Abstractions.Cache;
using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Application.Behaviors;

internal sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse? cachedResult = await cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);

        string name = request.GetType().Name;
        if (cachedResult is not null)
        {
            logger.LogTrace("Cache hit for {Query}", name);

            return cachedResult;
        }

        logger.LogTrace("Cache miss for {Query}", name);

        TResponse result = await next();

        if (result.IsSuccess)
        {
            await cacheService.SetAsync(request.CacheKey, result, request.Expiration, cancellationToken);
        }

        return result;
    }
}
