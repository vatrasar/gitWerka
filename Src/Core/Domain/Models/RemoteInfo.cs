namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a configured remote Git repository.
/// </summary>
public record RemoteInfo
{
    public string Name { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string? PushUrl { get; init; }
}
