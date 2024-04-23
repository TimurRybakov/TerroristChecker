using MediatR;

using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Application.Abstractions;

public interface ICommand : IRequest<Result>, IBaseCommand
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

public interface IBaseCommand
{
}
