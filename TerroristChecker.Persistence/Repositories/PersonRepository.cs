using Microsoft.EntityFrameworkCore;

using TerroristChecker.Domain.Abstractions;
using TerroristChecker.Domain.Dice.Entities;

namespace TerroristChecker.Persistence.Repositories;

internal sealed class PersonRepository(ApplicationDbContext dbContext)
    : Repository<Person>(dbContext), IPersonRepository
{
    private DbSet<Person> Terrorists { get; set; }  = null!;

    public async Task<List<Person>> GetTerroristListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<Person>()
            .ToListAsync(cancellationToken);
    }
}
