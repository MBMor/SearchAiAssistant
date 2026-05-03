using Microsoft.EntityFrameworkCore;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Infrastructure.Persistence;

public sealed class SearchAiAssistantDbContext(DbContextOptions<SearchAiAssistantDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchAiAssistantDbContext).Assembly);
    }
}