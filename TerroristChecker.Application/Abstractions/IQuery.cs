using MediatR;

using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
