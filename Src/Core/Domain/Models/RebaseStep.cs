namespace GitWerka.Core.Domain.Models;

using System;
using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model representing a single step entry in an interactive rebase workflow.
/// </summary>
public record RebaseStep
{
    public RebaseStepAction Action { get; init; } = RebaseStepAction.Pick;

    public string CommitSha { get; init; } = string.Empty;

    public string ShortSha { get; init; } = string.Empty;

    public string OriginalMessage { get; init; } = string.Empty;

    public string NewMessage { get; init; } = string.Empty;

    public string AuthorName { get; init; } = string.Empty;

    public DateTimeOffset AuthorDate { get; init; }
}
