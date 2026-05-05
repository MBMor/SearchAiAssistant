using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Infrastructure.Persistence;

namespace SearchAiAssistant.Tests.Integration.Infrastructure;

public static class IntegrationDatabaseHelper
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SearchAiAssistantDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public static async Task ResetAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SearchAiAssistantDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                search_documents,
                documents,
                employees,
                users
            RESTART IDENTITY CASCADE;
            """);
    }
}