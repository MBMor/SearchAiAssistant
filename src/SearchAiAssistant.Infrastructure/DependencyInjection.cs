using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Infrastructure.Common;
using SearchAiAssistant.Infrastructure.Persistence;

namespace SearchAiAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string postgresConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);

        services.AddDbContext<SearchAiAssistantDbContext>(options =>
        {
            options.UseNpgsql(postgresConnectionString);    
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
