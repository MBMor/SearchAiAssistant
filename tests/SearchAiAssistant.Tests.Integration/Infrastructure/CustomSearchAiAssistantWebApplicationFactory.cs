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
using SearchAiAssistant.Infrastructure.Authentication;
using SearchAiAssistant.Infrastructure.Persistence;
using SearchAiAssistant.Infrastructure.Search.OpenSearch;
using System.Text;
using Testcontainers.OpenSearch;
using Testcontainers.PostgreSql;
using Xunit;

namespace SearchAiAssistant.Tests.Integration.Infrastructure;

public sealed class CustomSearchAiAssistantWebApplicationFactory
    : WebApplicationFactory<ApiAssemblyMarker>, IAsyncLifetime
{
    private const string PostgreSqlImage = "postgres:16.2";
    private const string OpenSearchImage = "opensearchproject/opensearch:3.6.0";

    private const string TestJwtIssuer = "SearchAiAssistant.Api.Tests";
    private const string TestJwtAudience = "SearchAiAssistant.Api.Tests.Client";
    private const string TestJwtSigningKey = "this-is-a-test-signing-key-with-32-plus-chars";

    private readonly PostgreSqlContainer _postgresContainer;
    private readonly OpenSearchContainer _openSearchContainer;

    private string? _postgresConnectionString;
    private string? _openSearchUri;

    public CustomSearchAiAssistantWebApplicationFactory()
    {
        SearchIndexName = $"search-ai-assistant-tests-{Guid.NewGuid():N}";

        _postgresContainer = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("search_ai_assistant_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        _openSearchContainer = new OpenSearchBuilder(OpenSearchImage)
            .WithSecurityEnabled(false)
            .WithCleanUp(true)
            .Build();
    }

    public string SearchIndexName { get; }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _openSearchContainer.StartAsync();

        _postgresConnectionString = _postgresContainer.GetConnectionString();
        _openSearchUri = _openSearchContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Postgres", RequirePostgresConnectionString());

        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);
        builder.UseSetting("Jwt:SigningKey", TestJwtSigningKey);
        builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "60");

        builder.UseSetting("OpenSearch:Uri", RequireOpenSearchUri());
        builder.UseSetting("OpenSearch:IndexName", SearchIndexName);
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
                ["ConnectionStrings:Postgres"] = RequirePostgresConnectionString(),

                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:AccessTokenExpirationMinutes"] = "60",

                ["OpenSearch:Uri"] = RequireOpenSearchUri(),
                ["OpenSearch:IndexName"] = SearchIndexName,
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
                options.UseNpgsql(RequirePostgresConnectionString());
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

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        await _openSearchContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private string RequirePostgresConnectionString()
    {
        return _postgresConnectionString
            ?? throw new InvalidOperationException(
                "PostgreSQL test container must be initialized before creating the test host.");
    }

    private string RequireOpenSearchUri()
    {
        return _openSearchUri
            ?? throw new InvalidOperationException(
                "OpenSearch test container must be initialized before creating the test host.");
    }
}