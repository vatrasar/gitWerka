namespace GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Immutable state record for the ConflictResolver 3-panel screen.
/// </summary>
public record ConflictResolverState
{
    public string RepositoryPath { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int TotalConflicts { get; init; }

    public int ActiveConflictIndex { get; init; } = 1;

    public string OursCommitSha { get; init; } = string.Empty;

    public string OursBranch { get; init; } = string.Empty;

    public string TheirsCommitSha { get; init; } = string.Empty;

    public string TheirsBranch { get; init; } = string.Empty;

    public ImmutableList<ConflictFileChunk> Chunks { get; init; } = ImmutableList<ConflictFileChunk>.Empty;

    public ImmutableList<ConflictLineDisplay> OursDisplayLines { get; init; } = ImmutableList<ConflictLineDisplay>.Empty;

    public ImmutableList<ConflictLineDisplay> TheirsDisplayLines { get; init; } = ImmutableList<ConflictLineDisplay>.Empty;

    public ImmutableList<ConflictOutputLineDisplay> OutputDisplayLines { get; init; } = ImmutableList<ConflictOutputLineDisplay>.Empty;

    public string MergedOutputText { get; init; } = string.Empty;

    public bool IsAllOursChecked { get; init; }

    public bool IsAllTheirsChecked { get; init; }

    public bool IsSaving { get; init; }

    public string? ErrorMessage { get; init; }

    public string? SuccessMessage { get; init; }
}
