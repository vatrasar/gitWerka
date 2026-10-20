namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;

using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Subjects;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Smart component view model managing staged/unstaged changes, conflicts, and the commit composer.
/// </summary>
public partial class WorkingTreePanelViewModel : ViewModelBase<WorkingTreePanelState>
{
    private readonly Subject<Unit> _stageAllSubject = new();
    private readonly Subject<Unit> _unstageAllSubject = new();
    private readonly Subject<string> _stageFileSubject = new();
    private readonly Subject<string> _unstageFileSubject = new();
    private readonly Subject<Unit> _markAllResolvedSubject = new();
    private readonly Subject<WorkingTreeChange> _openConflictResolverSubject = new();
    private readonly Subject<Unit> _generateAiCommitSubject = new();
    private readonly Subject<Unit> _dismissAiErrorSubject = new();
    private readonly Subject<Unit> _openSettingsSubject = new();
    private readonly Subject<Unit> _configureProfileSubject = new();
    private readonly Subject<Unit> _commitSubject = new();

    public IObservable<Unit> StageAllRequested => _stageAllSubject;
    public IObservable<Unit> UnstageAllRequested => _unstageAllSubject;
    public IObservable<string> StageFileRequested => _stageFileSubject;
    public IObservable<string> UnstageFileRequested => _unstageFileSubject;
    public IObservable<Unit> MarkAllResolvedRequested => _markAllResolvedSubject;
    public IObservable<WorkingTreeChange> OpenConflictResolverRequested => _openConflictResolverSubject;
    public IObservable<Unit> GenerateAiCommitRequested => _generateAiCommitSubject;
    public IObservable<Unit> DismissAiErrorRequested => _dismissAiErrorSubject;
    public IObservable<Unit> OpenSettingsRequested => _openSettingsSubject;
    public IObservable<Unit> ConfigureProfileRequested => _configureProfileSubject;
    public IObservable<Unit> CommitRequested => _commitSubject;

    public WorkingTreePanelViewModel()
        : base(new WorkingTreePanelState())
    {
    }

    /// <summary>
    /// Updates working tree changes and operation status in component state.
    /// Usages: CommitLogViewModel on repository changes refresh.
    /// </summary>
    public void UpdateWorkingTree(
        ImmutableList<WorkingTreeChange> changes,
        string mergeSourceBranch,
        string mergeTargetBranch,
        RepositoryOperationStatus operationStatus,
        GitProfile? activeProfile)
    {
        UpdateState(s => s with
        {
            WorkingTreeChanges = changes ?? ImmutableList<WorkingTreeChange>.Empty,
            MergeSourceBranch = mergeSourceBranch ?? string.Empty,
            MergeTargetBranch = mergeTargetBranch ?? string.Empty,
            OperationStatus = operationStatus ?? new(),
            ActiveProfile = activeProfile
        });
    }

    /// <summary>
    /// Updates AI generation status and banners in component state.
    /// Usages: CommitLogViewModel AI commit message workflow.
    /// </summary>
    public void UpdateAiStatus(bool isGenerating, string? errorMessage, bool isSettingsActionVisible)
    {
        UpdateState(s => s with
        {
            IsGeneratingAiCommit = isGenerating,
            AiErrorMessage = errorMessage,
            IsAiErrorSettingsActionVisible = isSettingsActionVisible
        });
    }

    /// <summary>
    /// Updates the commit message input text.
    /// Usages: WorkingTreePanelView commit message text box, CommitLogViewModel AI result.
    /// </summary>
    public void UpdateCommitMessageInput(string text)
    {
        UpdateState(s => s with { CommitMessageInput = text ?? string.Empty });
    }

    /// <summary>
    /// Toggles or updates the amend commit option.
    /// Usages: WorkingTreePanelView amend check box.
    /// </summary>
    public void SetAmendCommit(bool isAmend)
    {
        UpdateState(s => s with { IsAmendCommit = isAmend });
    }

    [ReactiveCommand]
    private void StageAll() => _stageAllSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void UnstageAll() => _unstageAllSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void StageFile(string path) => _stageFileSubject.OnNext(path);

    [ReactiveCommand]
    private void UnstageFile(string path) => _unstageFileSubject.OnNext(path);

    [ReactiveCommand]
    private void MarkAllResolved() => _markAllResolvedSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void OpenConflictResolver(WorkingTreeChange file) => _openConflictResolverSubject.OnNext(file);

    [ReactiveCommand]
    private void GenerateAiCommit() => _generateAiCommitSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void DismissAiError() => _dismissAiErrorSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void OpenSettings() => _openSettingsSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void ConfigureProfile() => _configureProfileSubject.OnNext(Unit.Default);

    [ReactiveCommand]
    private void ExecuteCommit() => _commitSubject.OnNext(Unit.Default);
}
