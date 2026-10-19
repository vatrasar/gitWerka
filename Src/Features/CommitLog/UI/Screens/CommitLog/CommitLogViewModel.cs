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
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.Resources;
using GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;

/// <summary>
/// ViewModel for the CommitLog workspace screen displaying history graph, branch pointers, commit details, and diffs.
/// </summary>
public partial class CommitLogViewModel : ViewModelBase<CommitLogState>, IRoutableViewModel
{
    private readonly IGitService _gitService;
    private readonly ISystemLauncherService _systemLauncherService;
    private readonly IProfileRepository? _profileRepository;
    private readonly IRecentRepositoriesRepository? _recentRepositoriesRepository;
    private readonly IAiCommitMessageService? _aiCommitMessageService;
    private readonly IAiConfigurationRepository? _aiConfigurationRepository;
    private readonly IApplicationSettingsRepository? _applicationSettingsRepository;
    private readonly IAppStatusService? _appStatusService;

    public string? UrlPathSegment => "commit-log";

    public IScreen HostScreen { get; }

    public CommitLogViewModel(
        IScreen hostScreen,
        string repositoryPath,
        IGitService gitService,
        ISystemLauncherService? systemLauncherService = null,
        IProfileRepository? profileRepository = null,
        IRecentRepositoriesRepository? recentRepositoriesRepository = null,
        IAiCommitMessageService? aiCommitMessageService = null,
        IAiConfigurationRepository? aiConfigurationRepository = null,
        IApplicationSettingsRepository? applicationSettingsRepository = null,
        IAppStatusService? appStatusService = null) : base(new CommitLogState
        {
            RepositoryPath = repositoryPath,
            RepositoryName = new DirectoryInfo(repositoryPath).Name
        })
    {
        HostScreen = hostScreen;
        _gitService = gitService;
        _systemLauncherService = systemLauncherService ?? new GitWerka.Infrastructure.Services.SystemLauncherService();
        _profileRepository = profileRepository ?? Locator.Current.GetService<IProfileRepository>();
        _recentRepositoriesRepository = recentRepositoriesRepository ?? Locator.Current.GetService<IRecentRepositoriesRepository>();
        _aiCommitMessageService = aiCommitMessageService ?? Locator.Current.GetService<IAiCommitMessageService>();
        _aiConfigurationRepository = aiConfigurationRepository ?? Locator.Current.GetService<IAiConfigurationRepository>();
        _applicationSettingsRepository = applicationSettingsRepository ?? Locator.Current.GetService<IApplicationSettingsRepository>();
        _appStatusService = appStatusService ?? Locator.Current.GetService<IAppStatusService>();

        LoadRepositoryDataCommand.Execute().Subscribe();
        LoadApplicationSettingsCommand.Execute().Subscribe();
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
    /// Updates the custom repository name input for the Add to Favorites modal.
    /// Usages: CommitLogView favorite name text box.
    /// </summary>
    /// <param name="name">New custom repository name input.</param>
    public void UpdateNewFavoriteNameInput(string name)
    {
        UpdateState(s => s with { NewFavoriteNameInput = name ?? string.Empty });
    }

    /// <summary>
    /// Updates the target group name for the repository being added or edited in favorites.
    /// Usages: CommitLogView favorite group text box.
    /// </summary>
    /// <param name="group">New group name input.</param>
    public void UpdateNewFavoriteGroupInput(string group)
    {
        UpdateState(s => s with { NewFavoriteGroupInput = group ?? string.Empty });
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
    /// Updates the remote name input for adding a new remote.
    /// Usages: CommitLogView create remote modal.
    /// </summary>
    /// <param name="text">New remote name text.</param>
    public void UpdateNewRemoteNameInput(string text)
    {
        UpdateState(s => s with { NewRemoteNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates the remote URL input for adding a new remote.
    /// Usages: CommitLogView create remote modal.
    /// </summary>
    /// <param name="text">New remote URL text.</param>
    public void UpdateNewRemoteUrlInput(string text)
    {
        UpdateState(s => s with { NewRemoteUrlInput = text ?? string.Empty });
    }

    /// <summary>
    /// Selects an active remote in the remotes list.
    /// Usages: CommitLogView remotes list.
    /// </summary>
    /// <param name="remote">Remote information or null.</param>
    public void SelectRemote(RemoteInfo? remote)
    {
        UpdateState(s => s with { SelectedRemote = remote });
    }

    /// <summary>
    /// Sets the currently selected stash entry.
    /// Usages: CommitLogView stashes list.
    /// </summary>
    /// <param name="stash">Stash information or null.</param>
    public void SelectStash(StashInfo? stash)
    {
        UpdateState(s => s with { SelectedStash = stash });
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
    /// Updates default projects directory input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Default directory path string.</param>
    public void UpdateDefaultProjectsDirectoryInput(string text)
    {
        UpdateState(s => s with { DefaultProjectsDirectoryInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates maximum visible branches input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Maximum visible branches count string.</param>
    public void UpdateMaxVisibleBranchesInput(string text)
    {
        UpdateState(s => s with { MaxVisibleBranchesInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates OpenRouter API key configuration input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">API key string.</param>
    public void UpdateAiApiKeyInput(string text)
    {
        UpdateState(s => s with { AiApiKeyInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates AI model identifier input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Model identifier.</param>
    public void UpdateAiModelInput(string text)
    {
        UpdateState(s => s with { AiModelInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates requested commit style input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Commit style instructions.</param>
    public void UpdateAiCommitStyleInput(string text)
    {
        UpdateState(s => s with { AiCommitStyleInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates token limit input in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Token limit number string.</param>
    public void UpdateAiTokenLimitInput(string text)
    {
        UpdateState(s => s with { AiTokenLimitInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates project description input for the active repository in settings modal.
    /// Usages: CommitLogView settings modal.
    /// </summary>
    /// <param name="text">Project description context.</param>
    public void UpdateAiProjectDescriptionInput(string text)
    {
        UpdateState(s => s with { AiProjectDescriptionInput = text ?? string.Empty });
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

        if (commit.IsWip)
        {
            await SelectWipCommitAsync(commit);
            return;
        }

        CommitDetail? detail = await _gitService.GetCommitDetailAsync(State.RepositoryPath, commit.Sha);

        UpdateState(s => s with { SelectedCommitDetail = detail });

        if (detail != null && detail.ChangedFiles.Count > 0)
        {
            await SelectFileDiffAsync(detail.ChangedFiles[0]);
        }
    }

    private async Task SelectWipCommitAsync(CommitSummary commit)
    {
        var changedFiles = State.WorkingTreeChanges
            .GroupBy(c => c.Path)
            .Select(g => new FileDiffSummary
            {
                Path = g.Key,
                Status = g.First().Status,
                LinesAdded = 0,
                LinesDeleted = 0
            })
            .ToImmutableList();

        CommitDetail wipDetail = new()
        {
            Sha = "WIP",
            ShortSha = string.Empty,
            Message = "Uncommitted changes in working directory",
            AuthorName = "Working Directory",
            AuthorEmail = string.Empty,
            AuthorDate = DateTimeOffset.Now,
            CommitterName = "Working Directory",
            CommitterEmail = string.Empty,
            CommitterDate = DateTimeOffset.Now,
            ParentShas = commit.ParentShas,
            ChangedFiles = changedFiles
        };

        UpdateState(s => s with
        {
            SelectedCommitDetail = wipDetail,
            ActiveSidebarTab = CommitLogSidebarTab.WorkingTree
        });

        if (changedFiles.Count > 0)
        {
            await SelectFileDiffAsync(changedFiles[0]);
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
            int maxVisibleBranches = await ResolveMaxVisibleBranchesAsync();
            IReadOnlyList<CommitSummary> commits = await _gitService.GetCommitLogAsync(
                State.RepositoryPath,
                maxCount: 300,
                maxBranches: maxVisibleBranches);
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
                OperationStatus = opStatus,
                MergeSourceBranch = ExtractMergeSourceBranch(opStatus.Message),
                MergeTargetBranch = info.CurrentBranch
            });

            await LoadProfilesDataAsync();
            await InitializeFavoriteAndRecentStateAsync();

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

    /// <summary>
    /// Opens the 3-panel conflict resolution screen for the selected conflicted or resolved file.
    /// Usages: CommitLogView conflicted and resolved file lists.
    /// </summary>
    /// <param name="file">The working tree file change.</param>
    public void OpenConflictResolver(WorkingTreeChange? file)
    {
        if (file == null)
        {
            return;
        }

        HostScreen.Router.Navigate.Execute(new ConflictResolverViewModel(HostScreen, _gitService, State.RepositoryPath, file.Path)).Subscribe();
    }

    [ReactiveCommand]
    private async Task MarkAllResolved()
    {
        try
        {
            await _gitService.MarkAllConflictsResolvedAsync(State.RepositoryPath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            RepositoryOperationStatus opStatus = await _gitService.GetRepositoryOperationStatusAsync(State.RepositoryPath);

            UpdateState(s => s with
            {
                WorkingTreeChanges = changes.ToImmutableList(),
                OperationStatus = opStatus
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private static string ExtractMergeSourceBranch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "remote";
        }

        var match = System.Text.RegularExpressions.Regex.Match(message, @"(?:branch|remote-tracking branch)\s+'([^']+)'");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return "remote";
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
    private async Task ExecuteGenerateAiCommitMessage()
    {
        if (State.IsGeneratingAiCommit)
        {
            return;
        }

        if (_aiCommitMessageService == null)
        {
            UpdateState(s => s with
            {
                AiErrorMessage = "AI commit message service is not available.",
                IsAiErrorSettingsActionVisible = false
            });
            return;
        }

        UpdateState(s => s with
        {
            IsGeneratingAiCommit = true,
            AiErrorMessage = null,
            IsAiErrorSettingsActionVisible = false
        });

        try
        {
            string proposal = await _aiCommitMessageService.GenerateCommitMessageAsync(State.RepositoryPath);
            string sanitizedProposal = IAiCommitMessageService.TruncateCommitSubject(proposal);
            UpdateState(s => s with
            {
                CommitMessageInput = sanitizedProposal,
                IsGeneratingAiCommit = false,
                AiErrorMessage = null,
                IsAiErrorSettingsActionVisible = false
            });
        }
        catch (Exception ex)
        {
            bool isMissingApiKey = ex.Message.Contains("API key", StringComparison.OrdinalIgnoreCase);

            UpdateState(s => s with
            {
                IsGeneratingAiCommit = false,
                AiErrorMessage = ex.Message,
                IsAiErrorSettingsActionVisible = isMissingApiKey
            });
        }
    }

    [ReactiveCommand]
    private void DismissAiError()
    {
        UpdateState(s => s with
        {
            AiErrorMessage = null,
            IsAiErrorSettingsActionVisible = false
        });
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
        _appStatusService?.SetInProgress(CommitLogStrings.CommittingChanges);

        try
        {
            string authorName = !string.IsNullOrWhiteSpace(State.ActiveProfile?.Author?.Name)
                ? State.ActiveProfile.Author.Name
                : "GitWerka User";
            string authorEmail = !string.IsNullOrWhiteSpace(State.ActiveProfile?.Author?.Email)
                ? State.ActiveProfile.Author.Email
                : "user@gitwerka.local";

            await _gitService.CreateCommitAsync(
                State.RepositoryPath,
                State.CommitMessageInput.Trim(),
                authorName,
                authorEmail,
                State.IsAmendCommit);

            UpdateState(s => s with
            {
                CommitMessageInput = string.Empty,
                IsAmendCommit = false
            });
            await LoadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.CommitSucceeded);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
            _appStatusService?.SetError($"{CommitLogStrings.CommitFailed}: {ex.Message}");
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
        _appStatusService?.SetInProgress(CommitLogStrings.FetchingRemotes);
        try
        {
            await _gitService.FetchAsync(State.RepositoryPath);
            await LoadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.FetchSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Fetch);
            _appStatusService?.SetError($"{CommitLogStrings.FetchFailed}: {ex.Message}");
        }
    }

    [ReactiveCommand]
    private async Task AutoFetch()
    {
        if (State.IsLoading || State.Remotes.Count == 0 || IsAnyModalOpen())
        {
            return;
        }

        try
        {
            GitCredentials? credentials = null;
            if (State.ActiveProfile?.Credentials != null)
            {
                credentials = new GitCredentials
                {
                    Type = GitCredentialType.Https,
                    Username = State.ActiveProfile.Credentials.Username,
                    PasswordOrToken = State.ActiveProfile.Credentials.AccessToken
                };
            }

            await _gitService.FetchAsync(State.RepositoryPath, credentials: credentials);
            await LoadRepositoryData();
        }
        catch
        {
        }
    }

    private bool IsAnyModalOpen()
    {
        return State.IsCreateBranchModalVisible ||
               State.IsMergeModalVisible ||
               State.IsInteractiveRebaseModalVisible ||
               State.IsCreateRemoteModalVisible ||
               State.IsCreateTagModalVisible ||
               State.IsStashModalVisible ||
               State.IsSettingsModalVisible ||
               State.IsResetConfirmModalVisible ||
               State.IsCredentialsModalVisible;
    }

    [ReactiveCommand]
    private async Task Pull()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.PullingChanges);
        try
        {
            await _gitService.PullAsync(State.RepositoryPath);
            await LoadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.PullSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Pull);
            _appStatusService?.SetError($"{CommitLogStrings.PullFailed}: {ex.Message}");
        }
    }

    [ReactiveCommand]
    private async Task Push()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.PushingChanges);
        try
        {
            GitCredentials? credentials = null;
            if (State.ActiveProfile?.Credentials != null)
            {
                credentials = new GitCredentials
                {
                    Type = GitCredentialType.Https,
                    Username = State.ActiveProfile.Credentials.Username,
                    PasswordOrToken = State.ActiveProfile.Credentials.AccessToken
                };
            }

            await _gitService.PushAsync(State.RepositoryPath, credentials: credentials);
            await LoadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.PushSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Push);
            _appStatusService?.SetError($"{CommitLogStrings.PushFailed}: {ex.Message}");
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
        _appStatusService?.SetReady();
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
        SetRemoteOperationInProgress(pending);

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
            SetRemoteOperationSuccess(pending);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                CredentialsErrorMessage = ex.Message
            });
            SetRemoteOperationFailed(pending, ex.Message);
        }
    }

    private void SetRemoteOperationInProgress(RemoteOperationPending operation)
    {
        switch (operation)
        {
            case RemoteOperationPending.Push:
                _appStatusService?.SetInProgress(CommitLogStrings.PushingChanges);
                break;
            case RemoteOperationPending.Pull:
                _appStatusService?.SetInProgress(CommitLogStrings.PullingChanges);
                break;
            case RemoteOperationPending.Fetch:
                _appStatusService?.SetInProgress(CommitLogStrings.FetchingRemotes);
                break;
            default:
                break;
        }
    }

    private void SetRemoteOperationSuccess(RemoteOperationPending operation)
    {
        switch (operation)
        {
            case RemoteOperationPending.Push:
                _appStatusService?.SetSuccess(CommitLogStrings.PushSucceeded);
                break;
            case RemoteOperationPending.Pull:
                _appStatusService?.SetSuccess(CommitLogStrings.PullSucceeded);
                break;
            case RemoteOperationPending.Fetch:
                _appStatusService?.SetSuccess(CommitLogStrings.FetchSucceeded);
                break;
            default:
                break;
        }
    }

    private void SetRemoteOperationFailed(RemoteOperationPending operation, string errorMessage)
    {
        switch (operation)
        {
            case RemoteOperationPending.Push:
                _appStatusService?.SetError($"{CommitLogStrings.PushFailed}: {errorMessage}");
                break;
            case RemoteOperationPending.Pull:
                _appStatusService?.SetError($"{CommitLogStrings.PullFailed}: {errorMessage}");
                break;
            case RemoteOperationPending.Fetch:
                _appStatusService?.SetError($"{CommitLogStrings.FetchFailed}: {errorMessage}");
                break;
            default:
                break;
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
    private void ShowCreateRemoteModal()
    {
        UpdateState(s => s with
        {
            IsCreateRemoteModalVisible = true,
            NewRemoteNameInput = string.Empty,
            NewRemoteUrlInput = string.Empty
        });
    }

    [ReactiveCommand]
    private void HideCreateRemoteModal()
    {
        UpdateState(s => s with { IsCreateRemoteModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteCreateRemote()
    {
        if (string.IsNullOrWhiteSpace(State.NewRemoteNameInput) || string.IsNullOrWhiteSpace(State.NewRemoteUrlInput))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, IsCreateRemoteModalVisible = false });
        try
        {
            await _gitService.AddRemoteAsync(
                State.RepositoryPath,
                State.NewRemoteNameInput.Trim(),
                State.NewRemoteUrlInput.Trim());
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task DeleteRemote(string remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.RemoveRemoteAsync(State.RepositoryPath, remoteName);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task FetchRemote(string remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.FetchAsync(State.RepositoryPath, remoteName);
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Fetch);
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
    private async Task LoadApplicationSettings()
    {
        if (_applicationSettingsRepository == null)
        {
            return;
        }

        ApplicationSettings settings = await _applicationSettingsRepository.GetSettingsAsync();
        UpdateState(s => s with
        {
            DefaultProjectsDirectory = settings.DefaultProjectsDirectory ?? string.Empty,
            DefaultProjectsDirectoryInput = settings.DefaultProjectsDirectory ?? string.Empty
        });
    }

    [ReactiveCommand]
    private async Task ShowSettingsModal()
    {
        try
        {
            GitConfigInfo config = await _gitService.GetGitConfigAsync(State.RepositoryPath);
            (AiConfiguration aiConfig, string? projectDesc) = await LoadAiSettingsAsync();
            ApplicationSettings appSettings = _applicationSettingsRepository != null
                ? await _applicationSettingsRepository.GetSettingsAsync()
                : new ApplicationSettings { DefaultProjectsDirectory = State.DefaultProjectsDirectory };

            UpdateState(s => s with
            {
                IsSettingsModalVisible = true,
                ConfigUserNameInput = config.UserName,
                ConfigUserEmailInput = config.UserEmail,
                DefaultProjectsDirectory = appSettings.DefaultProjectsDirectory,
                DefaultProjectsDirectoryInput = appSettings.DefaultProjectsDirectory,
                MaxVisibleBranchesInput = (appSettings.MaxVisibleBranches > 0 ? appSettings.MaxVisibleBranches : 5).ToString(),
                AiApiKeyInput = aiConfig.ApiKey ?? string.Empty,
                AiModelInput = aiConfig.Model,
                AiCommitStyleInput = aiConfig.CommitStyle,
                AiTokenLimitInput = aiConfig.TokenLimit.ToString(),
                AiProjectDescriptionInput = projectDesc ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private async Task<int> ResolveMaxVisibleBranchesAsync()
    {
        if (_applicationSettingsRepository == null)
        {
            return 5;
        }

        ApplicationSettings appSettings = await _applicationSettingsRepository.GetSettingsAsync();
        return appSettings.MaxVisibleBranches > 0 ? appSettings.MaxVisibleBranches : 5;
    }

    private async Task<(AiConfiguration, string?)> LoadAiSettingsAsync()
    {
        if (_aiConfigurationRepository == null)
        {
            return (new AiConfiguration(), null);
        }

        AiConfiguration aiConfig = await _aiConfigurationRepository.GetConfigurationAsync();
        string? projectDesc = await _aiConfigurationRepository.GetProjectDescriptionAsync(State.RepositoryPath);

        return (aiConfig, projectDesc);
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

            string trimmedDir = State.DefaultProjectsDirectoryInput?.Trim() ?? string.Empty;
            if (_applicationSettingsRepository != null)
            {
                int maxBranches = 5;
                if (int.TryParse(State.MaxVisibleBranchesInput, out int parsedBranches) && parsedBranches > 0)
                {
                    maxBranches = parsedBranches;
                }

                await _applicationSettingsRepository.SaveSettingsAsync(new ApplicationSettings
                {
                    MaxVisibleBranches = maxBranches,
                    DefaultProjectsDirectory = trimmedDir
                });
            }

            UpdateState(s => s with
            {
                DefaultProjectsDirectory = trimmedDir
            });

            await SaveAiSettingsAsync();
            await LoadRepositoryData();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private async Task SaveAiSettingsAsync()
    {
        if (_aiConfigurationRepository == null)
        {
            return;
        }

        int.TryParse(State.AiTokenLimitInput, out int tokenLimit);
        if (tokenLimit <= 0)
        {
            tokenLimit = 4000;
        }

        AiConfiguration updatedAiConfig = new()
        {
            ApiKey = string.IsNullOrWhiteSpace(State.AiApiKeyInput) ? null : State.AiApiKeyInput.Trim(),
            Model = string.IsNullOrWhiteSpace(State.AiModelInput) ? "google/gemini-2.5-flash" : State.AiModelInput.Trim(),
            CommitStyle = string.IsNullOrWhiteSpace(State.AiCommitStyleInput) ? "Conventional Commits: <type>(<scope>): <subject>" : State.AiCommitStyleInput.Trim(),
            TokenLimit = tokenLimit
        };

        await _aiConfigurationRepository.SaveConfigurationAsync(updatedAiConfig);
        await _aiConfigurationRepository.SaveProjectDescriptionAsync(
            State.RepositoryPath,
            string.IsNullOrWhiteSpace(State.AiProjectDescriptionInput) ? null : State.AiProjectDescriptionInput.Trim());
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
            .Where(c => c.IsWip ||
                        c.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
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

    /// <summary>
    /// Selects an active Git profile for this repository and persists the selection.
    /// Usages: CommitLogView profile dropdown selection.
    /// </summary>
    /// <param name="profile">The profile to activate or null to clear.</param>
    public async Task SelectProfileAsync(GitProfile? profile)
    {
        UpdateState(s => s with { ActiveProfile = profile });

        if (_profileRepository == null)
        {
            return;
        }

        if (profile != null)
        {
            await _profileRepository.SetActiveProfileForRepositoryAsync(State.RepositoryPath, profile.Id);
            await ConfigureGitAuthorIfAvailableAsync(profile);
            return;
        }

        await _profileRepository.ClearActiveProfileForRepositoryAsync(State.RepositoryPath);
    }

    /// <summary>
    /// Navigates to the ProfileManagement screen.
    /// Usages: CommitLogView profile menu 'Manage profiles' action.
    /// </summary>
    public void NavigateToProfileManagement()
    {
        if (_profileRepository == null)
        {
            return;
        }

        HostScreen.Router.Navigate.Execute(
            new ProfileManagementViewModel(HostScreen, _profileRepository, State.RepositoryPath)
        ).Subscribe();
    }

    [ReactiveCommand]
    private async Task SelectProfile(GitProfile? profile)
    {
        await SelectProfileAsync(profile);
    }

    [ReactiveCommand]
    private void NavigateToProfiles()
    {
        NavigateToProfileManagement();
    }

    private async Task ConfigureGitAuthorIfAvailableAsync(GitProfile profile)
    {
        if (profile.Author == null)
        {
            return;
        }

        try
        {
            await _gitService.SetGitConfigAsync(State.RepositoryPath, profile.Author.Name, profile.Author.Email);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Reloads all profiles and the active profile from the repository.
    /// Usages: HostViewModel navigation sync, HostViewModel refresh.
    /// </summary>
    /// <returns>Task tracking async profiles reload.</returns>
    public async Task ReloadProfilesAsync()
    {
        await LoadProfilesDataAsync();
        if (State.ActiveProfile != null)
        {
            await ConfigureGitAuthorIfAvailableAsync(State.ActiveProfile);
        }
    }

    private async Task LoadProfilesDataAsync()
    {
        if (_profileRepository == null)
        {
            return;
        }

        try
        {
            IReadOnlyList<GitProfile> profiles = await _profileRepository.GetAllProfilesAsync();
            GitProfile? activeProfile = await _profileRepository.GetActiveProfileForRepositoryAsync(State.RepositoryPath);

            UpdateState(s => s with
            {
                AvailableProfiles = profiles.ToImmutableList(),
                ActiveProfile = activeProfile
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes favorite repositories and the most recently opened non-favorite repository from storage.
    /// Usages: CommitLogView favorites dock, repository open handlers.
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

    private async Task InitializeFavoriteAndRecentStateAsync()
    {
        if (_recentRepositoriesRepository == null || string.IsNullOrWhiteSpace(State.RepositoryPath))
        {
            return;
        }

        try
        {
            RecentRepository? existing = await _recentRepositoriesRepository.GetRepositoryByPathAsync(State.RepositoryPath);
            string folderName = new DirectoryInfo(State.RepositoryPath).Name;
            bool isFavorite = existing?.IsFavorite ?? false;
            string? customName = existing?.CustomName;
            string displayName = ResolveRepositoryDisplayName(isFavorite, customName, folderName);

            UpdateState(s => s with { RepositoryName = displayName });

            await _recentRepositoriesRepository.AddOrUpdateRecentRepositoryAsync(new RecentRepository
            {
                Path = State.RepositoryPath,
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
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private static string ResolveRepositoryDisplayName(bool isFavorite, string? customName, string fallbackName)
    {
        if (isFavorite && !string.IsNullOrWhiteSpace(customName))
        {
            return customName;
        }

        return fallbackName;
    }

    [ReactiveCommand]
    private async Task ToggleFavoritesDock()
    {
        bool nextOpen = !State.IsFavoritesDockOpen;
        if (nextOpen)
        {
            await RefreshFavoritesDockDataAsync();
        }

        UpdateState(s => s with { IsFavoritesDockOpen = nextOpen });
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
        string targetPath = string.IsNullOrWhiteSpace(path) ? State.RepositoryPath : path;
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

        if (string.Equals(State.TargetFavoritePath, State.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            UpdateState(s => s with { RepositoryName = nameToUse });
        }

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

        if (string.Equals(path, State.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            string fallbackName = new DirectoryInfo(State.RepositoryPath).Name;
            UpdateState(s => s with { RepositoryName = fallbackName });
        }

        await RefreshFavoritesDockDataAsync();
    }

    [ReactiveCommand]
    private void OpenRepositoryFromDock(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        UpdateState(s => s with { IsFavoritesDockOpen = false });

        if (string.Equals(path, State.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_gitService.IsValidRepository(path))
        {
            UpdateState(s => s with { ErrorMessage = "Selected directory is not a valid Git repository." });
            return;
        }

        HostScreen.Router.Navigate.Execute(new CommitLogViewModel(
            HostScreen,
            path,
            _gitService,
            _systemLauncherService,
            _profileRepository,
            _recentRepositoriesRepository,
            _aiCommitMessageService,
            _aiConfigurationRepository,
            _applicationSettingsRepository,
            _appStatusService
        )).Subscribe();
    }
}
