using SearchAiAssistant.Application.Common.Options;
using SearchAiAssistant.Infrastructure.Authentication;
using SearchAiAssistant.Infrastructure.Search.OpenSearch;

namespace SearchAiAssistant.Api.Extensions;

public static class OptionsRegistrationExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "JWT signing key is required.")
            .Validate(options => options.SigningKey.Length >= 32, "JWT signing key must be at least 32 characters.")
            .Validate(options => options.AccessTokenExpirationMinutes > 0, "JWT access token expiration must be greater than zero.")
            .ValidateOnStart();

        services
            .AddOptions<OpenSearchOptions>()
            .BindConfiguration(OpenSearchOptions.SectionName)
            .Validate(options => Uri.TryCreate(options.Uri, UriKind.Absolute, out _), "OpenSearch URI must be valid.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.IndexName), "OpenSearch index name is required.")
            .Validate(options => options.RequestTimeoutSeconds > 0, "OpenSearch request timeout must be greater than zero.")
            .ValidateOnStart();

        services
            .AddOptions<PaginationOptions>()
            .BindConfiguration(PaginationOptions.SectionName)
            .Validate(options => options.DefaultPageSize > 0, "Default page size must be greater than zero.")
            .Validate(options => options.MaxPageSize >= options.DefaultPageSize, "Max page size must be greater than or equal to default page size.")
            .ValidateOnStart();

        services
            .AddOptions<SearchOptions>()
            .BindConfiguration(SearchOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultSort), "Default search sort is required.")
            .ValidateOnStart();

        services
            .AddOptions<AiAssistantOptions>()
            .BindConfiguration(AiAssistantOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "AI assistant provider is required.")
            .Validate(options => options.MaxRetrievedSources > 0, "AI assistant max retrieved sources must be greater than zero.")
            .Validate(options => options.MinimumScore >= 0, "AI assistant minimum score cannot be negative.")
            .ValidateOnStart();

        return services;
    }
}