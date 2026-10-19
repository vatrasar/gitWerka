namespace GitWerka.Core.Domain.Models;

using System.Windows.Input;

/// <summary>
/// Domain model representing a rendered line in Panel A (Ours) or Panel B (Theirs) of the conflict resolver.
/// </summary>
public record ConflictLineDisplay
{
    public string LineNumber { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public bool IsConflict { get; init; }

    public bool IsHunkFirstLine { get; init; }

    public int HunkIndex { get; init; }
 
    public int LineIndex { get; init; }

    public bool IsSelected { get; init; }

    public bool IsOurs { get; init; }

    public bool IsConflictOurs => IsConflict && IsOurs;

    public bool IsConflictTheirs => IsConflict && !IsOurs;

    public bool? IsHunkChecked { get; init; }

    public string ActionTooltip { get; init; } = string.Empty;

    public string HunkTooltip { get; init; } = string.Empty;

    public ICommand? ToggleCommand { get; init; }

    public ICommand? ToggleHunkCommand { get; init; }
}
