namespace SearchAiAssistant.Tests.Integration.Infrastructure;

public static class SearchAiAssistantTestHost
{
    public static async Task<CustomSearchAiAssistantWebApplicationFactory> CreateInitializedAsync()
    {
        var factory = new CustomSearchAiAssistantWebApplicationFactory();

        await factory.InitializeAsync();

        return factory;
    }
}