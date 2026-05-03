using SearchAiAssistant.Domain.Common;
using SearchAiAssistant.Domain.Enums;

namespace SearchAiAssistant.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
        : base(id)
    {
        Email = Guard.RequiredEmail(email, nameof(email));
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), maxLength: 500);
        Role = role;
        CreatedAt = createdAt;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), maxLength: 500);
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }
}