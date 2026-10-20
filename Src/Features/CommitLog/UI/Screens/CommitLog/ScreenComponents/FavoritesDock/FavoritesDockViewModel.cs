namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Smart component view model managing favorites dock state and favorite repository operations.
/// </summary>
public partial class FavoritesDockViewModel : ViewModelBase<FavoritesDockState>
{
    private readonly IRecentRepositoriesRepository? _recentRepositoriesRepository;
    private readonly IGitService _gitService;
    private readonly Subject<string> _repositorySelectedSubject = new();

    public IObservable<string> RepositorySelected => _repositorySelectedSubject;

    public FavoritesDockViewModel(
        IRecentRepositoriesRepository? recentRepositoriesRepository,
        IGitService gitService,
        string initialRepositoryPath = "")
        : base(new FavoritesDockState { CurrentRepositoryPath = initialRepositoryPath })
    {
        _recentRepositoriesRepository = recentRepositoriesRepository;
        _gitService = gitService;
    }

    /// <summary>
    /// Updates the active repository path in state.
    /// Usages: CommitLogViewModel on repository change.
    /// </summary>
    /// <param name="path">Current repository path.</param>
    public void SetCurrentRepositoryPath(string path)
    {
        UpdateState(s => s with { CurrentRepositoryPath = path ?? string.Empty });
    }

    /// <summary>
    /// Updates the default projects directory in state.
    /// Usages: CommitLogComponentCoordinator state propagation.
    /// </summary>
    /// <param name="directory">Default projects directory path.</param>
    public void SetDefaultProjectsDirectory(string directory)
    {
        UpdateState(s => s with { DefaultProjectsDirectory = directory ?? string.Empty });
    }

    /// <summary>
    /// Updates the custom repository name input for the Add to Favorites modal.
    /// Usages: FavoritesDockView favorite name text box.
    /// </summary>
    /// <param name="name">New custom repository name input.</param>
    public void UpdateNewFavoriteNameInput(string name)
    {
        UpdateState(s => s with { NewFavoriteNameInput = name ?? string.Empty });
    }

    /// <summary>
    /// Updates the target group name for the repository being added or edited in favorites.
    /// Usages: FavoritesDockView favorite group text box.
    /// </summary>
    /// <param name="group">New group name input.</param>
    public void UpdateNewFavoriteGroupInput(string group)
    {
        UpdateState(s => s with { NewFavoriteGroupInput = group ?? string.Empty });
    }

