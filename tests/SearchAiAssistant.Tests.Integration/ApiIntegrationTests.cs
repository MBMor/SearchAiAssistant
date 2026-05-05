using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SearchAiAssistant.Application.Assistant;
using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Common.Pagination;
using SearchAiAssistant.Application.Documents;
using SearchAiAssistant.Application.Employees;
using SearchAiAssistant.Application.Search;
using SearchAiAssistant.Domain.Enums;
using SearchAiAssistant.Tests.Integration.Infrastructure;

namespace SearchAiAssistant.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class ApiIntegrationTests(SearchAiAssistantIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SearchAiAssistantIntegrationFixture _fixture = fixture;

    [Fact]
    public async Task Auth_RegisterLoginAndMe_ShouldWorkEndToEnd()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

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
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

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
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

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
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

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

    [Fact]
    public async Task Auth_RegisterDuplicateEmail_ShouldReturnConflict()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

        var email = $"duplicate-{Guid.NewGuid():N}@example.com";

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterUserRequest(
                Email: email,
                Password: "Password123!",
                Role: UserRole.User));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterUserRequest(
                Email: email.ToUpperInvariant(),
                Password: "Password123!",
                Role: UserRole.User));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Auth_LoginWithInvalidPassword_ShouldReturnUnauthorized()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

        var email = $"login-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterUserRequest(
                Email: email,
                Password: "Password123!",
                Role: UserRole.User));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                Email: email,
                Password: "WrongPassword123!"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Documents_Delete_ShouldRemoveDocumentFromSearch()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

        await AuthenticateAsAdminAsync(client);

        var createdDocument = await PostAsJsonAndReadAsync<CreateDocumentRequest, DocumentResponse>(
            client,
            "/api/documents",
            new CreateDocumentRequest(
                Title: "Delete Me Policy",
                Content: "This document should disappear from search after delete.",
                Category: "Test Policy",
                Tags: ["delete-me"]));

        var searchBeforeDeleteResponse = await client.GetAsync("/api/search/documents?query=delete-me");

        searchBeforeDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchBeforeDelete = await ReadJsonAsync<PagedResult<SearchResultItem>>(searchBeforeDeleteResponse);

        searchBeforeDelete.Items.Should().Contain(item => item.SourceId == createdDocument.Id);

        var deleteResponse = await client.DeleteAsync($"/api/documents/{createdDocument.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var searchAfterDeleteResponse = await client.GetAsync("/api/search/documents?query=delete-me");

        searchAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchAfterDelete = await ReadJsonAsync<PagedResult<SearchResultItem>>(searchAfterDeleteResponse);

        searchAfterDelete.Items.Should().NotContain(item => item.SourceId == createdDocument.Id);
    }

    [Fact]
    public async Task Assistant_UnknownQuestion_ShouldReturnNotEnoughInformation()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

        await AuthenticateAsUserAsync(client);

        var response = await PostAsJsonAndReadAsync<AskAssistantRequest, AssistantResponse>(
            client,
            "/api/assistant/ask",
            new AskAssistantRequest(
                Question: "What is the company submarine parking policy?",
                MaxSources: 5));

        response.HasEnoughInformation.Should().BeFalse();
        response.Sources.Should().BeEmpty();
        response.Answer.Should().Contain("not have enough information");
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        await _fixture.ResetAsync();

        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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