namespace SearchAiAssistant.Application.Abstractions.Persistence;

internal interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
