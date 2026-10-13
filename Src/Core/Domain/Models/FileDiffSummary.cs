namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model representing a single file changed in a commit or working tree.
/// </summary>
public record FileDiffSummary
{
    public string Path { get; init; } = string.Empty;

    public string? OldPath { get; init; }

    public FileChangeStatus Status { get; init; }

    public int LinesAdded { get; init; }

    public int LinesDeleted { get; init; }
}
