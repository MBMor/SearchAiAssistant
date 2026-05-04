using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Application.Auth;

namespace SearchAiAssistant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
