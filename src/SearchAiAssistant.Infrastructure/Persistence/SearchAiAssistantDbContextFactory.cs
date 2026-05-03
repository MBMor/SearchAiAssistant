using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SearchAiAssistant.Infrastructure.Persistence;

public sealed class SearchAiAssistantDbContextFactory
    : IDesignTimeDbContextFactory<SearchAiAssistantDbContext>
{
    public SearchAiAssistantDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=search_ai_assistant;Username=search_ai_assistant;Password=temporaryPassword";

        var optionsBuilder = new DbContextOptionsBuilder<SearchAiAssistantDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new SearchAiAssistantDbContext(optionsBuilder.Options);
    }
}