    /// <summary>
    /// Refreshes favorite repositories and recent non-favorite repository from storage.
    /// Usages: CommitLogViewModel, FavoritesDockView on opening drawer.
    /// </summary>
    /// <returns>Task tracking async refresh.</returns>
    public async Task RefreshFavoritesDockDataAsync()
    {
        if (_recentRepositoriesRepository == null)
        {
            return;
        }

        try
        {
            var favorites = await _recentRepositoriesRepository.GetFavoriteRepositoriesAsync();
            var recentNonFav = await _recentRepositoriesRepository.GetMostRecentNonFavoriteRepositoryAsync();
            var favList = favorites.ToImmutableList();
            var (groups, ungrouped) = BuildFavoriteGroupsAndUngrouped(favList, State.CollapsedFavoriteGroupNames);
            var groupNames = ExtractDistinctGroupNames(favList);

            UpdateState(s => s with
            {
                FavoriteRepositories = favList,
                FavoriteGroups = groups,
                UngroupedFavoriteRepositories = ungrouped,
                ExistingGroupNames = groupNames,
                RecentNonFavoriteRepository = recentNonFav
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ToggleFavoritesDock()
    {
        bool nextOpen = !State.IsFavoritesDockOpen;
        UpdateState(s => s with { IsFavoritesDockOpen = nextOpen });

        if (nextOpen)
        {
            await RefreshFavoritesDockDataAsync();
        }
    }

    [ReactiveCommand]
    private void CloseFavoritesDock()
    {
        UpdateState(s => s with { IsFavoritesDockOpen = false });
    }

    [ReactiveCommand]
    private void ToggleFavoriteGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        var collapsed = State.CollapsedFavoriteGroupNames.Contains(groupName)
            ? State.CollapsedFavoriteGroupNames.Remove(groupName)
            : State.CollapsedFavoriteGroupNames.Add(groupName);

        var (groups, ungrouped) = BuildFavoriteGroupsAndUngrouped(State.FavoriteRepositories, collapsed);
        UpdateState(s => s with
        {
            CollapsedFavoriteGroupNames = collapsed,
            FavoriteGroups = groups,
            UngroupedFavoriteRepositories = ungrouped
        });
    }

    [ReactiveCommand]
    private void ShowAddFavoriteModal(string? path = null)
    {
        string targetPath = string.IsNullOrWhiteSpace(path) ? State.CurrentRepositoryPath : path;
        RecentRepository? existing = State.FavoriteRepositories.FirstOrDefault(r => string.Equals(r.Path, targetPath, StringComparison.OrdinalIgnoreCase));
        string defaultName = existing?.CustomName ?? (!string.IsNullOrWhiteSpace(existing?.Name) ? existing.Name : new DirectoryInfo(targetPath).Name);
        string defaultGroup = existing?.GroupName ?? string.Empty;

        UpdateState(s => s with
        {
            IsAddFavoriteModalVisible = true,
            IsEditingExistingFavorite = existing != null,
            TargetFavoritePath = targetPath,
            NewFavoriteNameInput = defaultName,
            NewFavoriteGroupInput = defaultGroup
        });
    }

    [ReactiveCommand]
    private void ShowEditFavoriteModal(string path)
    {
        ShowAddFavoriteModal(path);
    }

    [ReactiveCommand]
    private void HideAddFavoriteModal()
    {
        UpdateState(s => s with { IsAddFavoriteModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ConfirmAddFavorite()
    {
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(State.TargetFavoritePath))
        {
            UpdateState(s => s with { IsAddFavoriteModalVisible = false });
            return;
        }

        string rawName = State.NewFavoriteNameInput?.Trim() ?? string.Empty;
        string nameToUse = string.IsNullOrWhiteSpace(rawName)
            ? new DirectoryInfo(State.TargetFavoritePath).Name
            : rawName;
        string? groupToUse = string.IsNullOrWhiteSpace(State.NewFavoriteGroupInput)
            ? null
            : State.NewFavoriteGroupInput.Trim();

        await _recentRepositoriesRepository.SetFavoriteAsync(State.TargetFavoritePath, true, nameToUse, groupToUse);
        await RefreshFavoritesDockDataAsync();
        UpdateState(s => s with { IsAddFavoriteModalVisible = false });
    }

    [ReactiveCommand]
    private async Task RemoveFromFavorites(string path)
    {
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await _recentRepositoriesRepository.SetFavoriteAsync(path, false, null, null);
        await RefreshFavoritesDockDataAsync();
    }

    [ReactiveCommand]
    private void OpenRepository(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        UpdateState(s => s with { IsFavoritesDockOpen = false });
        if (!_gitService.IsValidRepository(path))
        {
            UpdateState(s => s with { ErrorMessage = "Selected directory is not a valid Git repository." });
            return;
        }

        _repositorySelectedSubject.OnNext(path);
    }

    private static ImmutableList<string> ExtractDistinctGroupNames(ImmutableList<RecentRepository> favorites)
    {
        return favorites
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .Select(r => r.GroupName!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
    }

    private static (ImmutableList<FavoriteRepositoryGroup> Groups, ImmutableList<RecentRepository> Ungrouped) BuildFavoriteGroupsAndUngrouped(
        ImmutableList<RecentRepository> favorites,
        ImmutableHashSet<string> collapsedGroups)
    {
        var groups = favorites
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .GroupBy(r => r.GroupName!.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FavoriteRepositoryGroup
            {
                Name = g.Key,
                Repositories = g.ToImmutableList(),
                IsExpanded = !collapsedGroups.Contains(g.Key)
            })
            .ToImmutableList();

        var ungrouped = favorites
            .Where(r => string.IsNullOrWhiteSpace(r.GroupName))
            .ToImmutableList();

        return (groups, ungrouped);
    }
}
