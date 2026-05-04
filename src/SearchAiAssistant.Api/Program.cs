using SearchAiAssistant.Api.Extensions;
using SearchAiAssistant.Application;
using SearchAiAssistant.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");



builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddApplicationOptions();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(postgresConnectionString);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   


app.MapGet("/", () => Results.Ok(new
{
    Application = "Search & AI Assistant",
    Status = "Running"
}));

app.Run();

