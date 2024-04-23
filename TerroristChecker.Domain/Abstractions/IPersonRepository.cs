using TerroristChecker.Domain.Dice.Entities;

namespace TerroristChecker.Domain.Abstractions;

public interface IPersonRepository
{
    Task<List<Person>> GetTerroristListAsync(CancellationToken cancellationToken = default);
}
