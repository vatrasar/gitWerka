namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.Domain.Enums;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Smart component view model managing Git operation modal dialogues: branches, remotes, tags, stashes, merge, reset, and credentials.
/// </summary>
public partial class GitOperationModalsViewModel : ViewModelBase<GitOperationModalsState>
{
    private readonly Subject<Unit> _confirmCreateBranchSubject = new();
    private readonly Subject<Unit> _confirmCreateRemoteSubject = new();
    private readonly Subject<Unit> _confirmCreateTagSubject = new();
    private readonly Subject<Unit> _confirmCreateStashSubject = new();
    private readonly Subject<Unit> _confirmResetSubject = new();
    private readonly Subject<Unit> _confirmMergeSubject = new();
    private readonly Subject<Unit> _submitCredentialsSubject = new();

    public IObservable<Unit> ConfirmCreateBranchRequested => _confirmCreateBranchSubject;
    public IObservable<Unit> ConfirmCreateRemoteRequested => _confirmCreateRemoteSubject;
    public IObservable<Unit> ConfirmCreateTagRequested => _confirmCreateTagSubject;
    public IObservable<Unit> ConfirmCreateStashRequested => _confirmCreateStashSubject;
    public IObservable<Unit> ConfirmResetRequested => _confirmResetSubject;
    public IObservable<Unit> ConfirmMergeRequested => _confirmMergeSubject;
    public IObservable<Unit> SubmitCredentialsRequested => _submitCredentialsSubject;

    public GitOperationModalsViewModel()
        : base(new GitOperationModalsState())
    {
    }

    /// <summary>
    /// Updates the available branches list for the merge modal.
    /// Usages: CommitLogViewModel on repository data load.
    /// </summary>
    /// <param name="branches">Available branches list.</param>
    public void SetAvailableBranches(ImmutableList<BranchInfo> branches)
    {
        UpdateState(s => s with { AvailableBranches = branches ?? ImmutableList<BranchInfo>.Empty });
    }

    /// <summary>
    /// Opens the create branch modal.
    /// Usages: CommitLogViewModel, UI toolbar button.
    /// </summary>
    public void ShowCreateBranchModal()
    {
        UpdateState(s => s with { IsCreateBranchModalVisible = true, NewBranchNameInput = string.Empty });
    }

    /// <summary>
    /// Opens the create remote modal.
    /// Usages: CommitLogViewModel, UI toolbar button.
    /// </summary>
    public void ShowCreateRemoteModal()
    {
        UpdateState(s => s with { IsCreateRemoteModalVisible = true, NewRemoteNameInput = string.Empty, NewRemoteUrlInput = string.Empty });
    }

    /// <summary>
    /// Opens the create tag modal.
    /// Usages: CommitLogViewModel, UI toolbar button.
    /// </summary>
    public void ShowCreateTagModal()
    {
        UpdateState(s => s with { IsCreateTagModalVisible = true, NewTagNameInput = string.Empty, NewTagMessageInput = string.Empty });
    }

    /// <summary>
    /// Opens the create stash modal.
    /// Usages: CommitLogViewModel, UI toolbar button.
    /// </summary>
    public void ShowCreateStashModal()
    {
        UpdateState(s => s with { IsStashModalVisible = true, NewStashMessageInput = string.Empty });
    }

    /// <summary>
    /// Opens the reset confirmation modal.
    /// Usages: CommitLogViewModel on reset action.
    /// </summary>
    public void ShowResetConfirmModal(GitResetType type, CommitSummary commit)
    {
        UpdateState(s => s with { IsResetConfirmModalVisible = true, PendingResetType = type, PendingResetCommit = commit });
    }

    /// <summary>
    /// Opens the merge branch modal.
    /// Usages: CommitLogViewModel on merge action.
    /// </summary>
    public void ShowMergeModal(string? branchName = null)
    {
        string? targetBranch = branchName ?? State.AvailableBranches.FirstOrDefault(b => !b.IsCurrentHead)?.Name;
        UpdateState(s => s with
        {
            IsMergeModalVisible = true,
            SelectedMergeBranch = targetBranch,
            MergeFastForwardMode = GitMergeFastForwardMode.FastForward
        });
    }

