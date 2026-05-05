using FluentAssertions;
using SearchAiAssistant.Application.Assistant;
using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Application.Search;
using SearchAiAssistant.Domain.Enums;
using SearchAiAssistant.Tests.Integration.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SearchAiAssistant.Tests.Integration;

public sealed class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Auth_RegisterLoginAndMe_ShouldWorkEndToEnd()
    {
        await using var factory = await SearchAiAssistantTestHost.CreateInitializedAsync();
        using var client = factory.CreateClient();

        await IntegrationDatabaseHelper.MigrateAsync(factory.Services);

        var email = $"admin-{Guid.NewGuid():N}@example.com";

        var registerResponse = await PostAsJsonAndReadAsync<RegisterUserRequest, AuthResponse>(
            client,
            "/api/auth/register",
            new RegisterUserRequest(
                Email: email,
                Password: "Password123!",
                Role: UserRole.Admin));

        registerResponse.UserId.Should().NotBeEmpty();
        registerResponse.Email.Should().Be(email);
        registerResponse.Role.Should().Be("Admin");
        registerResponse.AccessToken.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await PostAsJsonAndReadAsync<LoginRequest, AuthResponse>(
            client,
            "/api/auth/login",
            new LoginRequest(
                Email: email.ToUpperInvariant(),
                Password: "Password123!"));

        loginResponse.UserId.Should().Be(registerResponse.UserId);
        loginResponse.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResponse.AccessToken);

        var meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentUser = await ReadJsonAsync<CurrentUserResponse>(meResponse);

        currentUser.UserId.Should().Be(registerResponse.UserId);
        currentUser.Email.Should().Be(email);
        currentUser.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Documents_CreateSearchAndAssistant_ShouldWorkEndToEnd()
    {
        await using var factory = await SearchAiAssistantTestHost.CreateInitializedAsync();
        using var client = factory.CreateClient();

        await IntegrationDatabaseHelper.MigrateAsync(factory.Services);
        await AuthenticateAsAdminAsync(client);

        var createdDocument = await PostAsJsonAndReadAsync<CreateDocumentRequest, DocumentResponse>(
            client,
            "/api/documents",
            new CreateDocumentRequest(
                Title: "Employee Benefits Policy",
                Content: "Employees receive benefits including remote work, paid vacation, health insurance and learning budget.",
                Category: "HR Policy",
                Tags: ["benefits", "policy", "hr"]));

        createdDocument.Id.Should().NotBeEmpty();

        var searchResponse = await client.GetAsync("/api/search/documents?query=benefits");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await ReadJsonAsync<PagedResult<SearchResultItem>>(searchResponse);

        searchResult.Items.Should().Contain(item =>
            item.SourceId == createdDocument.Id &&
            item.SourceType == SearchSourceTypes.Document &&
            item.Title == "Employee Benefits Policy");

        var assistantResponse = await PostAsJsonAndReadAsync<AskAssistantRequest, AssistantResponse>(
            client,
            "/api/assistant/ask",
            new AskAssistantRequest(
                Question: "What benefits do employees have?",
                MaxSources: 5));

        assistantResponse.HasEnoughInformation.Should().BeTrue();
        assistantResponse.Sources.Should().Contain(source => source.SourceId == createdDocument.Id);
        assistantResponse.Answer.Should().Contain("Employee Benefits Policy");
    }

    [Fact]
    public async Task Employees_CreateAndSearch_ShouldWorkEndToEnd()
    {
        await using var factory = await SearchAiAssistantTestHost.CreateInitializedAsync();
        using var client = factory.CreateClient();

        await IntegrationDatabaseHelper.MigrateAsync(factory.Services);
        await AuthenticateAsAdminAsync(client);

        var createdEmployee = await PostAsJsonAndReadAsync<CreateEmployeeRequest, EmployeeResponse>(
            client,
            "/api/employees",
            new CreateEmployeeRequest(
                FirstName: "Anna",
                LastName: "Novak",
                Email: $"anna.novak-{Guid.NewGuid():N}@example.com",
                Department: "Engineering",
                JobTitle: "Backend Developer",
                Skills: ["C#", ".NET", "PostgreSQL"],
                Location: "Prague"));

        createdEmployee.Id.Should().NotBeEmpty();

        var searchResponse = await client.GetAsync("/api/search/employees?query=backend");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResult = await ReadJsonAsync<PagedResult<SearchResultItem>>(searchResponse);

        searchResult.Items.Should().Contain(item =>
            item.SourceId == createdEmployee.Id &&
            item.SourceType == SearchSourceTypes.Employee &&
            item.Title == "Anna Novak");
    }

    [Fact]
    public async Task Documents_CreateAsRegularUser_ShouldReturnForbidden()
    {
        await using var factory = await SearchAiAssistantTestHost.CreateInitializedAsync();
        using var client = factory.CreateClient();

        await IntegrationDatabaseHelper.MigrateAsync(factory.Services);
        await AuthenticateAsUserAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/documents",
            new CreateDocumentRequest(
                Title: "Restricted Document",
                Content: "This should not be created by regular users.",
                Category: "Security",
                Tags: ["security"]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var authResponse = await PostAsJsonAndReadAsync<RegisterUserRequest, AuthResponse>(
            client,
            "/api/auth/register",
            new RegisterUserRequest(
                Email: $"admin-{Guid.NewGuid():N}@example.com",
                Password: "Password123!",
                Role: UserRole.Admin));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authResponse.AccessToken);
    }

    private static async Task AuthenticateAsUserAsync(HttpClient client)
    {
        var authResponse = await PostAsJsonAndReadAsync<RegisterUserRequest, AuthResponse>(
            client,
            "/api/auth/register",
            new RegisterUserRequest(
                Email: $"user-{Guid.NewGuid():N}@example.com",
                Password: "Password123!",
                Role: UserRole.User));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authResponse.AccessToken);
    }

    private static async Task<TResponse> PostAsJsonAndReadAsync<TRequest, TResponse>(
        HttpClient client,
        string requestUri,
        TRequest request)
    {
        var response = await client.PostAsJsonAsync(requestUri, request);

        response.EnsureSuccessStatusCode();

        return await ReadJsonAsync<TResponse>(response);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<T>(
            content,
            JsonSerializerOptions);

        result.Should().NotBeNull();

        return result!;
    }
}