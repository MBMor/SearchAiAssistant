using Microsoft.Extensions.DependencyInjection;
using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Documents;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Application.Indexing;

namespace SearchAiAssistant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService,  EmployeeService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IIndexManagementService, IndexManagementService>();

        return services;
    }
}
