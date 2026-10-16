namespace GitWerka.Core.Domain.Enums;

/// <summary>
/// Status of a changed file in Git index or working tree.
/// </summary>
public enum FileChangeStatus
{
    Untracked,
    Modified,
    Added,
    Deleted,
    Renamed,
    Copied,
    Ignored,
    Conflicted
}
