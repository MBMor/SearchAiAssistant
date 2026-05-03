using SearchAiAssistant.Api.Extensions;
using SearchAiAssistant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");

builder.Services.AddApplicationOptions();
builder.Services.AddInfrastructure(postgresConnectionString);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Application = "Search & AI Assistant",
    Status = "Running"
}));

app.Run();

