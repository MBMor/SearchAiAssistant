using Microsoft.OpenApi;
using System.Reflection;

namespace SearchAiAssistant.Api.Extensions;

public static class SwaggerExtensions
{
    private const string ApiTitle = "Search & AI Assistant API";
    private const string ApiVersion = "v1";
    private const string JwtBearerSchemeId = "bearer";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiVersion, new OpenApiInfo
            {
                Title = ApiTitle,
                Version = ApiVersion,
                Description = """
                    Local-first ASP.NET Core Web API for employee management, document knowledge-base management,
                    OpenSearch-powered full-text search, JWT authentication, and a retrieval-based local AI assistant.
                    """,
                Contact = new OpenApiContact
                {
                    Name = "Search & AI Assistant Portfolio Project"
                }
            });

            options.CustomSchemaIds(type =>
                type.FullName?.Replace('+', '.') ?? type.Name);

            options.SupportNonNullableReferenceTypes();

            var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(
                    xmlFilePath,
                    includeControllerXmlComments: true);
            }

            options.AddSecurityDefinition(JwtBearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = """
                    JWT Bearer authentication.

                    Use POST /api/auth/register or POST /api/auth/login to get an accessToken.
                    In Swagger UI, click Authorize and paste only the raw JWT token.
                    Do not include the 'Bearer' prefix.
                    """
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerSchemeId, document)] = []
                });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                $"{ApiTitle} {ApiVersion}");

            options.RoutePrefix = "swagger";
            options.DocumentTitle = ApiTitle;
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnablePersistAuthorization();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });

        return app;
    }
}