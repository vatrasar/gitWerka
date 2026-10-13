namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model representing the ongoing repository operation status (such as merge or rebase).
/// </summary>
public record RepositoryOperationStatus
{
    public RepositoryOperationType CurrentOperation { get; init; } = RepositoryOperationType.None;

    public bool HasConflicts { get; init; }

    public int CurrentStep { get; init; }

    public int TotalSteps { get; init; }

    public string Message { get; init; } = string.Empty;
}
