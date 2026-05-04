using SearchAiAssistant.Application.Abstractions.Authentication;
using SearchAiAssistant.Application.Abstractions.Persistence;
using SearchAiAssistant.Application.Common.Abstractions;
using SearchAiAssistant.Application.Common.Exceptions;
using SearchAiAssistant.Domain.Entities;

namespace SearchAiAssistant.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IAuthService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AuthResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        ValidatePassword(request.Password);

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new DuplicateEmailException(email);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User(
            id: Guid.NewGuid(),
            email: email,
            passwordHash: passwordHash,
            role: request.Role,
            createdAt: _dateTimeProvider.UtcNow);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken) 
            ?? throw new InvalidCredentialsException();

        var passwordIsValid = _passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!passwordIsValid)
        {
            throw new InvalidCredentialsException();
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponse(
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.ToString(),
            AccessToken: accessToken);
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ValidationException("Email must be valid.");
        }

        return normalizedEmail;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length < MinimumPasswordLength)
        {
            throw new ValidationException(
                $"Password must be at least {MinimumPasswordLength} characters long.");
        }
    }
}