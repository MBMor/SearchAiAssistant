using SearchAiAssistant.Application.Abstractions.Persistence;

namespace SearchAiAssistant.Infrastructure.Persistence;

public sealed class UnitOfWork(SearchAiAssistantDbContext dbContext) : IUnitOfWork
{
    private readonly SearchAiAssistantDbContext _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}