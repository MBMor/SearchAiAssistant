using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Employees;

namespace SearchAiAssistant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService,  EmployeeService>();

        return services;
    }
}
