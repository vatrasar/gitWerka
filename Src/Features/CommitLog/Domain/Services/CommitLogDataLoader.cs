namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;

/// <summary>
/// Service coordinating repository data loading, change detection, and state synchronization.
/// </summary>
public class CommitLogDataLoader
{
    private readonly IGitService _gitService;
    private readonly CommitLogSettingsCoordinator _settingsCoordinator;
    private readonly CommitLogHistoryActionCoordinator _historyActionCoordinator;
    private readonly CommitLogFavoritesCoordinator _favoritesCoordinator;
    private readonly Func<CommitLogState> _getState;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<WorkingTreePanelViewModel> _getWorkingTreePanel;
    private readonly Func<GitOperationModalsViewModel> _getGitModals;
    private readonly Func<FavoritesDockViewModel> _getFavoritesDock;

    private GitRepositoryStateSnapshot? _lastSnapshot;

    public CommitLogDataLoader(
        IGitService gitService,
        CommitLogSettingsCoordinator settingsCoordinator,
        CommitLogHistoryActionCoordinator historyActionCoordinator,
        CommitLogFavoritesCoordinator favoritesCoordinator,
        Func<CommitLogState> getState,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<WorkingTreePanelViewModel> getWorkingTreePanel,
        Func<GitOperationModalsViewModel> getGitModals,
        Func<FavoritesDockViewModel> getFavoritesDock)
    {
        _gitService = gitService;
        _settingsCoordinator = settingsCoordinator;
        _historyActionCoordinator = historyActionCoordinator;
        _favoritesCoordinator = favoritesCoordinator;
        _getState = getState;
        _updateState = updateState;
        _getWorkingTreePanel = getWorkingTreePanel;
        _getGitModals = getGitModals;
        _getFavoritesDock = getFavoritesDock;
    }

    /// <summary>
    /// Loads repository data, optionally checking for snapshot changes before executing state mutation.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="force">When true, forces reload regardless of snapshot equality; otherwise skips reload if unchanged.</param>
    public async Task RefreshRepositoryDataAsync(bool force = false)
    {
        var state = _getState();
        try
        {
            GitRepositoryStateSnapshot currentSnapshot = await _gitService.GetRepositoryStateSnapshotAsync(state.RepositoryPath);
            if (!force && _lastSnapshot != null && currentSnapshot.Equals(_lastSnapshot))
            {
                return;
            }

            if (force)
            {
                _updateState(s => s with { IsLoading = true, ErrorMessage = null });
            }

            await LoadAndApplyRepositoryDataAsync(currentSnapshot);
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    private async Task LoadAndApplyRepositoryDataAsync(GitRepositoryStateSnapshot snapshot)
    {
        var state = _getState();
        GitRepositoryInfo info = await _gitService.GetRepositoryInfoAsync(state.RepositoryPath);
        int maxVisibleBranches = await _settingsCoordinator.ResolveMaxVisibleBranchesAsync();
        var branches = await _gitService.GetBranchesAsync(state.RepositoryPath);
        string? activeBranch = CommitLogHistoryHelper.ResolveActiveBranchName(state.SelectedBranchName, branches);
        var commits = await _gitService.GetCommitLogAsync(
            state.RepositoryPath, maxCount: 300, maxBranches: maxVisibleBranches, branchName: activeBranch);
        var tags = await _gitService.GetTagsAsync(state.RepositoryPath);
        var stashes = await _gitService.GetStashesAsync(state.RepositoryPath);
        var remotes = await _gitService.GetRemotesAsync(state.RepositoryPath);
        var workingTree = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
        var opStatus = await _gitService.GetRepositoryOperationStatusAsync(state.RepositoryPath);

        var commitList = commits.ToImmutableList();
        var filtered = CommitLogHistoryHelper.FilterCommits(commitList, state.SearchQuery);
        var branchItems = CommitLogHistoryHelper.BuildBranchListItems(branches, maxVisibleBranches);

        string branchDisplay = info.IsHeadDetached && !string.IsNullOrEmpty(info.HeadCommitSha)
            ? $"HEAD ({info.HeadCommitSha[..Math.Min(7, info.HeadCommitSha.Length)]})"
            : info.CurrentBranch;

        _lastSnapshot = snapshot;

        _updateState(s => s with
        {
            IsLoading = false,
            CurrentBranch = branchDisplay,
            SelectedBranchName = activeBranch,
            AllCommits = commitList,
            FilteredCommits = filtered,
            Branches = branches.ToImmutableList(),
            BranchListItems = branchItems,
            Tags = tags.ToImmutableList(),
            Stashes = stashes.ToImmutableList(),
            Remotes = remotes.ToImmutableList(),
            WorkingTreeChanges = workingTree.ToImmutableList(),
            OperationStatus = opStatus,
            MergeSourceBranch = CommitLogHistoryHelper.ExtractMergeSourceBranch(opStatus.Message),
            MergeTargetBranch = branchDisplay
        });

        await PostUpdateSubsystemsAsync(filtered);
    }

    private async Task PostUpdateSubsystemsAsync(ImmutableList<CommitSummary> filtered)
    {
        var state = _getState();
        await _historyActionCoordinator.LoadProfilesDataAsync();
        await _favoritesCoordinator.InitializeFavoriteAndRecentStateAsync();
        await _getFavoritesDock().RefreshFavoritesDockDataAsync();

        _getWorkingTreePanel().UpdateWorkingTree(
            state.WorkingTreeChanges,
            state.MergeSourceBranch,
            state.MergeTargetBranch,
            state.OperationStatus,
            state.ActiveProfile);
        _getGitModals().SetAvailableBranches(state.Branches);

        await SynchronizeCommitSelectionAsync(filtered);
    }

    private async Task SynchronizeCommitSelectionAsync(ImmutableList<CommitSummary> filtered)
    {
        if (filtered.Count == 0)
        {
            return;
        }

        var state = _getState();
        CommitSummary toSelect = state.SelectedCommit != null
            ? filtered.FirstOrDefault(c => c.Sha == state.SelectedCommit.Sha) ?? filtered[0]
            : filtered[0];

        bool sameCommitAlreadySelected = state.SelectedCommit != null
            && state.SelectedCommit.Sha == toSelect.Sha
            && !toSelect.IsWip
            && state.SelectedCommitDetail != null;

        if (!sameCommitAlreadySelected)
        {
            await _historyActionCoordinator.SelectCommitAsync(toSelect);
        }
    }
}
