namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing the outcome of a Git merge operation.
/// </summary>
public record GitMergeResult
{
    public string Status { get; init; } = string.Empty;

    public bool HasConflicts { get; init; }

    public string? CommitSha { get; init; }

    public string Message { get; init; } = string.Empty;
}
