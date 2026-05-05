namespace SearchAiAssistant.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<SearchAiAssistantIntegrationFixture>
{
    public const string Name = "SearchAiAssistant integration tests";
}