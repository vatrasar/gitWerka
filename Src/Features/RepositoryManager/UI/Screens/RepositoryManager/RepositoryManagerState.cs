namespace GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;

using System;
using System.Collections.Immutable;
using System.Linq;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.RepositoryManager.Domain.Enums;

/// <summary>
/// Immutable state record for the RepositoryManager screen.
/// </summary>
public record RepositoryManagerState
{
    public bool IsLoading { get; init; }

    public string SearchQuery { get; init; } = string.Empty;

    public RepositoryManagerTab SelectedTab { get; init; } = RepositoryManagerTab.Favorites;

    public ImmutableList<RecentRepository> AllRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<RecentRepository> FilteredRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<RecentRepository> FavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<RecentRepository> FilteredFavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<RecentRepository> DisplayedRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableHashSet<string> ExpandedFavoriteGroupNames { get; init; } = ImmutableHashSet<string>.Empty;

    public ImmutableList<FavoriteRepositoryGroup> FavoriteGroups { get; init; } = ImmutableList<FavoriteRepositoryGroup>.Empty;

    public ImmutableList<RecentRepository> UngroupedFavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public bool IsEditFavoriteModalVisible { get; init; }

    public string TargetFavoritePath { get; init; } = string.Empty;

    public string EditFavoriteNameInput { get; init; } = string.Empty;

    public string EditFavoriteGroupInput { get; init; } = string.Empty;

    public bool IsCloneModalVisible { get; init; }

    public string CloneSourceUrl { get; init; } = string.Empty;

    public string CloneTargetDirectory { get; init; } = string.Empty;

    public bool IsInitModalVisible { get; init; }

    public string InitDirectoryPath { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public bool IsNotGitRepositoryModalVisible { get; init; }

    public string NotGitRepositoryPath { get; init; } = string.Empty;

    public bool IsOperationRunning { get; init; }

    public bool IsSettingsModalVisible { get; init; }

    public string DefaultProjectsDirectory { get; init; } = string.Empty;

    public string DefaultProjectsDirectoryInput { get; init; } = string.Empty;

    public string AiApiKeyInput { get; init; } = string.Empty;

    public string AiModelInput { get; init; } = "google/gemini-2.5-flash";

    public string AiCommitStyleInput { get; init; } = "Conventional Commits: <type>(<scope>): <subject>";

    public string AiTokenLimitInput { get; init; } = "4000";

    public ImmutableList<string> ExistingGroupNames { get; init; } = ImmutableList<string>.Empty;
}
