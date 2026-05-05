namespace SearchAiAssistant.Application.Indexing;

public sealed record RebuildIndexResponse(
    int EmployeesIndexed,
    int DocumentsIndexed)
{
    public int TotalIndexed => EmployeesIndexed + DocumentsIndexed;
}