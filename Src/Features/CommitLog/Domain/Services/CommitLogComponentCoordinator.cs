namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.InteractiveRebaseModal;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.SettingsModal;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;
using ReactiveUI;

/// <summary>
/// Coordinator wiring reactive events and state propagation between CommitLog smart child components and feature coordinators.
/// </summary>
public sealed class CommitLogComponentCoordinator
{
    private readonly FavoritesDockViewModel _favoritesDock;
    private readonly SettingsModalViewModel _settingsModal;
    private readonly InteractiveRebaseModalViewModel _interactiveRebaseModal;
    private readonly GitOperationModalsViewModel _gitModals;
    private readonly WorkingTreePanelViewModel _workingTreePanel;
    private readonly CommitLogFavoritesCoordinator _favoritesCoordinator;
    private readonly CommitLogMergeRebaseCoordinator _mergeRebaseCoordinator;
    private readonly CommitLogSettingsCoordinator _settingsCoordinator;
    private readonly CommitLogWorkingTreeCoordinator _workingTreeCoordinator;
    private readonly CommitLogRemoteCoordinator _remoteCoordinator;
    private readonly CommitLogBranchTagCoordinator _branchTagCoordinator;
    private readonly CommitLogHistoryActionCoordinator _historyActionCoordinator;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly IObservable<CommitLogState> _stateObservable;
    private readonly Func<Task> _loadRepositoryData;
    private readonly Action<string> _openRepositoryFromDock;

    public CommitLogComponentCoordinator(
        FavoritesDockViewModel favoritesDock,
        SettingsModalViewModel settingsModal,
        InteractiveRebaseModalViewModel interactiveRebaseModal,
        GitOperationModalsViewModel gitModals,
        WorkingTreePanelViewModel workingTreePanel,
        CommitLogFavoritesCoordinator favoritesCoordinator,
        CommitLogMergeRebaseCoordinator mergeRebaseCoordinator,
        CommitLogSettingsCoordinator settingsCoordinator,
        CommitLogWorkingTreeCoordinator workingTreeCoordinator,
        CommitLogRemoteCoordinator remoteCoordinator,
        CommitLogBranchTagCoordinator branchTagCoordinator,
        CommitLogHistoryActionCoordinator historyActionCoordinator,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        IObservable<CommitLogState> stateObservable,
        Func<Task> loadRepositoryData,
        Action<string> openRepositoryFromDock)
    {
        _favoritesDock = favoritesDock;
        _settingsModal = settingsModal;
        _interactiveRebaseModal = interactiveRebaseModal;
        _gitModals = gitModals;
        _workingTreePanel = workingTreePanel;
        _favoritesCoordinator = favoritesCoordinator;
        _mergeRebaseCoordinator = mergeRebaseCoordinator;
        _settingsCoordinator = settingsCoordinator;
        _workingTreeCoordinator = workingTreeCoordinator;
        _remoteCoordinator = remoteCoordinator;
        _branchTagCoordinator = branchTagCoordinator;
        _historyActionCoordinator = historyActionCoordinator;
        _updateState = updateState;
        _stateObservable = stateObservable;
        _loadRepositoryData = loadRepositoryData;
        _openRepositoryFromDock = openRepositoryFromDock;
    }

    public void Wire()
    {
        WireFavoritesDock();
        WireSettingsModal();
        WireInteractiveRebaseModal();
        WireGitModals();
        WireWorkingTreePanel();
        WireStatePropagation();
    }

    private void WireFavoritesDock()
    {
        _favoritesDock.RepositorySelected.Subscribe(path => _openRepositoryFromDock(path));
        _favoritesDock.WhenAnyValue(x => x.State).Subscribe(s => _updateState(p => p with
        {
            IsFavoritesDockOpen = s.IsFavoritesDockOpen,
            RecentNonFavoriteRepository = s.RecentNonFavoriteRepository,
            FavoriteRepositories = s.FavoriteRepositories,
            IsAddFavoriteModalVisible = s.IsAddFavoriteModalVisible,
            TargetFavoritePath = s.TargetFavoritePath,
            NewFavoriteNameInput = s.NewFavoriteNameInput,
            NewFavoriteGroupInput = s.NewFavoriteGroupInput,
            IsEditingExistingFavorite = s.IsEditingExistingFavorite,
            ExpandedFavoriteGroupNames = s.ExpandedFavoriteGroupNames,
            FavoriteGroups = s.FavoriteGroups,
            UngroupedFavoriteRepositories = s.UngroupedFavoriteRepositories,
            ExistingGroupNames = s.ExistingGroupNames
        }));
    }

