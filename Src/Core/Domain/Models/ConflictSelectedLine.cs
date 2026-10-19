namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a line from Ours (A) or Theirs (B) that has been selected into the conflict output.
/// </summary>
public record ConflictSelectedLine
{
    public bool IsOurs { get; init; }

    public int LineIndex { get; init; }

    public string Text { get; init; } = string.Empty;
}
