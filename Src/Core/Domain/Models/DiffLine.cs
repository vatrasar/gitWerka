namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model representing a single line in a diff viewer.
/// </summary>
public record DiffLine
{
    public DiffLineType Type { get; init; }

    public string Content { get; init; } = string.Empty;

    public int? OldLineNumber { get; init; }

    public int? NewLineNumber { get; init; }
}
