namespace GitWerka.Core.Domain.Enums;

/// <summary>
/// Active long-running Git operation state of the repository.
/// </summary>
public enum RepositoryOperationType
{
    None,
    Merge,
    Rebase,
    CherryPick,
    Revert
}
