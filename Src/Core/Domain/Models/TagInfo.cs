namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a Git tag reference.
/// </summary>
public record TagInfo
{
    public string Name { get; init; } = string.Empty;

    public string TargetSha { get; init; } = string.Empty;

    public string? Message { get; init; }

    public bool IsAnnotated { get; init; }
}
