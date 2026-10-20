namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using ReactiveUI;

/// <summary>
/// Coordinator managing Git commit actions (cherry-pick, revert, reset), stashes, system launcher actions, and profile switching for CommitLog.
/// Usages: CommitLogViewModel commit actions, stash modal, system integration, and profiles.
/// </summary>
public sealed class CommitLogHistoryActionCoordinator
{
    private readonly IGitService _gitService;
    private readonly ISystemLauncherService _systemLauncherService;
    private readonly IProfileRepository? _profileRepository;
    private readonly IScreen _hostScreen;
    private readonly Func<CommitLogState> _getState;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<Task> _loadRepositoryData;

    public CommitLogHistoryActionCoordinator(
        IGitService gitService,
        ISystemLauncherService systemLauncherService,
        IProfileRepository? profileRepository,
        IScreen hostScreen,
        Func<CommitLogState> getState,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<Task> loadRepositoryData)
    {
        _gitService = gitService;
        _systemLauncherService = systemLauncherService;
        _profileRepository = profileRepository;
        _hostScreen = hostScreen;
        _getState = getState;
        _updateState = updateState;
        _loadRepositoryData = loadRepositoryData;
    }

    public async Task SelectCommitAsync(CommitSummary commit)
    {
        if (commit == null)
        {
            return;
        }

        _updateState(s => s with
        {
            SelectedCommit = commit,
            SelectedCommitDetail = null,
            SelectedFileDiff = null,
            DiffLines = ImmutableList<DiffLine>.Empty
        });

        var state = _getState();
        if (commit.IsWip)
        {
            var wipDetail = CommitLogHistoryHelper.BuildWipCommitDetail(state.WorkingTreeChanges, commit.ParentShas);
            _updateState(s => s with { SelectedCommitDetail = wipDetail, ActiveSidebarTab = CommitLogSidebarTab.WorkingTree });
            if (wipDetail.ChangedFiles.Count > 0)
            {
                await SelectFileDiffAsync(wipDetail.ChangedFiles[0]);
            }
            return;
        }

        CommitDetail? detail = await _gitService.GetCommitDetailAsync(state.RepositoryPath, commit.Sha);
        _updateState(s => s with { SelectedCommitDetail = detail });

        if (detail != null && detail.ChangedFiles.Count > 0)
        {
            await SelectFileDiffAsync(detail.ChangedFiles[0]);
        }
    }

    public async Task SelectFileDiffAsync(FileDiffSummary file)
    {
        var state = _getState();
        if (file == null || state.SelectedCommit == null)
        {
            return;
        }

        _updateState(s => s with { SelectedFileDiff = file });
        IReadOnlyList<DiffLine> lines = await _gitService.GetFileDiffLinesAsync(
            state.RepositoryPath,
            state.SelectedCommit.Sha,
            file.Path);

        _updateState(s => s with { DiffLines = lines.ToImmutableList() });
    }

    /// <summary>
    /// Selects an active stash entry from the stashes list.
    /// Usages: CommitLogViewModel and CommitLogView.
    /// </summary>
    /// <param name="stash">Stash info or null.</param>
    public void SelectStash(StashInfo? stash)
    {
        _updateState(s => s with { SelectedStash = stash });
    }

    /// <summary>
    /// Updates input text for a new stash entry.
    /// Usages: CommitLogViewModel and GitModals create stash modal.
    /// </summary>
    /// <param name="text">New stash message.</param>
    public void UpdateNewStashMessageInput(string text)
    {
        _updateState(s => s with { NewStashMessageInput = text ?? string.Empty });
    }

