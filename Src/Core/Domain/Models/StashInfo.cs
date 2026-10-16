namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a Git stash entry.
/// </summary>
public record StashInfo
{
    public int Index { get; init; }

    public string Message { get; init; } = string.Empty;

    public string Sha { get; init; } = string.Empty;

    public string Name => $"stash@{{{Index}}}";
}
