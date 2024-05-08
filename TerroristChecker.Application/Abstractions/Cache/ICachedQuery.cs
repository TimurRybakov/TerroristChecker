using TerroristChecker.Application.Abstractions.Cqrs;

namespace TerroristChecker.Application.Abstractions.Cache;

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery
{
    string ICachedQuery.CacheKey => CacheKeyGenerator<IQuery<TResponse>>.Generate(this);

    TimeSpan? ICachedQuery.Expiration => null;
}

public interface ICachedQuery
{
    string CacheKey { get; }

    TimeSpan? Expiration { get; }
};
