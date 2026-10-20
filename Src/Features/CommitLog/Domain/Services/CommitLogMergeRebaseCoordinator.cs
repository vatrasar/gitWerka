namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Service coordinating merge operations, interactive rebasing, and operation conflict continuation/abortion.
/// </summary>
public class CommitLogMergeRebaseCoordinator
{
    private readonly IGitService _gitService;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<CommitLogState> _getState;

    public CommitLogMergeRebaseCoordinator(
        IGitService gitService,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<CommitLogState> getState)
    {
        _gitService = gitService;
        _updateState = updateState;
        _getState = getState;
    }

    public void SelectMergeBranch(string branchName) =>
        _updateState(s => s with { SelectedMergeBranch = branchName });

    public void SetMergeFastForwardMode(GitMergeFastForwardMode mode) =>
        _updateState(s => s with { MergeFastForwardMode = mode });

    public void SelectRebaseStep(RebaseStep step) =>
        _updateState(s => s with { SelectedRebaseStep = step });

    public void ChangeRebaseStepAction(RebaseStep step, RebaseStepAction action)
    {
        if (step == null)
        {
            return;
        }

        var updated = UpdateStepInList(_getState().RebaseSteps, step, s => s with { Action = action });
        _updateState(s => s with { RebaseSteps = updated });
    }

    public void UpdateRebaseStepMessage(RebaseStep step, string newMessage)
    {
        if (step == null)
        {
            return;
        }

        var updated = UpdateStepInList(_getState().RebaseSteps, step, s => s with { NewMessage = newMessage ?? string.Empty });
        _updateState(s => s with { RebaseSteps = updated });
    }

    public void ShowMergeModal(string? branchName = null)
    {
        var state = _getState();
        string? targetBranch = branchName ?? state.Branches.FirstOrDefault(b => !b.IsCurrentHead)?.Name;
        _updateState(s => s with
        {
            IsMergeModalVisible = true,
            SelectedMergeBranch = targetBranch,
            MergeFastForwardMode = GitMergeFastForwardMode.FastForward
        });
    }

    public void HideMergeModal() =>
        _updateState(s => s with { IsMergeModalVisible = false });

    public async Task ExecuteMergeAsync(Func<Task> reloadCallback)
    {
        var state = _getState();
        if (string.IsNullOrWhiteSpace(state.SelectedMergeBranch))
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, IsMergeModalVisible = false });
        try
        {
            await _gitService.MergeBranchAsync(state.RepositoryPath, state.SelectedMergeBranch, state.MergeFastForwardMode);
            await reloadCallback();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    public async Task StartInteractiveRebaseAsync(CommitSummary? commit)
    {
        var state = _getState();
        CommitSummary? target = commit ?? state.SelectedCommit;
        if (target == null)
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            IReadOnlyList<RebaseStep> steps = await _gitService.GetInteractiveRebaseStepsAsync(state.RepositoryPath, target.Sha);
            var stepsList = steps.ToImmutableList();

            _updateState(s => s with
            {
                IsLoading = false,
                IsInteractiveRebaseModalVisible = true,
                RebaseUpstreamSha = target.Sha,
                RebaseUpstreamShortSha = target.ShortSha,
                RebaseSteps = stepsList,
                SelectedRebaseStep = stepsList.FirstOrDefault()
            });
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    public void ShowInteractiveRebaseModal() =>
        _updateState(s => s with { IsInteractiveRebaseModalVisible = true });

    public void HideInteractiveRebaseModal()
    {
        _updateState(s => s with
        {
            IsInteractiveRebaseModalVisible = false,
            RebaseSteps = ImmutableList<RebaseStep>.Empty,
            SelectedRebaseStep = null
        });
    }

    public void MoveRebaseStepUp(RebaseStep? step)
    {
        var state = _getState();
        RebaseStep? target = step ?? state.SelectedRebaseStep;
        if (target == null)
        {
            return;
        }

        int index = state.RebaseSteps.IndexOf(target);
        if (index <= 0)
        {
            return;
        }

        var reordered = SwapRebaseSteps(state.RebaseSteps, index, index - 1);
        _updateState(s => s with { RebaseSteps = reordered, SelectedRebaseStep = target });
    }

    public void MoveRebaseStepDown(RebaseStep? step)
    {
        var state = _getState();
        RebaseStep? target = step ?? state.SelectedRebaseStep;
        if (target == null)
        {
            return;
        }

        int index = state.RebaseSteps.IndexOf(target);
        if (index < 0 || index >= state.RebaseSteps.Count - 1)
        {
            return;
        }

        var reordered = SwapRebaseSteps(state.RebaseSteps, index, index + 1);
        _updateState(s => s with { RebaseSteps = reordered, SelectedRebaseStep = target });
    }

    public async Task ExecuteInteractiveRebaseAsync(Func<Task> reloadCallback)
    {
        var state = _getState();
        if (string.IsNullOrWhiteSpace(state.RebaseUpstreamSha) || state.RebaseSteps.Count == 0)
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, IsInteractiveRebaseModalVisible = false });
        try
        {
            await _gitService.ExecuteInteractiveRebaseAsync(state.RepositoryPath, state.RebaseUpstreamSha, state.RebaseSteps);
            await reloadCallback();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    public async Task ContinueOperationAsync(Func<Task> reloadCallback)
    {
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            var state = _getState();
            if (state.OperationStatus.CurrentOperation == RepositoryOperationType.Merge)
            {
                await _gitService.ContinueMergeAsync(state.RepositoryPath);
            }
            else if (state.OperationStatus.CurrentOperation == RepositoryOperationType.Rebase)
            {
                await _gitService.ContinueRebaseAsync(state.RepositoryPath);
            }

            await reloadCallback();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    public async Task AbortOperationAsync(Func<Task> reloadCallback)
    {
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            var state = _getState();
            if (state.OperationStatus.CurrentOperation == RepositoryOperationType.Merge)
            {
                await _gitService.AbortMergeAsync(state.RepositoryPath);
            }
            else if (state.OperationStatus.CurrentOperation == RepositoryOperationType.Rebase)
            {
                await _gitService.AbortRebaseAsync(state.RepositoryPath);
            }

            await reloadCallback();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    private static ImmutableList<RebaseStep> SwapRebaseSteps(ImmutableList<RebaseStep> steps, int index1, int index2)
    {
        var builder = steps.ToBuilder();
        (builder[index1], builder[index2]) = (builder[index2], builder[index1]);
        return builder.ToImmutable();
    }

    private static ImmutableList<RebaseStep> UpdateStepInList(ImmutableList<RebaseStep> steps, RebaseStep target, Func<RebaseStep, RebaseStep> updater)
    {
        int index = steps.IndexOf(target);
        if (index < 0)
        {
            return steps;
        }

        var builder = steps.ToBuilder();
        builder[index] = updater(target);
        return builder.ToImmutable();
    }
}
