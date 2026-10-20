namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Immutable state record for the FavoritesDock smart component.
/// </summary>
public record FavoritesDockState
{
    public bool IsFavoritesDockOpen { get; init; }

    public string CurrentRepositoryPath { get; init; } = string.Empty;

    public string DefaultProjectsDirectory { get; init; } = string.Empty;

    public RecentRepository? RecentNonFavoriteRepository { get; init; }

    public ImmutableList<RecentRepository> FavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public bool IsAddFavoriteModalVisible { get; init; }

    public string TargetFavoritePath { get; init; } = string.Empty;

    public string NewFavoriteNameInput { get; init; } = string.Empty;

    public string NewFavoriteGroupInput { get; init; } = string.Empty;

    public bool IsEditingExistingFavorite { get; init; }

    public ImmutableHashSet<string> CollapsedFavoriteGroupNames { get; init; } = ImmutableHashSet<string>.Empty;

    public ImmutableList<FavoriteRepositoryGroup> FavoriteGroups { get; init; } = ImmutableList<FavoriteRepositoryGroup>.Empty;

    public ImmutableList<RecentRepository> UngroupedFavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<string> ExistingGroupNames { get; init; } = ImmutableList<string>.Empty;

    public string? ErrorMessage { get; init; }
}
