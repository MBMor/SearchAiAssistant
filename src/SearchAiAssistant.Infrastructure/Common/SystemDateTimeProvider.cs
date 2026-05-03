using SearchAiAssistant.Application.Common.Abstractions;

namespace SearchAiAssistant.Infrastructure.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}