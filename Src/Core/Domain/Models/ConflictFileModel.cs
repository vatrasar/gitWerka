namespace GitWerka.Core.Domain.Models;

using System.Collections.Immutable;

/// <summary>
/// Domain model representing a conflicted file with parsed chunks and Git metadata.
/// </summary>
public record ConflictFileModel
{
    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int TotalConflicts { get; init; }

    public string OursCommitSha { get; init; } = string.Empty;

    public string OursBranch { get; init; } = string.Empty;

    public string TheirsCommitSha { get; init; } = string.Empty;

    public string TheirsBranch { get; init; } = string.Empty;

    public ImmutableList<ConflictFileChunk> Chunks { get; init; } = ImmutableList<ConflictFileChunk>.Empty;
}
