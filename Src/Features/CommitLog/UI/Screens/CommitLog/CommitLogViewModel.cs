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
using GitWerka.Features.CommitLog.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.InteractiveRebaseModal;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.SettingsModal;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;
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

    private readonly CommitLogFavoritesCoordinator _favoritesCoordinator;
    private readonly CommitLogMergeRebaseCoordinator _mergeRebaseCoordinator;
    private readonly CommitLogSettingsCoordinator _settingsCoordinator;
    private readonly CommitLogWorkingTreeCoordinator _workingTreeCoordinator;
    private readonly CommitLogRemoteCoordinator _remoteCoordinator;
    private readonly CommitLogBranchTagCoordinator _branchTagCoordinator;
    private readonly CommitLogHistoryActionCoordinator _historyActionCoordinator;

    public string? UrlPathSegment => "commit-log";
    public IScreen HostScreen { get; }
    public FavoritesDockViewModel FavoritesDock { get; private set; } = null!;
    public SettingsModalViewModel SettingsModal { get; private set; } = null!;
    public InteractiveRebaseModalViewModel InteractiveRebaseModal { get; private set; } = null!;
    public GitOperationModalsViewModel GitModals { get; private set; } = null!;
    public WorkingTreePanelViewModel WorkingTreePanel { get; private set; } = null!;
    public IObservable<bool> CanCommit { get; }

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

        _favoritesCoordinator = new CommitLogFavoritesCoordinator(_recentRepositoriesRepository, _gitService, UpdateState, () => State);
        _mergeRebaseCoordinator = new CommitLogMergeRebaseCoordinator(_gitService, UpdateState, () => State);
        _settingsCoordinator = new CommitLogSettingsCoordinator(_gitService, _applicationSettingsRepository, _aiConfigurationRepository, UpdateState, () => State);
        _workingTreeCoordinator = new CommitLogWorkingTreeCoordinator(_gitService, HostScreen, _aiCommitMessageService, _appStatusService, () => State, UpdateState, async () => await LoadRepositoryData());
        _remoteCoordinator = new CommitLogRemoteCoordinator(_gitService, _appStatusService, () => State, UpdateState, async () => await LoadRepositoryData());
        _branchTagCoordinator = new CommitLogBranchTagCoordinator(_gitService, () => State, UpdateState, async () => await LoadRepositoryData(), async () => await _settingsCoordinator.ResolveMaxVisibleBranchesAsync());
        _historyActionCoordinator = new CommitLogHistoryActionCoordinator(_gitService, _systemLauncherService, _profileRepository, HostScreen, () => State, UpdateState, async () => await LoadRepositoryData());

        CanCommit = this.WhenAnyValue(x => x.State.ActiveProfile).Select(profile => profile != null);

        InitializeSmartComponents(repositoryPath);

        var componentCoordinator = new CommitLogComponentCoordinator(
            FavoritesDock, SettingsModal, InteractiveRebaseModal, GitModals, WorkingTreePanel,
            _favoritesCoordinator, _mergeRebaseCoordinator, _settingsCoordinator,
            _workingTreeCoordinator, _remoteCoordinator, _branchTagCoordinator, _historyActionCoordinator,
            UpdateState, this.WhenAnyValue(x => x.State), async () => await LoadRepositoryData(), OpenRepository);
        componentCoordinator.Wire();

        LoadRepositoryDataCommand.Execute().Subscribe();
        LoadApplicationSettingsCommand.Execute().Subscribe();
    }

    private void InitializeSmartComponents(string repositoryPath)
    {
        FavoritesDock = new FavoritesDockViewModel(_recentRepositoriesRepository, _gitService, repositoryPath);
        SettingsModal = new SettingsModalViewModel(_gitService, _applicationSettingsRepository, _aiConfigurationRepository, repositoryPath);
        InteractiveRebaseModal = new InteractiveRebaseModalViewModel(_gitService, repositoryPath);
        GitModals = new GitOperationModalsViewModel();
        WorkingTreePanel = new WorkingTreePanelViewModel();
    }

    public void OpenRepository(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath) ||
            string.Equals(repositoryPath, State.RepositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        FavoritesDock.CloseFavoritesDockCommand.Execute().Subscribe();
        UpdateState(s => s with { IsFavoritesDockOpen = false });

        HostScreen.Router.Navigate.Execute(new CommitLogViewModel(
            HostScreen, repositoryPath, _gitService, _systemLauncherService, _profileRepository,
            _recentRepositoriesRepository, _aiCommitMessageService, _aiConfigurationRepository,
            _applicationSettingsRepository, _appStatusService
        )).Subscribe();
    }

    public void UpdateAiApiKeyInput(string text) => _settingsCoordinator.UpdateAiApiKeyInput(text);
    public void UpdateAiModelInput(string text) => _settingsCoordinator.UpdateAiModelInput(text);
    public void UpdateAiCommitStyleInput(string text) => _settingsCoordinator.UpdateAiCommitStyleInput(text);
    public void UpdateAiTokenLimitInput(string text) => _settingsCoordinator.UpdateAiTokenLimitInput(text);
    public void UpdateAiProjectDescriptionInput(string text) => _settingsCoordinator.UpdateAiProjectDescriptionInput(text);
    public void UpdateDefaultProjectsDirectoryInput(string text) => _settingsCoordinator.UpdateDefaultProjectsDirectoryInput(text);
    public void UpdateMaxVisibleBranchesInput(string text) => _settingsCoordinator.UpdateMaxVisibleBranchesInput(text);

    public void UpdateSearchQuery(string query)
    {
        string trimmed = query?.Trim() ?? string.Empty;
        var filtered = CommitLogHistoryHelper.FilterCommits(State.AllCommits, trimmed);
        UpdateState(s => s with { SearchQuery = trimmed, FilteredCommits = filtered });
    }

    public async Task SelectCommitAsync(CommitSummary commit) => await _historyActionCoordinator.SelectCommitAsync(commit);
    public async Task SelectFileDiffAsync(FileDiffSummary file) => await _historyActionCoordinator.SelectFileDiffAsync(file);
    public async Task SelectBranchAsync(BranchListItem? item) => await _branchTagCoordinator.SelectBranchAsync(item, SelectCommitAsync);
    public void SetActiveSidebarTab(CommitLogSidebarTab tab) => UpdateState(s => s with { ActiveSidebarTab = tab });
    public void OpenConflictResolver(WorkingTreeChange? file) => _workingTreeCoordinator.OpenConflictResolver(file);
    public void UpdateCommitMessageInput(string message)
    {
        _workingTreeCoordinator.UpdateCommitMessageInput(message);
        WorkingTreePanel.UpdateCommitMessageInput(message);
    }

    public void ToggleAmendCommit(bool isAmend)
    {
        _workingTreeCoordinator.ToggleAmendCommit(isAmend);
        WorkingTreePanel.SetAmendCommit(isAmend);
    }
    public async Task SelectProfileAsync(GitProfile? profile) => await _historyActionCoordinator.SelectProfileAsync(profile);
    public void NavigateToProfileManagement() => _historyActionCoordinator.NavigateToProfileManagement();
    public async Task ReloadProfilesAsync() => await _historyActionCoordinator.ReloadProfilesAsync();

    public void UpdateNewBranchNameInput(string text)
    {
        _branchTagCoordinator.UpdateNewBranchNameInput(text);
        GitModals.UpdateNewBranchNameInput(text);
    }

    public void UpdateNewTagNameInput(string text)
    {
        _branchTagCoordinator.UpdateNewTagNameInput(text);
        GitModals.UpdateNewTagNameInput(text);
    }

    public void UpdateNewTagMessageInput(string text)
    {
        _branchTagCoordinator.UpdateNewTagMessageInput(text);
        GitModals.UpdateNewTagMessageInput(text);
    }

    public void UpdateCredentialsUsernameInput(string username)
    {
        _remoteCoordinator.UpdateCredentialsUsernameInput(username);
        GitModals.UpdateCredentialsUsernameInput(username);
    }

    public void UpdateCredentialsPasswordInput(string password)
    {
        _remoteCoordinator.UpdateCredentialsPasswordInput(password);
        GitModals.UpdateCredentialsPasswordInput(password);
    }

    public void SetCredentialsAuthType(GitCredentialType authType)
    {
        _remoteCoordinator.SetCredentialsAuthType(authType);
        GitModals.SetCredentialsAuthType(authType);
    }

    public void UpdateNewRemoteNameInput(string text)
    {
        _remoteCoordinator.UpdateNewRemoteNameInput(text);
        GitModals.UpdateNewRemoteNameInput(text);
    }

    public void UpdateNewRemoteUrlInput(string text)
    {
        _remoteCoordinator.UpdateNewRemoteUrlInput(text);
        GitModals.UpdateNewRemoteUrlInput(text);
    }

    public void SelectRemote(RemoteInfo? remote) => _remoteCoordinator.SelectRemote(remote);

    public void SelectStash(StashInfo? stash) => _historyActionCoordinator.SelectStash(stash);

    public void UpdateNewStashMessageInput(string text)
    {
        _historyActionCoordinator.UpdateNewStashMessageInput(text);
        GitModals.UpdateNewStashMessageInput(text);
    }

    public void UpdateNewFavoriteNameInput(string text)
    {
        _favoritesCoordinator.UpdateNewFavoriteNameInput(text);
        FavoritesDock.UpdateNewFavoriteNameInput(text);
    }

    public void UpdateNewFavoriteGroupInput(string text)
    {
        _favoritesCoordinator.UpdateNewFavoriteGroupInput(text);
        FavoritesDock.UpdateNewFavoriteGroupInput(text);
    }

    public async Task RefreshFavoritesDockDataAsync()
    {
        await _favoritesCoordinator.RefreshFavoritesDockDataAsync();
        await FavoritesDock.RefreshFavoritesDockDataAsync();
    }

    public void SetMergeFastForwardMode(GitMergeFastForwardMode mode)
    {
        _mergeRebaseCoordinator.SetMergeFastForwardMode(mode);
        GitModals.SetMergeFastForwardMode(mode);
    }

    public void SelectMergeBranch(string branchName)
    {
        _mergeRebaseCoordinator.SelectMergeBranch(branchName);
        GitModals.SelectMergeBranch(branchName);
    }

    public void SelectRebaseStep(RebaseStep step) => _mergeRebaseCoordinator.SelectRebaseStep(step);

    public void ChangeRebaseStepAction(RebaseStep step, RebaseStepAction action) => _mergeRebaseCoordinator.ChangeRebaseStepAction(step, action);

    public void UpdateRebaseStepMessage(RebaseStep step, string message) => _mergeRebaseCoordinator.UpdateRebaseStepMessage(step, message);

    [ReactiveCommand]
    private async Task LoadRepositoryData()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            GitRepositoryInfo info = await _gitService.GetRepositoryInfoAsync(State.RepositoryPath);
            int maxVisibleBranches = await _settingsCoordinator.ResolveMaxVisibleBranchesAsync();
            IReadOnlyList<BranchInfo> branches = await _gitService.GetBranchesAsync(State.RepositoryPath);
            string? activeBranch = CommitLogHistoryHelper.ResolveActiveBranchName(State.SelectedBranchName, branches);
            IReadOnlyList<CommitSummary> commits = await _gitService.GetCommitLogAsync(
                State.RepositoryPath, maxCount: 300, maxBranches: maxVisibleBranches, branchName: activeBranch);
            IReadOnlyList<TagInfo> tags = await _gitService.GetTagsAsync(State.RepositoryPath);
            IReadOnlyList<StashInfo> stashes = await _gitService.GetStashesAsync(State.RepositoryPath);
            IReadOnlyList<RemoteInfo> remotes = await _gitService.GetRemotesAsync(State.RepositoryPath);
            IReadOnlyList<WorkingTreeChange> workingTree = await _gitService.GetWorkingTreeChangesAsync(State.RepositoryPath);
            RepositoryOperationStatus opStatus = await _gitService.GetRepositoryOperationStatusAsync(State.RepositoryPath);

            var commitList = commits.ToImmutableList();
            var filtered = CommitLogHistoryHelper.FilterCommits(commitList, State.SearchQuery);
            var branchItems = CommitLogHistoryHelper.BuildBranchListItems(branches, maxVisibleBranches);

            UpdateState(s => s with
            {
                IsLoading = false,
                CurrentBranch = info.CurrentBranch,
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
                MergeTargetBranch = info.CurrentBranch
            });

            await _historyActionCoordinator.LoadProfilesDataAsync();
            await _favoritesCoordinator.InitializeFavoriteAndRecentStateAsync();
            await FavoritesDock.RefreshFavoritesDockDataAsync();

            WorkingTreePanel.UpdateWorkingTree(State.WorkingTreeChanges, State.MergeSourceBranch, State.MergeTargetBranch, State.OperationStatus, State.ActiveProfile);
            GitModals.SetAvailableBranches(State.Branches);

            if (filtered.Count > 0 && State.SelectedCommit == null)
            {
                await SelectCommitAsync(filtered[0]);
            }
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand] private void DismissError() => UpdateState(s => s with { ErrorMessage = null });
    [ReactiveCommand] private async Task ToggleFavoritesDock() => await FavoritesDock.ToggleFavoritesDockCommand.Execute().FirstAsync();
    [ReactiveCommand]
    private async Task ShowSettingsModal()
    {
        await _settingsCoordinator.ShowSettingsModalAsync();
        await SettingsModal.ShowSettingsModalCommand.Execute().FirstAsync();
    }

    [ReactiveCommand]
    private async Task LoadApplicationSettings()
    {
        await _settingsCoordinator.LoadApplicationSettingsAsync();
        await SettingsModal.LoadApplicationSettingsAsync();
    }

    [ReactiveCommand]
    private void ShowMergeModal(string? branchName)
    {
        _mergeRebaseCoordinator.ShowMergeModal(branchName);
        GitModals.ShowMergeModal(branchName);
    }

    [ReactiveCommand]
    private async Task StartInteractiveRebase(CommitSummary? commit)
    {
        await _mergeRebaseCoordinator.StartInteractiveRebaseAsync(commit);
        var s = State;
        InteractiveRebaseModal.ShowModal(s.RepositoryPath, s.RebaseUpstreamSha ?? string.Empty, s.RebaseUpstreamShortSha ?? string.Empty, s.RebaseSteps);
    }

    [ReactiveCommand] private async Task AbortOperation() => await _mergeRebaseCoordinator.AbortOperationAsync(async () => await LoadRepositoryData());
    [ReactiveCommand] private async Task ContinueOperation() => await _mergeRebaseCoordinator.ContinueOperationAsync(async () => await LoadRepositoryData());
    [ReactiveCommand] private async Task Fetch() => await _remoteCoordinator.FetchAsync();
    [ReactiveCommand] private async Task AutoFetch() => await _remoteCoordinator.AutoFetchAsync();
    [ReactiveCommand] private async Task Pull() => await _remoteCoordinator.PullAsync();
    [ReactiveCommand] private async Task Push() => await _remoteCoordinator.PushAsync();
    [ReactiveCommand] private async Task CheckoutBranch(string branchName) => await _branchTagCoordinator.CheckoutBranchAsync(branchName);

    [ReactiveCommand]
    private void ShowCreateBranchModal()
    {
        _branchTagCoordinator.ShowCreateBranchModal();
        GitModals.ShowCreateBranchModal();
    }

    [ReactiveCommand]
    private void ShowCreateRemoteModal()
    {
        _remoteCoordinator.ShowCreateRemoteModal();
        GitModals.ShowCreateRemoteModal();
    }
    [ReactiveCommand] private async Task FetchRemote(string remoteName) => await _remoteCoordinator.FetchRemoteAsync(remoteName);
    [ReactiveCommand] private async Task DeleteRemote(string remoteName) => await _remoteCoordinator.DeleteRemoteAsync(remoteName);
    [ReactiveCommand]
    private void ShowStashModal()
    {
        _historyActionCoordinator.ShowStashModal();
        GitModals.ShowCreateStashModal();
    }

    [ReactiveCommand] private async Task PopStash(int stashIndex) => await _historyActionCoordinator.PopStashAsync(stashIndex);
    [ReactiveCommand] private async Task DropStash(int stashIndex) => await _historyActionCoordinator.DropStashAsync(stashIndex);
    [ReactiveCommand] private async Task CheckoutCommit(CommitSummary? commit) => await _branchTagCoordinator.CheckoutCommitAsync(commit);
    [ReactiveCommand] private async Task CherryPickCommit(CommitSummary? commit) => await _historyActionCoordinator.CherryPickCommitAsync(commit);
    [ReactiveCommand] private async Task RevertCommit(CommitSummary? commit) => await _historyActionCoordinator.RevertCommitAsync(commit);

    [ReactiveCommand]
    private void CreateBranchFromCommit(CommitSummary? commit)
    {
        _branchTagCoordinator.CreateBranchFromCommit(commit);
        GitModals.ShowCreateBranchModal();
    }

    [ReactiveCommand]
    private void CreateTagFromCommit(CommitSummary? commit)
    {
        _branchTagCoordinator.CreateTagFromCommit(commit);
        GitModals.ShowCreateTagModal();
    }

    [ReactiveCommand]
    private async Task RequestResetBranch(GitResetType resetType)
    {
        await _historyActionCoordinator.RequestResetBranchAsync(resetType);
        if (State.PendingResetCommit != null)
        {
            GitModals.ShowResetConfirmModal(resetType, State.PendingResetCommit);
        }
    }

    [ReactiveCommand] private void OpenFileManager() => _historyActionCoordinator.OpenFileManager();
    [ReactiveCommand] private void OpenTerminal() => _historyActionCoordinator.OpenTerminal();
    [ReactiveCommand] private void OpenEditor() => _historyActionCoordinator.OpenEditor();
    [ReactiveCommand] private async Task SelectProfile(GitProfile? profile) => await _historyActionCoordinator.SelectProfileAsync(profile);
    [ReactiveCommand] private void NavigateToProfiles() => _historyActionCoordinator.NavigateToProfileManagement();
    [ReactiveCommand(CanExecute = nameof(CanCommit))] private async Task ExecuteCommit() => await _workingTreeCoordinator.ExecuteCommitAsync();
    [ReactiveCommand] private async Task StageFile(string filePath) => await _workingTreeCoordinator.StageFileAsync(filePath);
    [ReactiveCommand] private async Task UnstageFile(string filePath) => await _workingTreeCoordinator.UnstageFileAsync(filePath);
    [ReactiveCommand] private async Task DiscardFile(string filePath) => await _workingTreeCoordinator.DiscardFileAsync(filePath);
    [ReactiveCommand] private async Task StageAll() => await _workingTreeCoordinator.StageAllAsync();
    [ReactiveCommand] private async Task UnstageAll() => await _workingTreeCoordinator.UnstageAllAsync();
    [ReactiveCommand] private async Task MarkAllResolved() => await _workingTreeCoordinator.MarkAllResolvedAsync();
    [ReactiveCommand] private async Task ExecuteGenerateAiCommitMessage() => await _workingTreeCoordinator.ExecuteGenerateAiCommitMessageAsync();
    [ReactiveCommand] private void DismissAiError() => _workingTreeCoordinator.DismissAiError();
    [ReactiveCommand] private async Task DeleteBranch(string branchName) => await _branchTagCoordinator.DeleteBranchAsync(branchName);
    [ReactiveCommand]
    private async Task ExecuteCreateBranch()
    {
        await _branchTagCoordinator.ExecuteCreateBranchAsync();
        GitModals.HideCreateBranchModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void HideCreateBranchModal()
    {
        _branchTagCoordinator.HideCreateBranchModal();
        GitModals.HideCreateBranchModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void ShowCreateTagModal()
    {
        _branchTagCoordinator.ShowCreateTagModal();
        GitModals.ShowCreateTagModal();
    }

    [ReactiveCommand]
    private void HideCreateTagModal()
    {
        _branchTagCoordinator.HideCreateTagModal();
        GitModals.HideCreateTagModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private async Task ExecuteCreateTag()
    {
        await _branchTagCoordinator.ExecuteCreateTagAsync();
        GitModals.HideCreateTagModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand] private async Task DeleteTag(string tagName) => await _branchTagCoordinator.DeleteTagAsync(tagName);

    [ReactiveCommand]
    private void HideCreateRemoteModal()
    {
        _remoteCoordinator.HideCreateRemoteModal();
        GitModals.HideCreateRemoteModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private async Task ExecuteCreateRemote()
    {
        await _remoteCoordinator.ExecuteCreateRemoteAsync();
        GitModals.HideCreateRemoteModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void CancelCredentials()
    {
        _remoteCoordinator.CancelCredentials();
        GitModals.HideCredentialsModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand] private async Task SubmitCredentials() => await _remoteCoordinator.SubmitCredentialsAsync();

    [ReactiveCommand]
    private void HideStashModal()
    {
        _historyActionCoordinator.HideStashModal();
        GitModals.HideStashModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private async Task ExecuteCreateStash()
    {
        await _historyActionCoordinator.ExecuteCreateStashAsync();
        GitModals.HideStashModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand] private async Task ApplyStash(int stashIndex) => await _historyActionCoordinator.ApplyStashAsync(stashIndex);

    [ReactiveCommand]
    private async Task ExecuteResetBranch()
    {
        await _historyActionCoordinator.ExecuteResetBranchAsync();
        GitModals.HideResetConfirmModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void CancelResetBranch()
    {
        _historyActionCoordinator.CancelResetBranch();
        GitModals.HideResetConfirmModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void HideMergeModal()
    {
        _mergeRebaseCoordinator.HideMergeModal();
        GitModals.HideMergeModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private async Task ExecuteMerge()
    {
        await _mergeRebaseCoordinator.ExecuteMergeAsync(async () => await LoadRepositoryData());
        GitModals.HideMergeModalCommand.Execute().Subscribe();
    }

    [ReactiveCommand]
    private void ShowInteractiveRebaseModal()
    {
        _mergeRebaseCoordinator.ShowInteractiveRebaseModal();
        var s = State;
        InteractiveRebaseModal.ShowModal(s.RepositoryPath, s.RebaseUpstreamSha ?? string.Empty, s.RebaseUpstreamShortSha ?? string.Empty, s.RebaseSteps);
    }

    [ReactiveCommand]
    private void HideInteractiveRebaseModal()
    {
        _mergeRebaseCoordinator.HideInteractiveRebaseModal();
        InteractiveRebaseModal.HideInteractiveRebaseModalCommand.Execute().Subscribe();
    }
    [ReactiveCommand]
    private async Task OpenFavoritesDock()
    {
        if (!FavoritesDock.State.IsFavoritesDockOpen)
        {
            await FavoritesDock.ToggleFavoritesDockCommand.Execute().FirstAsync();
        }
    }

    [ReactiveCommand] private void CloseFavoritesDock() => FavoritesDock.CloseFavoritesDockCommand.Execute().Subscribe();
    [ReactiveCommand] private void ShowAddFavoriteModal() => FavoritesDock.ShowAddFavoriteModalCommand.Execute().Subscribe();
    [ReactiveCommand] private void ShowEditFavoriteModal(string path) => FavoritesDock.ShowEditFavoriteModalCommand.Execute(path).Subscribe();
    [ReactiveCommand] private void HideAddFavoriteModal() => FavoritesDock.HideAddFavoriteModalCommand.Execute().Subscribe();
    [ReactiveCommand]
    private async Task ConfirmAddFavorite()
    {
        await FavoritesDock.ConfirmAddFavoriteCommand.Execute().FirstAsync();
        await _favoritesCoordinator.InitializeFavoriteAndRecentStateAsync();
    }
    [ReactiveCommand]
    private async Task RemoveFromFavorites(string path)
    {
        await FavoritesDock.RemoveFromFavoritesCommand.Execute(path).FirstAsync();
        await _favoritesCoordinator.RemoveFromFavoritesAsync(path);
    }
    [ReactiveCommand] private void ToggleFavoriteGroup(string group) => FavoritesDock.ToggleFavoriteGroupCommand.Execute(group).Subscribe();
    [ReactiveCommand]
    private void HideSettingsModal()
    {
        _settingsCoordinator.HideSettingsModal();
        SettingsModal.HideSettingsModalCommand.Execute().Subscribe();
    }
    [ReactiveCommand] private async Task SaveSettings() => await _settingsCoordinator.SaveSettingsAsync(async () => await LoadRepositoryData());
    [ReactiveCommand] private void MoveRebaseStepUp(RebaseStep? step) => _mergeRebaseCoordinator.MoveRebaseStepUp(step);
    [ReactiveCommand] private void MoveRebaseStepDown(RebaseStep? step) => _mergeRebaseCoordinator.MoveRebaseStepDown(step);
    [ReactiveCommand] private async Task ExecuteInteractiveRebase() => await _mergeRebaseCoordinator.ExecuteInteractiveRebaseAsync(async () => await LoadRepositoryData());
    [ReactiveCommand] private async Task ExecuteSaveSettings() => await _settingsCoordinator.SaveSettingsAsync(async () => await LoadRepositoryData());
    [ReactiveCommand] private void OpenRepositoryFromDock(string repositoryPath) => OpenRepository(repositoryPath);
}
