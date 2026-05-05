using SearchAiAssistant.Application.Auth;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Domain.Entities;
using SearchAiAssistant.Domain.Enums;
using SearchAiAssistant.Tests.Unit.TestDoubles;
using Xunit;

namespace SearchAiAssistant.Tests.Unit.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUserAndReturnToken()
    {
        var userRepository = new InMemoryUserRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(userRepository, unitOfWork);

        var response = await service.RegisterAsync(new RegisterUserRequest(
            Email: " ADMIN@EXAMPLE.COM ",
            Password: "Password123!",
            Role: UserRole.Admin));

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal("admin@example.com", response.Email);
        Assert.Equal("Admin", response.Role);
        Assert.Equal($"token-for:{response.UserId}", response.AccessToken);
        Assert.Single(userRepository.Users);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowDuplicateEmailException()
    {
        var userRepository = new InMemoryUserRepository();

        userRepository.AddExisting(new User(
            id: Guid.NewGuid(),
            email: "admin@example.com",
            passwordHash: "hashed:Password123!",
            role: UserRole.Admin,
            createdAt: DateTimeOffset.UtcNow));

        var service = CreateService(userRepository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            service.RegisterAsync(new RegisterUserRequest(
                Email: "ADMIN@EXAMPLE.COM",
                Password: "Password123!",
                Role: UserRole.User)));
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ShouldThrowValidationException()
    {
        var service = CreateService(
            new InMemoryUserRepository(),
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterAsync(new RegisterUserRequest(
                Email: "user@example.com",
                Password: "short",
                Role: UserRole.User)));

        Assert.Contains("Password must be at least", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        var userRepository = new InMemoryUserRepository();

        var user = new User(
            id: Guid.NewGuid(),
            email: "user@example.com",
            passwordHash: "hashed:Password123!",
            role: UserRole.User,
            createdAt: DateTimeOffset.UtcNow);

        userRepository.AddExisting(user);

        var service = CreateService(userRepository, new FakeUnitOfWork());

        var response = await service.LoginAsync(new LoginRequest(
            Email: "USER@EXAMPLE.COM",
            Password: "Password123!"));

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal("user@example.com", response.Email);
        Assert.Equal("User", response.Role);
        Assert.Equal($"token-for:{user.Id}", response.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowInvalidCredentialsException()
    {
        var userRepository = new InMemoryUserRepository();

        userRepository.AddExisting(new User(
            id: Guid.NewGuid(),
            email: "user@example.com",
            passwordHash: "hashed:Password123!",
            role: UserRole.User,
            createdAt: DateTimeOffset.UtcNow));

        var service = CreateService(userRepository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(new LoginRequest(
                Email: "user@example.com",
                Password: "WrongPassword123!")));
    }

    private static AuthService CreateService(
        InMemoryUserRepository userRepository,
        FakeUnitOfWork unitOfWork)
    {
        return new AuthService(
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FixedDateTimeProvider(DateTimeOffset.Parse("2026-05-05T10:00:00Z")),
            unitOfWork);
    }
}