namespace GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Immutable state record for the RepositoryManager screen.
/// </summary>
public record RepositoryManagerState
{
    public bool IsLoading { get; init; }

    public string SearchQuery { get; init; } = string.Empty;

    public ImmutableList<RecentRepository> AllRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public ImmutableList<RecentRepository> FilteredRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public bool IsCloneModalVisible { get; init; }

    public string CloneSourceUrl { get; init; } = string.Empty;

    public string CloneTargetDirectory { get; init; } = string.Empty;

    public bool IsInitModalVisible { get; init; }

    public string InitDirectoryPath { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public bool IsOperationRunning { get; init; }
}
