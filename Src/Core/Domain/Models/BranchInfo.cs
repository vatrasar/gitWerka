namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a Git branch reference.
/// </summary>
public record BranchInfo
{
    public string Name { get; init; } = string.Empty;

    public bool IsRemote { get; init; }

    public bool IsCurrentHead { get; init; }

    public string TipSha { get; init; } = string.Empty;
}
