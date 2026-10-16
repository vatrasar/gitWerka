namespace GitWerka.Core.Domain.Models;

using System.Collections.Immutable;

/// <summary>
/// Domain model representing a chunk within a conflicted file, which is either common context or a conflict hunk.
/// </summary>
public record ConflictFileChunk
{
    public bool IsConflict { get; init; }

    public int HunkIndex { get; init; }

    public ImmutableList<string> CommonLines { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> OursLines { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> TheirsLines { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> BaseLines { get; init; } = ImmutableList<string>.Empty;

    public bool IsOursSelected { get; init; }

    public bool IsTheirsSelected { get; init; }
}
