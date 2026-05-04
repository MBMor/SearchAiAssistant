using System.Security.Cryptography;
using SearchAiAssistant.Application.Abstractions.Authentication;

namespace SearchAiAssistant.Infrastructure.Authentication;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeInBytes = 16;
    private const int KeySizeInBytes = 32;
    private const int Iterations = 600_000;
    private const string FormatPrefix = "PBKDF2-SHA256";

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySizeInBytes);

        return string.Join(
            ':',
            FormatPrefix,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(':');

        if (parts.Length != 4 || parts[0] != FormatPrefix)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        if (iterations < 600_000 || iterations > 1_000_000)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);

            expectedHash = Convert.FromBase64String(parts[3]);

            if (salt.Length != SaltSizeInBytes || expectedHash.Length != KeySizeInBytes)
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}