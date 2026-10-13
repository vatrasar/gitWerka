namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model representing an uncommitted file change in the working tree.
/// </summary>
public record WorkingTreeChange
{
    public string Path { get; init; } = string.Empty;

    public FileChangeStatus Status { get; init; }

    public bool IsStaged { get; init; }
}
