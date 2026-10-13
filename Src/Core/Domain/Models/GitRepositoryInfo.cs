namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model containing high-level summary information about an opened Git repository.
/// </summary>
public record GitRepositoryInfo
{
    public string Path { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CurrentBranch { get; init; } = string.Empty;

    public string HeadCommitSha { get; init; } = string.Empty;

    public string HeadCommitMessage { get; init; } = string.Empty;

    public bool IsHeadDetached { get; init; }

    public int AheadBy { get; init; }

    public int BehindBy { get; init; }
}
