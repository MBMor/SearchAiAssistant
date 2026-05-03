using SearchAiAssistant.Api.Extensions;
using SearchAiAssistant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationOptions();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Application = "Search & AI Assistant",
    Status = "Running"
}));

app.Run();

