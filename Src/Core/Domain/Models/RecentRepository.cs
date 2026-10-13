namespace GitWerka.Core.Domain.Models;

using System;

/// <summary>
/// Domain model representing a recent git repository.
/// </summary>
public record RecentRepository
{
    public string Path { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public DateTime LastOpenedAtUtc { get; init; }

    public bool IsPinned { get; init; }
}
