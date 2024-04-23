using MediatR;

using TerroristChecker.Application.Abstractions;
using TerroristChecker.Application.Cqrs.Queries;
using TerroristChecker.Application.Dice.Services;
using TerroristChecker.Domain;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Abstractions;

namespace TerroristChecker.Application.Cqrs.Commands;

public sealed class UpdatePersonCacheCommand : ICommand;

internal sealed class UpdatePersonCacheCommandHandler(
    IMediator mediator,
    IPersonCacheService personCacheService) : ICommandHandler<UpdatePersonCacheCommand>
{
    public async Task<Result> Handle(UpdatePersonCacheCommand request, CancellationToken cancellationToken)
    {
        var query = new GetTerroristsQuery();

        var terrorists = await mediator.Send(query, cancellationToken);

        if (!terrorists.IsSuccess)
        {
            return Result.Failure(TerroristErrors.NotAcquired);
        }

        personCacheService.Clear();

        foreach (var person in terrorists.Value)
        {
            personCacheService.Add(person.Id, person.FullName, person.Birthday);
        }

        return Result.Success();

    }
}