    /// <summary>
    /// Opens the current repository directory in the OS file manager.
    /// Usages: CommitLogViewModel toolbar action.
    /// </summary>
    public void OpenFileManager()
    {
        var state = _getState();
        try
        {
            _systemLauncherService.OpenInFileManager(state.RepositoryPath);
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Opens a terminal window inside the current repository directory.
    /// Usages: CommitLogViewModel toolbar action.
    /// </summary>
    public void OpenTerminal()
    {
        var state = _getState();
        try
        {
            _systemLauncherService.OpenInTerminal(state.RepositoryPath);
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Opens the current repository folder in the configured code editor.
    /// Usages: CommitLogViewModel toolbar action.
    /// </summary>
    public void OpenEditor()
    {
        var state = _getState();
        try
        {
            _systemLauncherService.OpenInEditor(state.RepositoryPath);
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Shows the create stash modal dialog.
    /// Usages: CommitLogViewModel toolbar action.
    /// </summary>
    public void ShowStashModal()
    {
        _updateState(s => s with { IsStashModalVisible = true, NewStashMessageInput = string.Empty });
    }

    /// <summary>
    /// Hides the create stash modal dialog.
    /// Usages: CommitLogViewModel and GitModals create stash modal.
    /// </summary>
    public void HideStashModal()
    {
        _updateState(s => s with { IsStashModalVisible = false });
    }

    /// <summary>
    /// Creates a new stash entry with uncommitted changes.
    /// Usages: CommitLogViewModel and GitModals create stash modal.
    /// </summary>
    public async Task ExecuteCreateStashAsync()
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true, IsStashModalVisible = false });
        try
        {
            string message = string.IsNullOrWhiteSpace(state.NewStashMessageInput)
                ? $"Stash at {DateTime.Now:yyyy-MM-dd HH:mm}"
                : state.NewStashMessageInput.Trim();
            await _gitService.CreateStashAsync(state.RepositoryPath, message);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Applies changes from a stash index without dropping it.
    /// Usages: CommitLogViewModel and stashes context menu.
    /// </summary>
    /// <param name="stashIndex">0-based stash index.</param>
    public async Task ApplyStashAsync(int stashIndex)
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.ApplyStashAsync(state.RepositoryPath, stashIndex);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Pops a stash entry by applying its changes and removing it.
    /// Usages: CommitLogViewModel and stashes context menu.
    /// </summary>
    /// <param name="stashIndex">0-based stash index.</param>
    public async Task PopStashAsync(int stashIndex)
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.PopStashAsync(state.RepositoryPath, stashIndex);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Drops a stash entry permanently.
    /// Usages: CommitLogViewModel and stashes context menu.
    /// </summary>
    /// <param name="stashIndex">0-based stash index.</param>
    public async Task DropStashAsync(int stashIndex)
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.DropStashAsync(state.RepositoryPath, stashIndex);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Cherry-picks a commit into the current branch.
    /// Usages: CommitLogViewModel and commit context menu.
    /// </summary>
    /// <param name="commit">Target commit to cherry-pick.</param>
    public async Task CherryPickCommitAsync(CommitSummary? commit)
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
            await _gitService.CherryPickCommitAsync(state.RepositoryPath, target.Sha);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Creates a revert commit for the specified commit.
    /// Usages: CommitLogViewModel and commit context menu.
    /// </summary>
    /// <param name="commit">Target commit to revert.</param>
    public async Task RevertCommitAsync(CommitSummary? commit)
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
            await _gitService.RevertCommitAsync(state.RepositoryPath, target.Sha);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Initiates a branch reset to the selected commit, prompting confirmation for hard reset.
    /// Usages: CommitLogViewModel and commit context menu.
    /// </summary>
    /// <param name="resetType">Soft, Mixed, or Hard reset type.</param>
    public async Task RequestResetBranchAsync(GitResetType resetType)
    {
        var state = _getState();
        if (state.SelectedCommit == null)
        {
            return;
        }

        if (resetType == GitResetType.Hard)
        {
            _updateState(s => s with
            {
                IsResetConfirmModalVisible = true,
                PendingResetType = resetType,
                PendingResetCommit = state.SelectedCommit
            });
            return;
        }

        await ExecuteDirectResetAsync(resetType, state.SelectedCommit);
    }

    /// <summary>
    /// Confirms and executes a pending branch reset from the confirmation modal.
    /// Usages: CommitLogViewModel and GitModals reset confirmation modal.
    /// </summary>
    public async Task ExecuteResetBranchAsync()
    {
        var state = _getState();
        if (state.PendingResetCommit == null)
        {
            return;
        }

        _updateState(s => s with { IsResetConfirmModalVisible = false });
        await ExecuteDirectResetAsync(state.PendingResetType, state.PendingResetCommit);
    }

    /// <summary>
    /// Cancels a pending reset confirmation.
    /// Usages: CommitLogViewModel and GitModals reset confirmation modal.
    /// </summary>
    public void CancelResetBranch()
    {
        _updateState(s => s with { IsResetConfirmModalVisible = false, PendingResetCommit = null });
    }

    /// <summary>
    /// Selects and activates a Git profile for the current repository.
    /// Usages: CommitLogViewModel and profile selection combobox.
    /// </summary>
    /// <param name="profile">The profile to activate or null to clear.</param>
    public async Task SelectProfileAsync(GitProfile? profile)
    {
        var state = _getState();
        _updateState(s => s with { ActiveProfile = profile });

        if (_profileRepository == null)
        {
            return;
        }

        if (profile != null)
        {
            await _profileRepository.SetActiveProfileForRepositoryAsync(state.RepositoryPath, profile.Id);
            await ConfigureGitAuthorIfAvailableAsync(profile);
            return;
        }

        await _profileRepository.ClearActiveProfileForRepositoryAsync(state.RepositoryPath);
    }

    /// <summary>
    /// Navigates to the ProfileManagement screen.
    /// Usages: CommitLogViewModel and profile menu action.
    /// </summary>
    public void NavigateToProfileManagement()
    {
        if (_profileRepository == null)
        {
            return;
        }

        var state = _getState();
        _hostScreen.Router.Navigate.Execute(
            new ProfileManagementViewModel(_hostScreen, _profileRepository, state.RepositoryPath)
        ).Subscribe();
    }

    /// <summary>
    /// Reloads all profiles and the active profile from the repository.
    /// Usages: CommitLogViewModel profiles synchronization.
    /// </summary>
    public async Task ReloadProfilesAsync()
    {
        await LoadProfilesDataAsync();
        var state = _getState();
        if (state.ActiveProfile != null)
        {
            await ConfigureGitAuthorIfAvailableAsync(state.ActiveProfile);
        }
    }

    /// <summary>
    /// Loads profiles data from the profiles repository.
    /// Usages: CommitLogViewModel repository data loading.
    /// </summary>
    public async Task LoadProfilesDataAsync()
    {
        if (_profileRepository == null)
        {
            return;
        }

        var state = _getState();
        try
        {
            IReadOnlyList<GitProfile> profiles = await _profileRepository.GetAllProfilesAsync();
            GitProfile? activeProfile = await _profileRepository.GetActiveProfileForRepositoryAsync(state.RepositoryPath);

            _updateState(s => s with
            {
                AvailableProfiles = profiles.ToImmutableList(),
                ActiveProfile = activeProfile
            });
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private async Task ConfigureGitAuthorIfAvailableAsync(GitProfile profile)
    {
        if (profile.Author == null)
        {
            return;
        }

        var state = _getState();
        try
        {
            await _gitService.SetGitConfigAsync(state.RepositoryPath, profile.Author.Name, profile.Author.Email);
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private async Task ExecuteDirectResetAsync(GitResetType resetType, CommitSummary commit)
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true, PendingResetCommit = null });
        try
        {
            await _gitService.ResetBranchToCommitAsync(state.RepositoryPath, commit.Sha, resetType);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }
}
