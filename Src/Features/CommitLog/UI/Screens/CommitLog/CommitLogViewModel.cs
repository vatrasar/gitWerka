namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.Domain.Enums;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// ViewModel for the CommitLog workspace screen displaying history graph, branch pointers, commit details, and diffs.
/// </summary>
public partial class CommitLogViewModel : ViewModelBase<CommitLogState>, IRoutableViewModel
{
    private readonly IGitService _gitService;
    private readonly ISystemLauncherService _systemLauncherService;

    public string? UrlPathSegment => "commit-log";

    public IScreen HostScreen { get; }

    public CommitLogViewModel(
        IScreen hostScreen,
        string repositoryPath,
        IGitService gitService,
        ISystemLauncherService? systemLauncherService = null) : base(new CommitLogState
        {
            RepositoryPath = repositoryPath,
            RepositoryName = new DirectoryInfo(repositoryPath).Name
        })
    {
        HostScreen = hostScreen;
        _gitService = gitService;
        _systemLauncherService = systemLauncherService ?? new GitWerka.Infrastructure.Services.SystemLauncherService();

        LoadRepositoryDataCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Filters commits based on user input string matching commit message, author, or SHA hash.
    /// Usages: CommitLogView search input.
    /// </summary>
    /// <param name="query">Filter text query.</param>
    public void UpdateSearchQuery(string query)
    {
        string trimmed = query?.Trim() ?? string.Empty;
        var filtered = FilterCommits(State.AllCommits, trimmed);

        UpdateState(s => s with
        {
            SearchQuery = trimmed,
            FilteredCommits = filtered
        });
    }

    /// <summary>
    /// Updates the text of the commit message composer.
    /// Usages: CommitLogView commit composer.
    /// </summary>
    /// <param name="message">New commit message input text.</param>
    public void UpdateCommitMessageInput(string message)
    {
        UpdateState(s => s with { CommitMessageInput = message ?? string.Empty });
    }

    /// <summary>
    /// Toggles the amend previous commit option. Pre-populates the commit message with HEAD commit's message if currently empty.
    /// Usages: CommitLogView amend checkbox.
    /// </summary>
    /// <param name="isAmend">True to amend previous commit, false for normal commit.</param>
    public void ToggleAmendCommit(bool isAmend)
    {
        if (isAmend)
        {
            string headMessage = State.AllCommits.FirstOrDefault()?.Message ?? string.Empty;
            string messageToUse = string.IsNullOrWhiteSpace(State.CommitMessageInput) ? headMessage : State.CommitMessageInput;

            UpdateState(s => s with
            {
                IsAmendCommit = true,
                CommitMessageInput = messageToUse
            });
            return;
        }

        UpdateState(s => s with { IsAmendCommit = false });
    }

    /// <summary>
    /// Updates the credentials username input.
    /// Usages: CommitLogView credentials modal.
    /// </summary>
    /// <param name="username">Entered username.</param>
    public void UpdateCredentialsUsernameInput(string username)
    {
        UpdateState(s => s with { CredentialsUsernameInput = username ?? string.Empty });
    }

    /// <summary>
    /// Updates the credentials password or SSH passphrase input.
    /// Usages: CommitLogView credentials modal.
    /// </summary>
    /// <param name="password">Entered password or passphrase.</param>
    public void UpdateCredentialsPasswordInput(string password)
    {
        UpdateState(s => s with { CredentialsPasswordInput = password ?? string.Empty });
    }

    /// <summary>
    /// Sets the credentials authentication type.
    /// Usages: CommitLogView credentials modal auth type selection.
    /// </summary>
    /// <param name="authType">Selected authentication type.</param>
    public void SetCredentialsAuthType(GitCredentialType authType)
    {
        UpdateState(s => s with { CredentialsAuthType = authType });
    }

    /// <summary>
    /// Switches the active view tab in the left sidebar.
    /// Usages: CommitLogView sidebar tabs.
    /// </summary>
    /// <param name="tab">Target sidebar tab.</param>
    public void SetActiveSidebarTab(CommitLogSidebarTab tab)
    {
        UpdateState(s => s with { ActiveSidebarTab = tab });
    }

    /// <summary>
    /// Updates the text input for creating a new branch.
    /// Usages: CommitLogView create branch modal.
    /// </summary>
    /// <param name="text">New branch name text.</param>
    public void UpdateNewBranchNameInput(string text)
    {
        UpdateState(s => s with { NewBranchNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates the text input for creating a new tag name.
    /// Usages: CommitLogView create tag modal.
    /// </summary>
    /// <param name="text">New tag name text.</param>
    public void UpdateNewTagNameInput(string text)
    {
        UpdateState(s => s with { NewTagNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates the message input for creating an annotated tag.
    /// Usages: CommitLogView create tag modal.
    /// </summary>
    /// <param name="text">New tag annotation text.</param>
    public void UpdateNewTagMessageInput(string text)
    {
        UpdateState(s => s with { NewTagMessageInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates the description input for creating a stash.
    /// Usages: CommitLogView create stash modal.
    /// </summary>
    /// <param name="text">New stash description text.</param>
    public void UpdateNewStashMessageInput(string text)
    {
        UpdateState(s => s with { NewStashMessageInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates user name configuration input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Author user name.</param>
    public void UpdateConfigUserNameInput(string text)
    {
        UpdateState(s => s with { ConfigUserNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates user email configuration input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Author email address.</param>
    public void UpdateConfigUserEmailInput(string text)
    {
        UpdateState(s => s with { ConfigUserEmailInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates the selected branch for merge in the merge modal.
    /// Usages: CommitLogView merge modal.
    /// </summary>
    /// <param name="branchName">The branch name to merge.</param>
    public void SelectMergeBranch(string branchName)
    {
        UpdateState(s => s with { SelectedMergeBranch = branchName });
    }

    /// <summary>
    /// Updates the merge fast-forward mode preference.
    /// Usages: CommitLogView merge modal.
    /// </summary>
    /// <param name="mode">The fast forward mode.</param>
    public void SetMergeFastForwardMode(GitMergeFastForwardMode mode)
    {
        UpdateState(s => s with { MergeFastForwardMode = mode });
    }

    /// <summary>
    /// Selects a rebase step in the interactive rebase modal.
    /// Usages: CommitLogView interactive rebase list.
    /// </summary>
    /// <param name="step">The selected rebase step.</param>
    public void SelectRebaseStep(RebaseStep step)
    {
        UpdateState(s => s with { SelectedRebaseStep = step });
    }

    /// <summary>
    /// Updates the rebase action for a specific step.
    /// Usages: CommitLogView interactive rebase action selector.
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
        UpdateState(s => s with { RebaseSteps = updated });
    }

    /// <summary>
    /// Updates the reword message for a rebase step.
    /// Usages: CommitLogView interactive rebase message editor.
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
        UpdateState(s => s with { RebaseSteps = updated });
    }

    /// <summary>
    /// Selects a commit and loads its details and changed files list.
    /// Usages: CommitLogView commit list selection.
    /// </summary>
    /// <param name="commit">The selected commit summary.</param>
    /// <returns>Task tracking async details retrieval.</returns>
    public async Task SelectCommitAsync(CommitSummary commit)
    {
        if (commit == null)
        {
            return;
        }

        UpdateState(s => s with
        {
            SelectedCommit = commit,
            SelectedCommitDetail = null,
            SelectedFileDiff = null,
            DiffLines = ImmutableList<DiffLine>.Empty
        });

        CommitDetail? detail = await _gitService.GetCommitDetailAsync(State.RepositoryPath, commit.Sha);

        UpdateState(s => s with { SelectedCommitDetail = detail });

        if (detail != null && detail.ChangedFiles.Count > 0)
        {
            await SelectFileDiffAsync(detail.ChangedFiles[0]);
        }
    }

    /// <summary>
    /// Selects a file from the changed files list and loads its diff lines.
    /// Usages: CommitLogView file diff selection.
    /// </summary>
    /// <param name="file">Selected changed file summary.</param>
    /// <returns>Task tracking async diff computation.</returns>
    public async Task SelectFileDiffAsync(FileDiffSummary file)
    {
        if (file == null || State.SelectedCommit == null)
        {
            return;
        }

        UpdateState(s => s with { SelectedFileDiff = file });

        IReadOnlyList<DiffLine> lines = await _gitService.GetFileDiffLinesAsync(
            State.RepositoryPath,
            State.SelectedCommit.Sha,
            file.Path);

        UpdateState(s => s with { DiffLines = lines.ToImmutableList() });
    }

    [ReactiveCommand]
    private async Task LoadRepositoryData()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            GitRepositoryInfo info = await _gitService.GetRepositoryInfoAsync(State.RepositoryPath);
            IReadOnlyList<CommitSummary> commits = await _gitService.GetCommitLogAsync(State.RepositoryPath);
            IReadOnlyList<BranchInfo> branches = await _gitService.GetBranchesAsync(State.RepositoryPath);
            IReadOnlyList<TagInfo> tags = await _gitService.GetTagsAsync(State.RepositoryPath);
            IReadOnlyList<StashInfo> stashes = await _gitService.GetStashesAsync(State.RepositoryPath);
            IReadOnlyList<RemoteInfo> remotes = await _gitService.GetRemotesAsync(State.RepositoryPath);
            IReadOnlyList<WorkingTreeChange> workingTree = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            RepositoryOperationStatus opStatus = await _gitService.GetRepositoryOperationStatusAsync(State.RepositoryPath);

            var commitList = commits.ToImmutableList();
            var filtered = FilterCommits(commitList, State.SearchQuery);

            UpdateState(s => s with
            {
                IsLoading = false,
                CurrentBranch = info.CurrentBranch,
                AllCommits = commitList,
                FilteredCommits = filtered,
                Branches = branches.ToImmutableList(),
                Tags = tags.ToImmutableList(),
                Stashes = stashes.ToImmutableList(),
                Remotes = remotes.ToImmutableList(),
                WorkingTreeChanges = workingTree.ToImmutableList(),
                OperationStatus = opStatus
            });

            if (filtered.Count > 0 && State.SelectedCommit == null)
            {
                await SelectCommitAsync(filtered[0]);
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task CheckoutBranch(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _gitService.CheckoutBranchAsync(State.RepositoryPath, branchName);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task StageFile(string filePath)
    {
        try
        {
            await _gitService.StageFileAsync(State.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            UpdateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task UnstageFile(string filePath)
    {
        try
        {
            await _gitService.UnstageFileAsync(State.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            UpdateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task DiscardFile(string filePath)
    {
        try
        {
            await _gitService.DiscardFileChangesAsync(State.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            UpdateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ExecuteCommit()
    {
        if (string.IsNullOrWhiteSpace(State.CommitMessageInput))
        {
            UpdateState(s => s with { ErrorMessage = "Commit message cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _gitService.CreateCommitAsync(
                State.RepositoryPath,
                State.CommitMessageInput.Trim(),
                "GitWerka User",
                "user@gitwerka.local",
                State.IsAmendCommit);

            UpdateState(s => s with
            {
                CommitMessageInput = string.Empty,
                IsAmendCommit = false
            });
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void DismissError()
    {
        UpdateState(s => s with { ErrorMessage = null });
    }

    [ReactiveCommand]
    private void OpenFileManager()
    {
        try
        {
            _systemLauncherService.OpenInFileManager(State.RepositoryPath);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void OpenTerminal()
    {
        try
        {
            _systemLauncherService.OpenInTerminal(State.RepositoryPath);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void OpenEditor()
    {
        try
        {
            _systemLauncherService.OpenInEditor(State.RepositoryPath);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task Fetch()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.FetchAsync(State.RepositoryPath);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Fetch);
        }
    }

    [ReactiveCommand]
    private async Task Pull()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.PullAsync(State.RepositoryPath);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Pull);
        }
    }

    [ReactiveCommand]
    private async Task Push()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.PushAsync(State.RepositoryPath);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Push);
        }
    }

    [ReactiveCommand]
    private void CancelCredentials()
    {
        UpdateState(s => s with
        {
            IsCredentialsModalVisible = false,
            PendingRemoteOperation = RemoteOperationPending.None,
            CredentialsUsernameInput = string.Empty,
            CredentialsPasswordInput = string.Empty,
            CredentialsErrorMessage = null
        });
    }

    [ReactiveCommand]
    private async Task SubmitCredentials()
    {
        var pending = State.PendingRemoteOperation;
        if (pending == RemoteOperationPending.None)
        {
            UpdateState(s => s with { IsCredentialsModalVisible = false });
            return;
        }

        GitCredentials credentials = BuildCredentialsFromState();
        UpdateState(s => s with { IsLoading = true, CredentialsErrorMessage = null });

        try
        {
            await ExecutePendingRemoteOperationAsync(pending, credentials);
            UpdateState(s => s with
            {
                IsCredentialsModalVisible = false,
                PendingRemoteOperation = RemoteOperationPending.None,
                CredentialsUsernameInput = string.Empty,
                CredentialsPasswordInput = string.Empty
            });
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                CredentialsErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void ShowCreateBranchModal()
    {
        UpdateState(s => s with { IsCreateBranchModalVisible = true, NewBranchNameInput = string.Empty });
    }

    [ReactiveCommand]
    private void HideCreateBranchModal()
    {
        UpdateState(s => s with { IsCreateBranchModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteCreateBranch()
    {
        if (string.IsNullOrWhiteSpace(State.NewBranchNameInput))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, IsCreateBranchModalVisible = false });
        try
        {
            await _gitService.CreateBranchAsync(State.RepositoryPath, State.NewBranchNameInput.Trim(), State.SelectedCommit?.Sha);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task DeleteBranch(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DeleteBranchAsync(State.RepositoryPath, branchName);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void ShowCreateTagModal()
    {
        UpdateState(s => s with
        {
            IsCreateTagModalVisible = true,
            NewTagNameInput = string.Empty,
            NewTagMessageInput = string.Empty
        });
    }

    [ReactiveCommand]
    private void HideCreateTagModal()
    {
        UpdateState(s => s with { IsCreateTagModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteCreateTag()
    {
        if (string.IsNullOrWhiteSpace(State.NewTagNameInput))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, IsCreateTagModalVisible = false });
        try
        {
            string? message = string.IsNullOrWhiteSpace(State.NewTagMessageInput) ? null : State.NewTagMessageInput.Trim();
            await _gitService.CreateTagAsync(
                State.RepositoryPath,
                State.NewTagNameInput.Trim(),
                State.SelectedCommit?.Sha,
                message);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task DeleteTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DeleteTagAsync(State.RepositoryPath, tagName);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void ShowStashModal()
    {
        UpdateState(s => s with { IsStashModalVisible = true, NewStashMessageInput = string.Empty });
    }

    [ReactiveCommand]
    private void HideStashModal()
    {
        UpdateState(s => s with { IsStashModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteCreateStash()
    {
        UpdateState(s => s with { IsLoading = true, IsStashModalVisible = false });
        try
        {
            string message = string.IsNullOrWhiteSpace(State.NewStashMessageInput)
                ? $"Stash at {DateTime.Now:yyyy-MM-dd HH:mm}"
                : State.NewStashMessageInput.Trim();
            await _gitService.CreateStashAsync(State.RepositoryPath, message);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ApplyStash(int stashIndex)
    {
        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.ApplyStashAsync(State.RepositoryPath, stashIndex);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task PopStash(int stashIndex)
    {
        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.PopStashAsync(State.RepositoryPath, stashIndex);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task DropStash(int stashIndex)
    {
        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DropStashAsync(State.RepositoryPath, stashIndex);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ShowSettingsModal()
    {
        try
        {
            GitConfigInfo config = await _gitService.GetGitConfigAsync(State.RepositoryPath);
            UpdateState(s => s with
            {
                IsSettingsModalVisible = true,
                ConfigUserNameInput = config.UserName,
                ConfigUserEmailInput = config.UserEmail
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void HideSettingsModal()
    {
        UpdateState(s => s with { IsSettingsModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteSaveSettings()
    {
        UpdateState(s => s with { IsSettingsModalVisible = false });
        try
        {
            await _gitService.SetGitConfigAsync(
                State.RepositoryPath,
                State.ConfigUserNameInput.Trim(),
                State.ConfigUserEmailInput.Trim());
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task StageAll()
    {
        try
        {
            await StageAllUnstaged();
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            UpdateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task UnstageAll()
    {
        try
        {
            await UnstageAllStaged();
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            UpdateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task CherryPickCommit(CommitSummary? commit)
    {
        CommitSummary? target = commit ?? State.SelectedCommit;
        if (target == null)
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.CherryPickCommitAsync(State.RepositoryPath, target.Sha);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task RevertCommit(CommitSummary? commit)
    {
        CommitSummary? target = commit ?? State.SelectedCommit;
        if (target == null)
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.RevertCommitAsync(State.RepositoryPath, target.Sha);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task RequestResetBranch(GitResetType resetType)
    {
        if (State.SelectedCommit == null)
        {
            return;
        }

        if (resetType == GitResetType.Hard)
        {
            UpdateState(s => s with
            {
                IsResetConfirmModalVisible = true,
                PendingResetType = resetType,
                PendingResetCommit = State.SelectedCommit
            });
            return;
        }

        await ExecuteDirectReset(resetType, State.SelectedCommit);
    }

    [ReactiveCommand]
    private async Task ExecuteResetBranch()
    {
        if (State.PendingResetCommit == null)
        {
            return;
        }

        UpdateState(s => s with { IsResetConfirmModalVisible = false });
        await ExecuteDirectReset(State.PendingResetType, State.PendingResetCommit);
    }

    [ReactiveCommand]
    private void CancelResetBranch()
    {
        UpdateState(s => s with { IsResetConfirmModalVisible = false, PendingResetCommit = null });
    }

    [ReactiveCommand]
    private void CreateBranchFromCommit(CommitSummary? commit)
    {
        CommitSummary? target = commit ?? State.SelectedCommit;
        if (target != null)
        {
            UpdateState(s => s with
            {
                SelectedCommit = target,
                IsCreateBranchModalVisible = true,
                NewBranchNameInput = string.Empty
            });
        }
    }

    [ReactiveCommand]
    private void CreateTagFromCommit(CommitSummary? commit)
    {
        CommitSummary? target = commit ?? State.SelectedCommit;
        if (target != null)
        {
            UpdateState(s => s with
            {
                SelectedCommit = target,
                IsCreateTagModalVisible = true,
                NewTagNameInput = string.Empty,
                NewTagMessageInput = string.Empty
            });
        }
    }

    private async Task ExecuteDirectReset(GitResetType resetType, CommitSummary commit)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.ResetBranchToCommitAsync(State.RepositoryPath, commit.Sha, resetType);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    private async Task StageAllUnstaged()
    {
        foreach (var change in State.UnstagedChanges)
        {
            await _gitService.StageFileAsync(State.RepositoryPath, change.Path);
        }
    }

    private async Task UnstageAllStaged()
    {
        foreach (var change in State.StagedChanges)
        {
            await _gitService.UnstageFileAsync(State.RepositoryPath, change.Path);
        }
    }

    [ReactiveCommand]
    private void ShowMergeModal(string? branchName = null)
    {
        string? targetBranch = branchName ?? State.Branches.FirstOrDefault(b => !b.IsCurrentHead)?.Name;
        UpdateState(s => s with
        {
            IsMergeModalVisible = true,
            SelectedMergeBranch = targetBranch,
            MergeFastForwardMode = GitMergeFastForwardMode.FastForward
        });
    }

    [ReactiveCommand]
    private void HideMergeModal()
    {
        UpdateState(s => s with { IsMergeModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteMerge()
    {
        if (string.IsNullOrWhiteSpace(State.SelectedMergeBranch))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, IsMergeModalVisible = false });
        try
        {
            await _gitService.MergeBranchAsync(State.RepositoryPath, State.SelectedMergeBranch, State.MergeFastForwardMode);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task StartInteractiveRebase(CommitSummary? commit)
    {
        CommitSummary? target = commit ?? State.SelectedCommit;
        if (target == null)
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            IReadOnlyList<RebaseStep> steps = await _gitService.GetInteractiveRebaseStepsAsync(State.RepositoryPath, target.Sha);
            var stepsList = steps.ToImmutableList();

            UpdateState(s => s with
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
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void HideInteractiveRebaseModal()
    {
        UpdateState(s => s with
        {
            IsInteractiveRebaseModalVisible = false,
            RebaseSteps = ImmutableList<RebaseStep>.Empty,
            SelectedRebaseStep = null
        });
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

        UpdateState(s => s with { IsLoading = true, IsInteractiveRebaseModalVisible = false });
        try
        {
            await _gitService.ExecuteInteractiveRebaseAsync(State.RepositoryPath, State.RebaseUpstreamSha, State.RebaseSteps);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ContinueOperation()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            if (State.OperationStatus.CurrentOperation == RepositoryOperationType.Merge)
            {
                await _gitService.ContinueMergeAsync(State.RepositoryPath);
            }
            else if (State.OperationStatus.CurrentOperation == RepositoryOperationType.Rebase)
            {
                await _gitService.ContinueRebaseAsync(State.RepositoryPath);
            }

            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task AbortOperation()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            if (State.OperationStatus.CurrentOperation == RepositoryOperationType.Merge)
            {
                await _gitService.AbortMergeAsync(State.RepositoryPath);
            }
            else if (State.OperationStatus.CurrentOperation == RepositoryOperationType.Rebase)
            {
                await _gitService.AbortRebaseAsync(State.RepositoryPath);
            }

            await LoadRepositoryData();
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

    private static ImmutableList<CommitSummary> FilterCommits(ImmutableList<CommitSummary> commits, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return commits;
        }

        return commits
            .Where(c => c.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.Sha.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToImmutableList();
    }

    private void HandleRemoteOperationException(Exception ex, RemoteOperationPending pendingOperation)
    {
        if (IsAuthenticationException(ex))
        {
            PromptForCredentials(pendingOperation);
            return;
        }

        UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
    }

    private static bool IsAuthenticationException(Exception ex)
    {
        string message = ex.Message.ToLowerInvariant();
        return message.Contains("401") ||
               message.Contains("auth") ||
               message.Contains("credential") ||
               message.Contains("publickey") ||
               message.Contains("permission denied") ||
               message.Contains("password") ||
               message.Contains("passphrase") ||
               message.Contains("user cancelled") ||
               message.Contains("unauthorized");
    }

    private void PromptForCredentials(RemoteOperationPending operation)
    {
        var credType = DetermineDefaultCredentialType();
        UpdateState(s => s with
        {
            IsLoading = false,
            PendingRemoteOperation = operation,
            CredentialsAuthType = credType,
            CredentialsUsernameInput = string.Empty,
            CredentialsPasswordInput = string.Empty,
            CredentialsErrorMessage = null,
            IsCredentialsModalVisible = true
        });
    }

    private GitCredentialType DetermineDefaultCredentialType()
    {
        var remote = State.Remotes.FirstOrDefault();
        if (remote != null)
        {
            string url = remote.Url.Trim();
            if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase))
            {
                return GitCredentialType.SshKeyPassphrase;
            }
        }

        return GitCredentialType.Https;
    }

    private GitCredentials BuildCredentialsFromState()
    {
        if (State.CredentialsAuthType == GitCredentialType.Https)
        {
            return new GitCredentials
            {
                Type = GitCredentialType.Https,
                Username = State.CredentialsUsernameInput.Trim(),
                PasswordOrToken = State.CredentialsPasswordInput
            };
        }

        return new GitCredentials
        {
            Type = GitCredentialType.SshKeyPassphrase,
            Passphrase = State.CredentialsPasswordInput
        };
    }

    private async Task ExecutePendingRemoteOperationAsync(RemoteOperationPending operation, GitCredentials? credentials)
    {
        switch (operation)
        {
            case RemoteOperationPending.Fetch:
                await _gitService.FetchAsync(State.RepositoryPath, credentials: credentials);
                break;
            case RemoteOperationPending.Pull:
                await _gitService.PullAsync(State.RepositoryPath, credentials: credentials);
                break;
            case RemoteOperationPending.Push:
                await _gitService.PushAsync(State.RepositoryPath, credentials: credentials);
                break;
            default:
                break;
        }
    }
}
