using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SearchAiAssistant.Application.Common.Exceptions;

namespace SearchAiAssistant.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request was cancelled by the client. TraceId: {TraceId}",
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);

        LogException(problemDetails.Status, exception, context);

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, JsonSerializerOptions);

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(exception),
            Detail = GetDetail(exception),
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        return problemDetails;
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,

            InvalidCredentialsException => StatusCodes.Status401Unauthorized,

            DuplicateEmailException => StatusCodes.Status409Conflict,
            DuplicateEmployeeEmailException => StatusCodes.Status409Conflict,

            InvalidOperationException => StatusCodes.Status503ServiceUnavailable,

            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ValidationException => "Validation failed",
            ArgumentException => "Validation failed",

            InvalidCredentialsException => "Invalid credentials",

            DuplicateEmailException => "Duplicate email",
            DuplicateEmployeeEmailException => "Duplicate employee email",

            InvalidOperationException => "Service unavailable",

            _ => "Unexpected error"
        };
    }

    private static string GetDetail(Exception exception)
    {
        return exception switch
        {
            ValidationException => exception.Message,
            ArgumentException => exception.Message,

            InvalidCredentialsException => exception.Message,

            DuplicateEmailException => exception.Message,
            DuplicateEmployeeEmailException => exception.Message,

            InvalidOperationException => "A dependent service could not process the request.",

            _ => "An unexpected error occurred while processing the request."
        };
    }

    private void LogException(
        int? statusCode,
        Exception exception,
        HttpContext context)
    {
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        var traceId = context.TraceIdentifier;

        if (statusCode is >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, TraceId: {TraceId}",
                method,
                path,
                statusCode,
                traceId);

            return;
        }

        _logger.LogWarning(
            exception,
            "Handled application exception occurred. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, TraceId: {TraceId}",
            method,
            path,
            statusCode,
            traceId);
    }
}