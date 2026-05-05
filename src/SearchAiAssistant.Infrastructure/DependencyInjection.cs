using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SearchAiAssistant.Application.Abstractions.Authentication;
using SearchAiAssistant.Application.Abstractions.Indexing;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Abstractions.Search;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Infrastructure.Authentication;
using SearchAiAssistant.Infrastructure.Common;
using SearchAiAssistant.Infrastructure.Persistence;
using SearchAiAssistant.Infrastructure.Persistence.Repositories;
using SearchAiAssistant.Infrastructure.Search.OpenSearch;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient<ISearchService, OpenSearchSearchService>((serviceProvider, httpClient) =>
        {
            var openSearchOptions = serviceProvider
                .GetRequiredService<IOptions<OpenSearchOptions>>()
                .Value;

            httpClient.BaseAddress = new Uri(openSearchOptions.Uri.TrimEnd('/') + "/");
            httpClient.Timeout = TimeSpan.FromSeconds(openSearchOptions.RequestTimeoutSeconds);
        });

        services.AddHttpClient<IIndexingService, OpenSearchIndexingService>((serviceProvider, httpClient) =>
        {
            var openSearchOptions = serviceProvider
                .GetRequiredService<IOptions<OpenSearchOptions>>()
                .Value;

            httpClient.BaseAddress = new Uri(openSearchOptions.Uri.TrimEnd('/') + "/");
            httpClient.Timeout = TimeSpan.FromSeconds(openSearchOptions.RequestTimeoutSeconds);
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
