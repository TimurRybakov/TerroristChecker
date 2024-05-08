using MediatR;

using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Application.Abstractions.Cqrs;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>, ICommandOrQuery
{
}
