namespace GitWerka.Core.Domain.Enums;

/// <summary>
/// Action to perform on a commit step during an interactive rebase.
/// </summary>
public enum RebaseStepAction
{
    Pick,
    Reword,
    Edit,
    Squash,
    Fixup,
    Drop
}
