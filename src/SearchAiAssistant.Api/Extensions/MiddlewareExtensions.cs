using SearchAiAssistant.Api.Middleware;

namespace SearchAiAssistant.Api.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        return app;
    }

    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();

        return app;
    }
}