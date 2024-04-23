using Microsoft.EntityFrameworkCore;

using TerroristChecker.Domain.Abstractions;

namespace TerroristChecker.Persistence.Repositories;

internal abstract class Repository<TEntity>(ApplicationDbContext dbContext)
    where TEntity : Entity
{
    protected readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void Add(TEntity entity)
    {
        _dbContext.Add(entity);
    }
}