    private void WireSettingsModal()
    {
        _settingsModal.SettingsSaved.Subscribe(async _ => await _loadRepositoryData());
        _settingsModal.WhenAnyValue(x => x.State).Subscribe(s => _updateState(p => p with
        {
            IsSettingsModalVisible = s.IsVisible,
            ConfigUserNameInput = s.ConfigUserNameInput,
            ConfigUserEmailInput = s.ConfigUserEmailInput,
            DefaultProjectsDirectory = s.DefaultProjectsDirectory,
            DefaultProjectsDirectoryInput = s.DefaultProjectsDirectoryInput,
            MaxVisibleBranchesInput = s.MaxVisibleBranchesInput,
            AutoFetchIntervalInput = s.AutoFetchIntervalInput,
            AiApiKeyInput = s.AiApiKeyInput,
            AiModelInput = s.AiModelInput,
            AiCommitStyleInput = s.AiCommitStyleInput,
            AiTokenLimitInput = s.AiTokenLimitInput,
            AiProjectDescriptionInput = s.AiProjectDescriptionInput
        }));
    }

    private void WireInteractiveRebaseModal()
    {
        _interactiveRebaseModal.RebaseCompleted.Subscribe(async _ => await _loadRepositoryData());
        _interactiveRebaseModal.WhenAnyValue(x => x.State).Subscribe(s => _updateState(p => p with
        {
            IsInteractiveRebaseModalVisible = s.IsVisible,
            RebaseUpstreamSha = s.RebaseUpstreamSha,
            RebaseUpstreamShortSha = s.RebaseUpstreamShortSha,
            RebaseSteps = s.RebaseSteps,
            SelectedRebaseStep = s.SelectedRebaseStep
        }));
    }

    private void WireGitModals()
    {
        _gitModals.ConfirmCreateBranchRequested.Subscribe(async _ => await _branchTagCoordinator.ExecuteCreateBranchAsync());
        _gitModals.ConfirmCreateRemoteRequested.Subscribe(async _ => await _remoteCoordinator.ExecuteCreateRemoteAsync());
        _gitModals.ConfirmCreateTagRequested.Subscribe(async _ => await _branchTagCoordinator.ExecuteCreateTagAsync());
        _gitModals.ConfirmCreateStashRequested.Subscribe(async _ => await _historyActionCoordinator.ExecuteCreateStashAsync());
        _gitModals.ConfirmResetRequested.Subscribe(async _ => await _historyActionCoordinator.ExecuteResetBranchAsync());
        _gitModals.ConfirmMergeRequested.Subscribe(async _ => await _mergeRebaseCoordinator.ExecuteMergeAsync(async () => await _loadRepositoryData()));
        _gitModals.SubmitCredentialsRequested.Subscribe(async _ => await _remoteCoordinator.SubmitCredentialsAsync());

        _gitModals.WhenAnyValue(x => x.State).Subscribe(s => _updateState(p => p with
        {
            IsCreateBranchModalVisible = s.IsCreateBranchModalVisible,
            NewBranchNameInput = s.NewBranchNameInput,
            IsCreateRemoteModalVisible = s.IsCreateRemoteModalVisible,
            NewRemoteNameInput = s.NewRemoteNameInput,
            NewRemoteUrlInput = s.NewRemoteUrlInput,
            IsCreateTagModalVisible = s.IsCreateTagModalVisible,
            NewTagNameInput = s.NewTagNameInput,
            NewTagMessageInput = s.NewTagMessageInput,
            IsStashModalVisible = s.IsStashModalVisible,
            NewStashMessageInput = s.NewStashMessageInput,
            IsResetConfirmModalVisible = s.IsResetConfirmModalVisible,
            PendingResetType = s.PendingResetType,
            PendingResetCommit = s.PendingResetCommit,
            IsMergeModalVisible = s.IsMergeModalVisible,
            SelectedMergeBranch = s.SelectedMergeBranch,
            MergeFastForwardMode = s.MergeFastForwardMode,
            IsCredentialsModalVisible = s.IsCredentialsModalVisible,
            CredentialsAuthType = s.CredentialsAuthType,
            CredentialsUsernameInput = s.CredentialsUsernameInput,
            CredentialsPasswordInput = s.CredentialsPasswordInput,
            CredentialsErrorMessage = s.CredentialsErrorMessage
        }));
    }

