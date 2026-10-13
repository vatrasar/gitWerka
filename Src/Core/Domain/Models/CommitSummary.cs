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
}
