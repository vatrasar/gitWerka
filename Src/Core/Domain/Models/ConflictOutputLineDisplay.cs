namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a rendered line in the Output (Merged) panel of the conflict resolver.
/// </summary>
public record ConflictOutputLineDisplay
{
    public string LineNumber { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public bool IsConflict { get; init; }

    public bool IsFromOurs { get; init; }

    public bool IsFromTheirs { get; init; }
}
