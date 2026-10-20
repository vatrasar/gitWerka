namespace GitWerka.Core.Domain.Models;

using System;
using System.Collections.Immutable;

/// <summary>
/// Domain model representing a commit with graph topology data for visualization.
/// </summary>
public record CommitSummary
{
    public string Sha { get; init; } = string.Empty;

    public string ShortSha { get; init; } = string.Empty;

    public string MessageShort { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string AuthorName { get; init; } = string.Empty;

    public string AuthorEmail { get; init; } = string.Empty;

    public DateTimeOffset AuthorDate { get; init; }

    public ImmutableList<string> ParentShas { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> BranchNames { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> TagNames { get; init; } = ImmutableList<string>.Empty;

    public int Lane { get; init; }

    public ImmutableList<int> PassingLanes { get; init; } = ImmutableList<int>.Empty;

    public ImmutableList<int> MergingLanes { get; init; } = ImmutableList<int>.Empty;

    public ImmutableList<int> ForkingOffLanes { get; init; } = ImmutableList<int>.Empty;

    public bool IsWip { get; init; }

    public bool IsHead { get; init; }

    public bool IsHeadDetached { get; init; }

    public string? HeadBranchName { get; init; }

    public string HeadLabel => "HEAD";

    public string HeadToolTip => IsHeadDetached
        ? $"HEAD (detached at {ShortSha})"
        : (string.IsNullOrEmpty(HeadBranchName) ? "HEAD" : $"HEAD -> {HeadBranchName}");

    public int UncommittedFilesCount { get; init; }

    public double GraphWidth { get; init; } = 80.0;

    public bool HasBranches => BranchNames.Count > 0;

    public bool HasTags => TagNames.Count > 0;

    public bool HasBadges => IsWip || IsHead || BranchNames.Count > 0 || TagNames.Count > 0;
}
