using Microsoft.EntityFrameworkCore;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(SearchAiAssistantDbContext dbContext) : IUserRepository
{
    private readonly SearchAiAssistantDbContext _dbContext = dbContext;

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }
}