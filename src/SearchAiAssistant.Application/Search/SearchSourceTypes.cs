namespace SearchAiAssistant.Application.Search;

public static class SearchSourceTypes
{
    public const string Employee = "employee";

    public const string Document = "document";

    public static bool IsSupported(string sourceType)
    {
        return sourceType.Equals(Employee, StringComparison.OrdinalIgnoreCase)
            || sourceType.Equals(Document, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string sourceType)
    {
        return sourceType.Trim().ToLowerInvariant();
    }
}