    private void WireWorkingTreePanel()
    {
        _workingTreePanel.StageAllRequested.Subscribe(async _ => await _workingTreeCoordinator.StageAllAsync());
        _workingTreePanel.UnstageAllRequested.Subscribe(async _ => await _workingTreeCoordinator.UnstageAllAsync());
        _workingTreePanel.StageFileRequested.Subscribe(async path => await _workingTreeCoordinator.StageFileAsync(path));
        _workingTreePanel.UnstageFileRequested.Subscribe(async path => await _workingTreeCoordinator.UnstageFileAsync(path));
        _workingTreePanel.MarkAllResolvedRequested.Subscribe(async _ => await _workingTreeCoordinator.MarkAllResolvedAsync());
        _workingTreePanel.OpenConflictResolverRequested.Subscribe(file => _workingTreeCoordinator.OpenConflictResolver(file));
        _workingTreePanel.GenerateAiCommitRequested.Subscribe(async _ => await _workingTreeCoordinator.ExecuteGenerateAiCommitMessageAsync());
        _workingTreePanel.DismissAiErrorRequested.Subscribe(_ => _workingTreeCoordinator.DismissAiError());
        _workingTreePanel.OpenSettingsRequested.Subscribe(async _ => await _settingsModal.ShowSettingsModalCommand.Execute().FirstAsync());
        _workingTreePanel.ConfigureProfileRequested.Subscribe(_ => _historyActionCoordinator.NavigateToProfileManagement());
        _workingTreePanel.CommitRequested.Subscribe(async _ => await _workingTreeCoordinator.ExecuteCommitAsync());

        _workingTreePanel.WhenAnyValue(x => x.State.CommitMessageInput)
            .DistinctUntilChanged()
            .Subscribe(msg => _updateState(p => p with { CommitMessageInput = msg ?? string.Empty }));
        _workingTreePanel.WhenAnyValue(x => x.State.IsAmendCommit)
            .DistinctUntilChanged()
            .Subscribe(isAmend => _workingTreeCoordinator.ToggleAmendCommit(isAmend));
    }

    private void WireStatePropagation()
    {
        _stateObservable.Select(s => (s.WorkingTreeChanges, s.MergeSourceBranch, s.MergeTargetBranch, s.OperationStatus, s.ActiveProfile))
            .DistinctUntilChanged()
            .Subscribe(t => _workingTreePanel.UpdateWorkingTree(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5));

        _stateObservable.Select(s => (s.IsGeneratingAiCommit, s.AiErrorMessage, s.IsAiErrorSettingsActionVisible))
            .DistinctUntilChanged()
            .Subscribe(t => _workingTreePanel.UpdateAiStatus(t.Item1, t.Item2, t.Item3));

        _stateObservable.Select(s => s.CommitMessageInput)
            .DistinctUntilChanged()
            .Subscribe(msg =>
            {
                if (_workingTreePanel.State.CommitMessageInput != msg)
                {
                    _workingTreePanel.UpdateCommitMessageInput(msg);
                }
            });

        _stateObservable.Select(s => s.IsAmendCommit)
            .DistinctUntilChanged()
            .Subscribe(isAmend =>
            {
                if (_workingTreePanel.State.IsAmendCommit != isAmend)
                {
                    _workingTreePanel.SetAmendCommit(isAmend);
                }
            });

        _stateObservable.Select(s => s.Branches)
            .DistinctUntilChanged()
            .Subscribe(branches => _gitModals.SetAvailableBranches(branches));

        _stateObservable.Select(s => (s.PendingRemoteOperation, s.IsCredentialsModalVisible, s.CredentialsAuthType, s.CredentialsErrorMessage))
            .DistinctUntilChanged()
            .Subscribe(t =>
            {
                if (t.IsCredentialsModalVisible && t.PendingRemoteOperation != RemoteOperationPending.None)
                {
                    _gitModals.ShowCredentialsModal(t.PendingRemoteOperation, t.CredentialsAuthType, t.CredentialsErrorMessage);
                }
                else if (!t.IsCredentialsModalVisible && _gitModals.State.IsCredentialsModalVisible)
                {
                    _gitModals.HideCredentialsModalCommand.Execute().Subscribe();
                }
            });

        _stateObservable.Select(s => s.RepositoryPath)
            .DistinctUntilChanged()
            .Subscribe(path =>
            {
                _favoritesDock.SetCurrentRepositoryPath(path);
                _settingsModal.SetRepositoryPath(path);
            });

        _stateObservable.Select(s => s.DefaultProjectsDirectory)
            .DistinctUntilChanged()
            .Subscribe(dir => _favoritesDock.SetDefaultProjectsDirectory(dir));
    }
}
