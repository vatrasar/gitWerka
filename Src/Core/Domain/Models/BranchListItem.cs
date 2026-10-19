namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing an item in the branch selection list (all-branches option or concrete branch).
/// </summary>
public record BranchListItem
{
    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsAllBranches { get; init; }

    public bool IsRemote { get; init; }

    public bool IsCurrentHead { get; init; }

    public string TipSha { get; init; } = string.Empty;
}
