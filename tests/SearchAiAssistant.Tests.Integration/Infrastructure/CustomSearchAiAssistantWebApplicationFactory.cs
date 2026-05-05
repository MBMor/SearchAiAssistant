using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SearchAiAssistant.Api;
using SearchAiAssistant.Infrastructure.Persistence;

namespace SearchAiAssistant.Tests.Integration.Infrastructure;

public sealed class CustomSearchAiAssistantWebApplicationFactory
    : WebApplicationFactory<ApiAssemblyMarker>
{
    private const string TestJwtIssuer = "SearchAiAssistant.Api.Tests";
    private const string TestJwtAudience = "SearchAiAssistant.Api.Tests.Client";
    private const string TestJwtSigningKey = "this-is-a-test-signing-key-with-32-plus-chars";

    private readonly string _postgresConnectionString;
    private readonly string _openSearchUri;
    private readonly string _searchIndexName;

    public CustomSearchAiAssistantWebApplicationFactory(
        string postgresConnectionString,
        string openSearchUri,
        string searchIndexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(openSearchUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchIndexName);

        _postgresConnectionString = postgresConnectionString;
        _openSearchUri = openSearchUri;
        _searchIndexName = searchIndexName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Postgres", _postgresConnectionString);

        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);
        builder.UseSetting("Jwt:SigningKey", TestJwtSigningKey);
        builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "60");

        builder.UseSetting("OpenSearch:Uri", _openSearchUri);
        builder.UseSetting("OpenSearch:IndexName", _searchIndexName);
        builder.UseSetting("OpenSearch:RequestTimeoutSeconds", "30");

        builder.UseSetting("Pagination:DefaultPageSize", "20");
        builder.UseSetting("Pagination:MaxPageSize", "100");

        builder.UseSetting("Search:DefaultSort", "relevance");
        builder.UseSetting("Search:EnableHighlighting", "true");

        builder.UseSetting("AiAssistant:Provider", "Mock");
        builder.UseSetting("AiAssistant:MaxRetrievedSources", "5");
        builder.UseSetting("AiAssistant:MinimumScore", "0.1");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgresConnectionString,

                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:AccessTokenExpirationMinutes"] = "60",

                ["OpenSearch:Uri"] = _openSearchUri,
                ["OpenSearch:IndexName"] = _searchIndexName,
                ["OpenSearch:RequestTimeoutSeconds"] = "30",

                ["Pagination:DefaultPageSize"] = "20",
                ["Pagination:MaxPageSize"] = "100",

                ["Search:DefaultSort"] = "relevance",
                ["Search:EnableHighlighting"] = "true",

                ["AiAssistant:Provider"] = "Mock",
                ["AiAssistant:MaxRetrievedSources"] = "5",
                ["AiAssistant:MinimumScore"] = "0.1"
            };

            configurationBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SearchAiAssistantDbContext>>();
            services.RemoveAll<SearchAiAssistantDbContext>();

            services.AddDbContext<SearchAiAssistantDbContext>(options =>
            {
                options.UseNpgsql(_postgresConnectionString);
            });

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.ValidIssuer = TestJwtIssuer;
                    options.TokenValidationParameters.ValidAudience = TestJwtAudience;
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey));
                });
        });
    }
}