    /// <summary>
    /// Opens the credentials prompt modal.
    /// Usages: CommitLogViewModel on auth required error.
    /// </summary>
    public void ShowCredentialsModal(RemoteOperationPending operation, GitCredentialType defaultType = GitCredentialType.Https, string? errorMessage = null)
    {
        UpdateState(s => s with
        {
            IsCredentialsModalVisible = true,
            PendingRemoteOperation = operation,
            CredentialsAuthType = defaultType,
            CredentialsPasswordInput = string.Empty,
            CredentialsErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// Updates the branch name input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewBranchNameInput(string text) => UpdateState(s => s with { NewBranchNameInput = text ?? string.Empty });

    /// <summary>
    /// Updates the remote name input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewRemoteNameInput(string text) => UpdateState(s => s with { NewRemoteNameInput = text ?? string.Empty });

    /// <summary>
    /// Updates the remote URL input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewRemoteUrlInput(string text) => UpdateState(s => s with { NewRemoteUrlInput = text ?? string.Empty });

    /// <summary>
    /// Updates the tag name input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewTagNameInput(string text) => UpdateState(s => s with { NewTagNameInput = text ?? string.Empty });

    /// <summary>
    /// Updates the tag message input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewTagMessageInput(string text) => UpdateState(s => s with { NewTagMessageInput = text ?? string.Empty });

    /// <summary>
    /// Updates the stash message input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateNewStashMessageInput(string text) => UpdateState(s => s with { NewStashMessageInput = text ?? string.Empty });

    /// <summary>
    /// Updates the selected branch to merge.
    /// Usages: GitOperationModalsView combo box.
    /// </summary>
    public void SelectMergeBranch(string branchName) => UpdateState(s => s with { SelectedMergeBranch = branchName });

    /// <summary>
    /// Updates the merge fast forward mode.
    /// Usages: GitOperationModalsView radio buttons.
    /// </summary>
    public void SetMergeFastForwardMode(GitMergeFastForwardMode mode) => UpdateState(s => s with { MergeFastForwardMode = mode });

    /// <summary>
    /// Updates the credentials auth type.
    /// Usages: GitOperationModalsView radio buttons.
    /// </summary>
    public void SetCredentialsAuthType(GitCredentialType type) => UpdateState(s => s with { CredentialsAuthType = type });

    /// <summary>
    /// Updates the credentials username input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateCredentialsUsernameInput(string text) => UpdateState(s => s with { CredentialsUsernameInput = text ?? string.Empty });

    /// <summary>
    /// Updates the credentials password input.
    /// Usages: GitOperationModalsView text box.
    /// </summary>
    public void UpdateCredentialsPasswordInput(string text) => UpdateState(s => s with { CredentialsPasswordInput = text ?? string.Empty });

    [ReactiveCommand]
    private void HideCreateBranchModal() => UpdateState(s => s with { IsCreateBranchModalVisible = false });

    [ReactiveCommand]
    private void ConfirmCreateBranch()
    {
        UpdateState(s => s with { IsCreateBranchModalVisible = false });
        _confirmCreateBranchSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideCreateRemoteModal() => UpdateState(s => s with { IsCreateRemoteModalVisible = false });

    [ReactiveCommand]
    private void ConfirmCreateRemote()
    {
        UpdateState(s => s with { IsCreateRemoteModalVisible = false });
        _confirmCreateRemoteSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideCreateTagModal() => UpdateState(s => s with { IsCreateTagModalVisible = false });

    [ReactiveCommand]
    private void ConfirmCreateTag()
    {
        UpdateState(s => s with { IsCreateTagModalVisible = false });
        _confirmCreateTagSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideStashModal() => UpdateState(s => s with { IsStashModalVisible = false });

    [ReactiveCommand]
    private void ConfirmCreateStash()
    {
        UpdateState(s => s with { IsStashModalVisible = false });
        _confirmCreateStashSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideResetConfirmModal() => UpdateState(s => s with { IsResetConfirmModalVisible = false, PendingResetCommit = null });

    [ReactiveCommand]
    private void ConfirmReset()
    {
        UpdateState(s => s with { IsResetConfirmModalVisible = false, PendingResetCommit = null });
        _confirmResetSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideMergeModal() => UpdateState(s => s with { IsMergeModalVisible = false });

    [ReactiveCommand]
    private void ConfirmMerge()
    {
        UpdateState(s => s with { IsMergeModalVisible = false });
        _confirmMergeSubject.OnNext(Unit.Default);
    }

    [ReactiveCommand]
    private void HideCredentialsModal() => UpdateState(s => s with { IsCredentialsModalVisible = false, PendingRemoteOperation = RemoteOperationPending.None });

    [ReactiveCommand]
    private void SubmitCredentials()
    {
        UpdateState(s => s with { IsCredentialsModalVisible = false });
        _submitCredentialsSubject.OnNext(Unit.Default);
    }
}
