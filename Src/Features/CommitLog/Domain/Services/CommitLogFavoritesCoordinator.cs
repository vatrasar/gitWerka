namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Service coordinating favorite repositories storage, grouping, and dock interactions for CommitLog.
/// </summary>
public class CommitLogFavoritesCoordinator
{
    private readonly IRecentRepositoriesRepository? _recentRepositoriesRepository;
    private readonly IGitService _gitService;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<CommitLogState> _getState;

    public CommitLogFavoritesCoordinator(
        IRecentRepositoriesRepository? recentRepositoriesRepository,
        IGitService gitService,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<CommitLogState> getState)
    {
        _recentRepositoriesRepository = recentRepositoriesRepository;
        _gitService = gitService;
        _updateState = updateState;
        _getState = getState;
    }

    public void UpdateNewFavoriteNameInput(string name) =>
        _updateState(s => s with { NewFavoriteNameInput = name ?? string.Empty });

    public void UpdateNewFavoriteGroupInput(string group) =>
        _updateState(s => s with { NewFavoriteGroupInput = group ?? string.Empty });

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
            var (groups, ungrouped) = BuildFavoriteGroupsAndUngrouped(favList, _getState().ExpandedFavoriteGroupNames);
            var groupNames = ExtractDistinctGroupNames(favList);

            _updateState(s => s with
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
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    public async Task InitializeFavoriteAndRecentStateAsync()
    {
        var state = _getState();
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(state.RepositoryPath))
        {
            return;
        }

        try
        {
            RecentRepository? existing = await _recentRepositoriesRepository.GetRepositoryByPathAsync(state.RepositoryPath);
            string folderName = new DirectoryInfo(state.RepositoryPath).Name;
            bool isFavorite = existing?.IsFavorite ?? false;
            string? customName = existing?.CustomName;
            string displayName = ResolveRepositoryDisplayName(isFavorite, customName, folderName);

            _updateState(s => s with { RepositoryName = displayName });

            await _recentRepositoriesRepository.AddOrUpdateRecentRepositoryAsync(new RecentRepository
            {
                Path = state.RepositoryPath,
                Name = folderName,
                LastOpenedAtUtc = DateTime.UtcNow,
                IsFavorite = isFavorite,
                CustomName = customName,
                GroupName = existing?.GroupName
            });

            await RefreshFavoritesDockDataAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    public void OpenFavoritesDock() =>
        _updateState(s => s with { IsFavoritesDockOpen = true });

    public void ToggleFavoritesDock() =>
        _updateState(s => s with { IsFavoritesDockOpen = !_getState().IsFavoritesDockOpen });

    public async Task ToggleFavoritesDockAsync()
    {
        bool nextOpen = !_getState().IsFavoritesDockOpen;
        if (nextOpen)
        {
            await RefreshFavoritesDockDataAsync();
        }

        _updateState(s => s with { IsFavoritesDockOpen = nextOpen });
    }

    public void CloseFavoritesDock() =>
        _updateState(s => s with { IsFavoritesDockOpen = false });

    public void ToggleFavoriteGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        var state = _getState();
        var expanded = state.ExpandedFavoriteGroupNames.Contains(groupName)
            ? state.ExpandedFavoriteGroupNames.Remove(groupName)
            : state.ExpandedFavoriteGroupNames.Add(groupName);

        var (groups, ungrouped) = BuildFavoriteGroupsAndUngrouped(state.FavoriteRepositories, expanded);
        _updateState(s => s with
        {
            ExpandedFavoriteGroupNames = expanded,
            FavoriteGroups = groups,
            UngroupedFavoriteRepositories = ungrouped
        });
    }

    public void ShowAddFavoriteModal(string? path = null)
    {
        var state = _getState();
        string targetPath = string.IsNullOrWhiteSpace(path) ? state.RepositoryPath : path;
        RecentRepository? existing = state.FavoriteRepositories.FirstOrDefault(r => string.Equals(r.Path, targetPath, StringComparison.OrdinalIgnoreCase));
        string defaultName = existing?.CustomName ?? (!string.IsNullOrWhiteSpace(existing?.Name) ? existing.Name : new DirectoryInfo(targetPath).Name);
        string defaultGroup = existing?.GroupName ?? string.Empty;

        _updateState(s => s with
        {
            IsAddFavoriteModalVisible = true,
            IsEditingExistingFavorite = existing != null,
            TargetFavoritePath = targetPath,
            NewFavoriteNameInput = defaultName,
            NewFavoriteGroupInput = defaultGroup
        });
    }

    public void HideAddFavoriteModal() =>
        _updateState(s => s with { IsAddFavoriteModalVisible = false });

    public async Task ConfirmAddFavoriteAsync()
    {
        var state = _getState();
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(state.TargetFavoritePath))
        {
            _updateState(s => s with { IsAddFavoriteModalVisible = false });
            return;
        }

        string rawName = state.NewFavoriteNameInput?.Trim() ?? string.Empty;
        string nameToUse = string.IsNullOrWhiteSpace(rawName)
            ? new DirectoryInfo(state.TargetFavoritePath).Name
            : rawName;
        string? groupToUse = string.IsNullOrWhiteSpace(state.NewFavoriteGroupInput)
            ? null
            : state.NewFavoriteGroupInput.Trim();

        await _recentRepositoriesRepository.SetFavoriteAsync(state.TargetFavoritePath, true, nameToUse, groupToUse);

        if (string.Equals(state.TargetFavoritePath, state.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            _updateState(s => s with { RepositoryName = nameToUse });
        }

        await RefreshFavoritesDockDataAsync();
        _updateState(s => s with { IsAddFavoriteModalVisible = false });
    }

    public Task SaveFavoriteAsync() => ConfirmAddFavoriteAsync();

    public Task RemoveFavoriteAsync(string path) => RemoveFromFavoritesAsync(path);

    public async Task RemoveFromFavoritesAsync(string path)
    {
        var state = _getState();
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await _recentRepositoriesRepository.SetFavoriteAsync(path, false, null, null);

        if (string.Equals(path, state.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            string fallbackName = new DirectoryInfo(state.RepositoryPath).Name;
            _updateState(s => s with { RepositoryName = fallbackName });
        }

        await RefreshFavoritesDockDataAsync();
    }

    public void OpenRepositoryFromDock(string path, Action<string> navigateAction)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _updateState(s => s with { IsFavoritesDockOpen = false });

        var state = _getState();
        if (string.Equals(path, state.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_gitService.IsValidRepository(path))
        {
            _updateState(s => s with { ErrorMessage = "Selected directory is not a valid Git repository." });
            return;
        }

        navigateAction(path);
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
        ImmutableHashSet<string> expandedGroups)
    {
        var groups = favorites
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .GroupBy(r => r.GroupName!.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FavoriteRepositoryGroup
            {
                Name = g.Key,
                Repositories = g.ToImmutableList(),
                IsExpanded = expandedGroups.Contains(g.Key)
            })
            .ToImmutableList();

        var ungrouped = favorites
            .Where(r => string.IsNullOrWhiteSpace(r.GroupName))
            .ToImmutableList();

        return (groups, ungrouped);
    }

    private static string ResolveRepositoryDisplayName(bool isFavorite, string? customName, string fallbackName)
    {
        if (isFavorite && !string.IsNullOrWhiteSpace(customName))
        {
            return customName;
        }

        return fallbackName;
    }
}
