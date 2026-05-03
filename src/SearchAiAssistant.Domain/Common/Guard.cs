namespace SearchAiAssistant.Domain.Common;

public static class Guard
{
    public static string Required(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot be longer than {maxLength} characters.",
                parameterName);
        }

        return trimmedValue;
    }

    public static string RequiredEmail(string? value, string parameterName, int maxLength = 320)
    {
        var email = Required(value, parameterName, maxLength).ToLowerInvariant();

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException($"{parameterName} must be a valid email address.", parameterName);
        }

        return email;
    }

    public static List<string> NormalizeStringList(
        IEnumerable<string>? values,
        string parameterName,
        int maxItemLength = 100)
    {
        if (values is null)
        {
            return [];
        }

        var normalizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var value in normalizedValues)
        {
            if (value.Length > maxItemLength)
            {
                throw new ArgumentException(
                    $"{parameterName} items cannot be longer than {maxItemLength} characters.",
                    parameterName);
            }
        }

        return normalizedValues;
    }
}