using SearchAiAssistant.Infrastructure.Authentication;
using Xunit;

namespace SearchAiAssistant.Tests.Unit.Infrastructure;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldNotReturnPlainTextPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.HashPassword("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.StartsWith("PBKDF2-SHA256:", hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.HashPassword("Password123!");

        var result = hasher.VerifyPassword("Password123!", hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.HashPassword("Password123!");

        var result = hasher.VerifyPassword("WrongPassword123!", hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("PBKDF2-SHA256:not-number:salt:hash")]
    [InlineData("OTHER:210000:salt:hash")]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse(string passwordHash)
    {
        var hasher = new Pbkdf2PasswordHasher();

        var result = hasher.VerifyPassword("Password123!", passwordHash);

        Assert.False(result);
    }
}