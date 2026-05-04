using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Application.Abstractions.Authentication;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Infrastructure.Authentication;
using SearchAiAssistant.Infrastructure.Common;
using SearchAiAssistant.Infrastructure.Persistence;
using SearchAiAssistant.Infrastructure.Persistence.Repositories;

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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
