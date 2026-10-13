namespace GitWerka.Core.Domain.Enums;

/// <summary>
/// Type of a line in a Git diff hunk.
/// </summary>
public enum DiffLineType
{
    Context,
    Addition,
    Deletion,
    HunkHeader
}
