using SearchAiAssistant.Api.Extensions;
using SearchAiAssistant.Application;
using SearchAiAssistant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");

builder.Services.AddApplicationOptions();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(postgresConnectionString);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Application = "Search & AI Assistant",
    Status = "Running"
}));

app.Run();

