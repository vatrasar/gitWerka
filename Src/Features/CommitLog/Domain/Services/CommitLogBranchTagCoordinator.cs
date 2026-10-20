namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Coordinator managing Git branch and tag creation, deletion, and checkout operations for CommitLog.
/// Usages: CommitLogViewModel branch and tag operations.
/// </summary>
public sealed class CommitLogBranchTagCoordinator
{
    private readonly IGitService _gitService;
    private readonly Func<CommitLogState> _getState;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<Task> _loadRepositoryData;
    private readonly Func<Task<int>> _resolveMaxVisibleBranches;

    public CommitLogBranchTagCoordinator(
        IGitService gitService,
        Func<CommitLogState> getState,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<Task> loadRepositoryData,
        Func<Task<int>> resolveMaxVisibleBranches)
    {
        _gitService = gitService;
        _getState = getState;
        _updateState = updateState;
        _loadRepositoryData = loadRepositoryData;
        _resolveMaxVisibleBranches = resolveMaxVisibleBranches;
    }

    public async Task SelectBranchAsync(BranchListItem? item, Func<CommitSummary, Task> selectCommitCallback)
    {
        string? targetBranch = (item == null || item.IsAllBranches) ? null : item.Name;
        var state = _getState();
        if (string.Equals(state.SelectedBranchName, targetBranch, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _updateState(s => s with
        {
            IsLoading = true,
            SelectedBranchName = targetBranch,
            ErrorMessage = null
        });

        try
        {
            int maxVisibleBranches = await _resolveMaxVisibleBranches();
            var commits = await _gitService.GetCommitLogAsync(
                state.RepositoryPath,
                maxCount: 300,
                maxBranches: maxVisibleBranches,
                branchName: targetBranch);

            var commitList = System.Collections.Immutable.ImmutableList.ToImmutableList(commits);
            var filtered = CommitLogHistoryHelper.FilterCommits(commitList, state.SearchQuery);

            _updateState(s => s with
            {
                IsLoading = false,
                AllCommits = commitList,
                FilteredCommits = filtered,
                SelectedCommit = filtered.FirstOrDefault()
            });

            if (filtered.Count > 0)
            {
                await selectCommitCallback(filtered[0]);
            }
            else
            {
                _updateState(s => s with
                {
                    SelectedCommit = null,
                    SelectedCommitDetail = null,
                    DiffLines = System.Collections.Immutable.ImmutableList<DiffLine>.Empty
                });
            }
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Updates input text for the new branch name.
    /// Usages: CommitLogViewModel and GitModals create branch modal.
    /// </summary>
    /// <param name="text">New branch name.</param>
    public void UpdateNewBranchNameInput(string text)
    {
        _updateState(s => s with { NewBranchNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates input text for the new tag name.
    /// Usages: CommitLogViewModel and GitModals create tag modal.
    /// </summary>
    /// <param name="text">New tag name.</param>
    public void UpdateNewTagNameInput(string text)
    {
        _updateState(s => s with { NewTagNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates optional annotation message input for the new tag.
    /// Usages: CommitLogViewModel and GitModals create tag modal.
    /// </summary>
    /// <param name="text">Tag annotation message.</param>
    public void UpdateNewTagMessageInput(string text)
    {
        _updateState(s => s with { NewTagMessageInput = text ?? string.Empty });
    }

    /// <summary>
    /// Checks out a branch by name.
    /// Usages: CommitLogViewModel and branches list.
    /// </summary>
    /// <param name="branchName">Branch name to checkout.</param>
    public async Task CheckoutBranchAsync(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return;
        }

        var state = _getState();
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _gitService.CheckoutBranchAsync(state.RepositoryPath, branchName);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Checks out a specific commit into a detached HEAD state.
    /// Usages: CommitLogViewModel and commit context menu.
    /// </summary>
    /// <param name="commit">Target commit to checkout, or null to checkout the currently selected commit.</param>
    public async Task CheckoutCommitAsync(CommitSummary? commit = null)
    {
        var state = _getState();
        CommitSummary? target = commit ?? state.SelectedCommit;
        if (target == null || target.IsWip || string.IsNullOrWhiteSpace(target.Sha))
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _gitService.CheckoutCommitAsync(state.RepositoryPath, target.Sha);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Shows the create branch modal dialog.
    /// Usages: CommitLogViewModel and toolbar.
    /// </summary>
    public void ShowCreateBranchModal()
    {
        _updateState(s => s with { IsCreateBranchModalVisible = true, NewBranchNameInput = string.Empty });
    }

    /// <summary>
    /// Hides the create branch modal dialog.
    /// Usages: CommitLogViewModel and GitModals create branch modal.
    /// </summary>
    public void HideCreateBranchModal()
    {
        _updateState(s => s with { IsCreateBranchModalVisible = false });
    }

    /// <summary>
    /// Creates a branch at current HEAD or selected commit.
    /// Usages: CommitLogViewModel and GitModals create branch modal.
    /// </summary>
    public async Task ExecuteCreateBranchAsync()
    {
        var state = _getState();
        if (string.IsNullOrWhiteSpace(state.NewBranchNameInput))
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, IsCreateBranchModalVisible = false });
        try
        {
            await _gitService.CreateBranchAsync(state.RepositoryPath, state.NewBranchNameInput.Trim(), state.SelectedCommit?.Sha);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a local branch.
    /// Usages: CommitLogViewModel and branches context menu.
    /// </summary>
    /// <param name="branchName">Branch name to delete.</param>
    public async Task DeleteBranchAsync(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return;
        }

        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DeleteBranchAsync(state.RepositoryPath, branchName);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Shows create tag modal dialog.
    /// Usages: CommitLogViewModel and toolbar.
    /// </summary>
    public void ShowCreateTagModal()
    {
        _updateState(s => s with
        {
            IsCreateTagModalVisible = true,
            NewTagNameInput = string.Empty,
            NewTagMessageInput = string.Empty
        });
    }

    /// <summary>
    /// Hides create tag modal dialog.
    /// Usages: CommitLogViewModel and GitModals create tag modal.
    /// </summary>
    public void HideCreateTagModal()
    {
        _updateState(s => s with { IsCreateTagModalVisible = false });
    }

    /// <summary>
    /// Creates a tag on the selected commit or HEAD.
    /// Usages: CommitLogViewModel and GitModals create tag modal.
    /// </summary>
    public async Task ExecuteCreateTagAsync()
    {
        var state = _getState();
        if (string.IsNullOrWhiteSpace(state.NewTagNameInput))
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, IsCreateTagModalVisible = false });
        try
        {
            string? message = string.IsNullOrWhiteSpace(state.NewTagMessageInput) ? null : state.NewTagMessageInput.Trim();
            await _gitService.CreateTagAsync(
                state.RepositoryPath,
                state.NewTagNameInput.Trim(),
                state.SelectedCommit?.Sha,
                message);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a tag by name.
    /// Usages: CommitLogViewModel and tags context menu.
    /// </summary>
    /// <param name="tagName">Tag name to delete.</param>
    public async Task DeleteTagAsync(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return;
        }

        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DeleteTagAsync(state.RepositoryPath, tagName);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Opens the create branch modal targeting a specific commit.
    /// Usages: CommitLogViewModel commit context menu.
    /// </summary>
    /// <param name="commit">Target commit summary.</param>
    public void CreateBranchFromCommit(CommitSummary? commit)
    {
        var state = _getState();
        CommitSummary? target = commit ?? state.SelectedCommit;
        if (target != null)
        {
            _updateState(s => s with
            {
                SelectedCommit = target,
                IsCreateBranchModalVisible = true,
                NewBranchNameInput = string.Empty
            });
        }
    }

    /// <summary>
    /// Opens the create tag modal targeting a specific commit.
    /// Usages: CommitLogViewModel commit context menu.
    /// </summary>
    /// <param name="commit">Target commit summary.</param>
    public void CreateTagFromCommit(CommitSummary? commit)
    {
        var state = _getState();
        CommitSummary? target = commit ?? state.SelectedCommit;
        if (target != null)
        {
            _updateState(s => s with
            {
                SelectedCommit = target,
                IsCreateTagModalVisible = true,
                NewTagNameInput = string.Empty,
                NewTagMessageInput = string.Empty
            });
        }
    }
}
