using TerroristChecker.Application.Abstractions.Cqrs;
using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Entities;

namespace TerroristChecker.Application.Cqrs.Queries.GetTerrorists;

public sealed record GetTerroristsQuery : IQuery<List<Person>>;

internal sealed class GetTerroristsQueryHandler(IPersonRepository personRepository)
    : IQueryHandler<GetTerroristsQuery, List<Person>>
{
    public async Task<Result<List<Person>>> Handle(GetTerroristsQuery request, CancellationToken cancellationToken)
    {
        var terrorists = await personRepository.GetTerroristListAsync(cancellationToken);

        return terrorists;
    }
}
