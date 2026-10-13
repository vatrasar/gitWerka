namespace GitWerka.Core.Domain.Models;

using System;
using System.Collections.Immutable;

/// <summary>
/// Domain model containing comprehensive details of a single Git commit.
/// </summary>
public record CommitDetail
{
    public string Sha { get; init; } = string.Empty;

    public string ShortSha { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string AuthorName { get; init; } = string.Empty;

    public string AuthorEmail { get; init; } = string.Empty;

    public DateTimeOffset AuthorDate { get; init; }

    public string CommitterName { get; init; } = string.Empty;

    public string CommitterEmail { get; init; } = string.Empty;

    public DateTimeOffset CommitterDate { get; init; }

    public ImmutableList<string> ParentShas { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<FileDiffSummary> ChangedFiles { get; init; } = ImmutableList<FileDiffSummary>.Empty;
}
