using MediatR;

using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
