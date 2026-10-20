namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.InteractiveRebaseModal;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Smart component view model managing interactive rebase sequence editing and execution.
/// </summary>
public partial class InteractiveRebaseModalViewModel : ViewModelBase<InteractiveRebaseModalState>
{
    private readonly IGitService _gitService;
    private readonly Subject<Unit> _rebaseCompletedSubject = new();

    public IObservable<Unit> RebaseCompleted => _rebaseCompletedSubject;

    public InteractiveRebaseModalViewModel(IGitService gitService, string initialRepositoryPath = "")
        : base(new InteractiveRebaseModalState { RepositoryPath = initialRepositoryPath })
    {
        _gitService = gitService;
    }

    /// <summary>
    /// Updates the active repository path.
    /// Usages: CommitLogViewModel on repository switch.
    /// </summary>
    /// <param name="path">Current repository path.</param>
    public void SetRepositoryPath(string path)
    {
        UpdateState(s => s with { RepositoryPath = path ?? string.Empty });
    }

    /// <summary>
    /// Opens the interactive rebase modal initialized with upstream commit and rebase steps.
    /// Usages: CommitLogViewModel on initiating interactive rebase.
    /// </summary>
    /// <param name="repoPath">Target repository path.</param>
    /// <param name="upstreamSha">Upstream commit SHA.</param>
    /// <param name="upstreamShortSha">Abbreviated upstream commit SHA.</param>
    /// <param name="steps">Rebase steps sequence.</param>
    public void ShowModal(string repoPath, string upstreamSha, string upstreamShortSha, IEnumerable<RebaseStep> steps)
    {
        var stepList = steps.ToImmutableList();
        UpdateState(s => s with
        {
            IsVisible = true,
            RepositoryPath = repoPath,
            RebaseUpstreamSha = upstreamSha,
            RebaseUpstreamShortSha = upstreamShortSha,
            RebaseSteps = stepList,
            SelectedRebaseStep = stepList.FirstOrDefault()
        });
    }

    /// <summary>
    /// Selects a rebase step in the modal list.
    /// Usages: InteractiveRebaseModalView list selection.
    /// </summary>
    /// <param name="step">The selected rebase step.</param>
    public void SelectRebaseStep(RebaseStep step)
    {
        UpdateState(s => s with { SelectedRebaseStep = step });
    }

    /// <summary>
    /// Updates the rebase action for a specific step.
    /// Usages: InteractiveRebaseModalView action buttons.
    /// </summary>
    /// <param name="step">The target rebase step.</param>
    /// <param name="action">The new action.</param>
    public void ChangeRebaseStepAction(RebaseStep step, RebaseStepAction action)
    {
        if (step == null)
        {
            return;
        }

        var updated = UpdateStepInList(State.RebaseSteps, step, s => s with { Action = action });
        RebaseStep? updatedSelected = updated.FirstOrDefault(s => s.CommitSha == step.CommitSha);
        UpdateState(s => s with
        {
            RebaseSteps = updated,
            SelectedRebaseStep = updatedSelected ?? s.SelectedRebaseStep
        });
    }

    /// <summary>
    /// Updates the reword message for a rebase step.
    /// Usages: InteractiveRebaseModalView message editor.
    /// </summary>
    /// <param name="step">The target rebase step.</param>
    /// <param name="newMessage">The new commit message.</param>
    public void UpdateRebaseStepMessage(RebaseStep step, string newMessage)
    {
        if (step == null)
        {
            return;
        }

        var updated = UpdateStepInList(State.RebaseSteps, step, s => s with { NewMessage = newMessage ?? string.Empty });
        RebaseStep? updatedSelected = updated.FirstOrDefault(s => s.CommitSha == step.CommitSha);
        UpdateState(s => s with
        {
            RebaseSteps = updated,
            SelectedRebaseStep = updatedSelected ?? s.SelectedRebaseStep
        });
    }

    [ReactiveCommand]
    private void HideInteractiveRebaseModal()
    {
        UpdateState(s => s with { IsVisible = false });
    }

    [ReactiveCommand]
    private void MoveRebaseStepUp(RebaseStep? step)
    {
        RebaseStep? target = step ?? State.SelectedRebaseStep;
        if (target == null)
        {
            return;
        }

        int index = State.RebaseSteps.IndexOf(target);
        if (index <= 0)
        {
            return;
        }

        var reordered = SwapRebaseSteps(State.RebaseSteps, index, index - 1);
        UpdateState(s => s with { RebaseSteps = reordered, SelectedRebaseStep = target });
    }

    [ReactiveCommand]
    private void MoveRebaseStepDown(RebaseStep? step)
    {
        RebaseStep? target = step ?? State.SelectedRebaseStep;
        if (target == null)
        {
            return;
        }

        int index = State.RebaseSteps.IndexOf(target);
        if (index < 0 || index >= State.RebaseSteps.Count - 1)
        {
            return;
        }

        var reordered = SwapRebaseSteps(State.RebaseSteps, index, index + 1);
        UpdateState(s => s with { RebaseSteps = reordered, SelectedRebaseStep = target });
    }

    [ReactiveCommand]
    private async Task ExecuteInteractiveRebase()
    {
        if (string.IsNullOrWhiteSpace(State.RebaseUpstreamSha) || State.RebaseSteps.Count == 0)
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, IsVisible = false });
        try
        {
            await _gitService.ExecuteInteractiveRebaseAsync(State.RepositoryPath, State.RebaseUpstreamSha, State.RebaseSteps);
            UpdateState(s => s with { IsLoading = false });
            _rebaseCompletedSubject.OnNext(Unit.Default);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
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
