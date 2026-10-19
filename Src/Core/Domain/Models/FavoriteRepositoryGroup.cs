namespace GitWerka.Core.Domain.Models;

using System.Collections.Immutable;

/// <summary>
/// Domain model representing a named group of favorite repositories.
/// </summary>
public record FavoriteRepositoryGroup
{
    public string Name { get; init; } = string.Empty;

    public ImmutableList<RecentRepository> Repositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public bool IsExpanded { get; init; } = true;

    public int Count => Repositories.Count;
}
