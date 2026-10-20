namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.Resources;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Coordinator managing Git remote interactions, network fetch/pull/push, and authentication credentials for CommitLog.
/// Usages: CommitLogViewModel remote operations and credentials modal.
/// </summary>
public sealed class CommitLogRemoteCoordinator
{
    private readonly IGitService _gitService;
    private readonly IAppStatusService? _appStatusService;
    private readonly Func<CommitLogState> _getState;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<Task> _loadRepositoryData;

    public CommitLogRemoteCoordinator(
        IGitService gitService,
        IAppStatusService? appStatusService,
        Func<CommitLogState> getState,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<Task> loadRepositoryData)
    {
        _gitService = gitService;
        _appStatusService = appStatusService;
        _getState = getState;
        _updateState = updateState;
        _loadRepositoryData = loadRepositoryData;
    }

    /// <summary>
    /// Updates username input for credentials modal.
    /// Usages: CommitLogViewModel and GitModals credentials modal.
    /// </summary>
    /// <param name="username">Entered username.</param>
    public void UpdateCredentialsUsernameInput(string username)
    {
        _updateState(s => s with { CredentialsUsernameInput = username ?? string.Empty });
    }

    /// <summary>
    /// Updates password or SSH passphrase input for credentials modal.
    /// Usages: CommitLogViewModel and GitModals credentials modal.
    /// </summary>
    /// <param name="password">Entered password or passphrase.</param>
    public void UpdateCredentialsPasswordInput(string password)
    {
        _updateState(s => s with { CredentialsPasswordInput = password ?? string.Empty });
    }

    /// <summary>
    /// Sets authentication credential type for credentials modal.
    /// Usages: CommitLogViewModel and GitModals credentials modal.
    /// </summary>
    /// <param name="authType">Selected authentication type.</param>
    public void SetCredentialsAuthType(GitCredentialType authType)
    {
        _updateState(s => s with { CredentialsAuthType = authType });
    }

    /// <summary>
    /// Updates name input for adding a new remote.
    /// Usages: CommitLogViewModel and GitModals create remote modal.
    /// </summary>
    /// <param name="text">Remote name.</param>
    public void UpdateNewRemoteNameInput(string text)
    {
        _updateState(s => s with { NewRemoteNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates URL input for adding a new remote.
    /// Usages: CommitLogViewModel and GitModals create remote modal.
    /// </summary>
    /// <param name="text">Remote URL.</param>
    public void UpdateNewRemoteUrlInput(string text)
    {
        _updateState(s => s with { NewRemoteUrlInput = text ?? string.Empty });
    }

    /// <summary>
    /// Selects an active remote in the remotes list.
    /// Usages: CommitLogViewModel and CommitLogView.
    /// </summary>
    /// <param name="remote">Selected remote info or null.</param>
    public void SelectRemote(RemoteInfo? remote)
    {
        _updateState(s => s with { SelectedRemote = remote });
    }

    /// <summary>
    /// Fetches all remotes for the repository.
    /// Usages: CommitLogViewModel and toolbar fetch button.
    /// </summary>
    public async Task FetchAsync()
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.FetchingRemotes);
        try
        {
            await _gitService.FetchAsync(state.RepositoryPath);
            await _loadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.FetchSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Fetch);
            _appStatusService?.SetError($"{CommitLogStrings.FetchFailed}: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs silent background fetch if no modals are open and remotes exist.
    /// Usages: CommitLogViewModel auto-fetch timer.
    /// </summary>
    public async Task AutoFetchAsync()
    {
        var state = _getState();
        if (state.IsLoading || state.Remotes.Count == 0 || IsAnyModalOpen())
        {
            return;
        }

        try
        {
            GitCredentials? credentials = null;
            if (state.ActiveProfile?.Credentials != null)
            {
                credentials = new GitCredentials
                {
                    Type = GitCredentialType.Https,
                    Username = state.ActiveProfile.Credentials.Username,
                    PasswordOrToken = state.ActiveProfile.Credentials.AccessToken
                };
            }

            await _gitService.FetchAsync(state.RepositoryPath, credentials: credentials);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Pulls changes from current upstream branch into local branch.
    /// Usages: CommitLogViewModel and toolbar pull button.
    /// </summary>
    public async Task PullAsync()
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.PullingChanges);
        try
        {
            await _gitService.PullAsync(state.RepositoryPath);
            await _loadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.PullSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Pull);
            _appStatusService?.SetError($"{CommitLogStrings.PullFailed}: {ex.Message}");
        }
    }

    /// <summary>
    /// Pushes local commits to upstream remote branch.
    /// Usages: CommitLogViewModel and toolbar push button.
    /// </summary>
    public async Task PushAsync()
    {
        var state = _getState();
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.PushingChanges);
        try
        {
            GitCredentials? credentials = null;
            if (state.ActiveProfile?.Credentials != null)
            {
                credentials = new GitCredentials
                {
                    Type = GitCredentialType.Https,
                    Username = state.ActiveProfile.Credentials.Username,
                    PasswordOrToken = state.ActiveProfile.Credentials.AccessToken
                };
            }

            await _gitService.PushAsync(state.RepositoryPath, credentials: credentials);
            await _loadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.PushSucceeded);
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Push);
            _appStatusService?.SetError($"{CommitLogStrings.PushFailed}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels pending credentials modal prompt.
    /// Usages: CommitLogViewModel and GitModals credentials modal.
    /// </summary>
    public void CancelCredentials()
    {
        _updateState(s => s with
        {
            IsCredentialsModalVisible = false,
            PendingRemoteOperation = RemoteOperationPending.None,
            CredentialsUsernameInput = string.Empty,
            CredentialsPasswordInput = string.Empty,
            CredentialsErrorMessage = null
        });
        _appStatusService?.SetReady();
    }

    /// <summary>
    /// Submits credentials for a pending remote operation.
    /// Usages: CommitLogViewModel and GitModals credentials modal.
    /// </summary>
    public async Task SubmitCredentialsAsync()
    {
        var state = _getState();
        var pending = state.PendingRemoteOperation;
        if (pending == RemoteOperationPending.None)
        {
            _updateState(s => s with { IsCredentialsModalVisible = false });
            return;
        }

        GitCredentials credentials = BuildCredentialsFromState();
        _updateState(s => s with { IsLoading = true, CredentialsErrorMessage = null });
        SetRemoteOperationInProgress(pending);

        try
        {
            await ExecutePendingRemoteOperationAsync(pending, credentials);
            _updateState(s => s with
            {
                IsCredentialsModalVisible = false,
                PendingRemoteOperation = RemoteOperationPending.None,
                CredentialsUsernameInput = string.Empty,
                CredentialsPasswordInput = string.Empty
            });
            await _loadRepositoryData();
            SetRemoteOperationSuccess(pending);
        }
        catch (Exception ex)
        {
            _updateState(s => s with
            {
                IsLoading = false,
                CredentialsErrorMessage = ex.Message
            });
            SetRemoteOperationFailed(pending, ex.Message);
        }
    }

    /// <summary>
    /// Displays create remote modal dialog.
    /// Usages: CommitLogViewModel and remotes list.
    /// </summary>
    public void ShowCreateRemoteModal()
    {
        _updateState(s => s with
        {
            IsCreateRemoteModalVisible = true,
            NewRemoteNameInput = string.Empty,
            NewRemoteUrlInput = string.Empty
        });
    }

    /// <summary>
    /// Hides create remote modal dialog.
    /// Usages: CommitLogViewModel and GitModals create remote modal.
    /// </summary>
    public void HideCreateRemoteModal()
    {
        _updateState(s => s with { IsCreateRemoteModalVisible = false });
    }

    /// <summary>
    /// Adds a new Git remote to the repository.
    /// Usages: CommitLogViewModel and GitModals create remote modal.
    /// </summary>
    public async Task ExecuteCreateRemoteAsync()
    {
        var state = _getState();
        if (string.IsNullOrWhiteSpace(state.NewRemoteNameInput) || string.IsNullOrWhiteSpace(state.NewRemoteUrlInput))
        {
            return;
        }

        _updateState(s => s with { IsLoading = true, IsCreateRemoteModalVisible = false });
        try
        {
            await _gitService.AddRemoteAsync(
                state.RepositoryPath,
                state.NewRemoteNameInput.Trim(),
                state.NewRemoteUrlInput.Trim());
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Removes a named remote from the repository.
    /// Usages: CommitLogViewModel and remotes list context menu.
    /// </summary>
    /// <param name="remoteName">Name of the remote to remove.</param>
    public async Task DeleteRemoteAsync(string remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return;
        }

        var state = _getState();
        _updateState(s => s with { IsLoading = true });
        try
        {
            await _gitService.RemoveRemoteAsync(state.RepositoryPath, remoteName);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Fetches from a specific named remote.
    /// Usages: CommitLogViewModel and remotes list context menu.
    /// </summary>
    /// <param name="remoteName">Name of remote to fetch.</param>
    public async Task FetchRemoteAsync(string remoteName)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return;
        }

        var state = _getState();
        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await _gitService.FetchAsync(state.RepositoryPath, remoteName);
            await _loadRepositoryData();
        }
        catch (Exception ex)
        {
            HandleRemoteOperationException(ex, RemoteOperationPending.Fetch);
        }
    }

    private bool IsAnyModalOpen()
    {
        var state = _getState();
        return state.IsCreateBranchModalVisible ||
               state.IsMergeModalVisible ||
               state.IsInteractiveRebaseModalVisible ||
               state.IsCreateRemoteModalVisible ||
               state.IsCreateTagModalVisible ||
               state.IsStashModalVisible ||
               state.IsSettingsModalVisible ||
               state.IsResetConfirmModalVisible ||
               state.IsCredentialsModalVisible;
    }

    private void HandleRemoteOperationException(Exception ex, RemoteOperationPending pendingOperation)
    {
        if (IsAuthenticationException(ex))
        {
            PromptForCredentials(pendingOperation);
            return;
        }

        _updateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
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
        _updateState(s => s with
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
        var state = _getState();
        var remote = state.Remotes.FirstOrDefault();
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
        var state = _getState();
        if (state.CredentialsAuthType == GitCredentialType.Https)
        {
            return new GitCredentials
            {
                Type = GitCredentialType.Https,
                Username = state.CredentialsUsernameInput.Trim(),
                PasswordOrToken = state.CredentialsPasswordInput
            };
        }

        return new GitCredentials
        {
            Type = GitCredentialType.SshKeyPassphrase,
            Passphrase = state.CredentialsPasswordInput
        };
    }

    private async Task ExecutePendingRemoteOperationAsync(RemoteOperationPending operation, GitCredentials? credentials)
    {
        var state = _getState();
        switch (operation)
        {
            case RemoteOperationPending.Fetch:
                await _gitService.FetchAsync(state.RepositoryPath, credentials: credentials);
                break;
            case RemoteOperationPending.Pull:
                await _gitService.PullAsync(state.RepositoryPath, credentials: credentials);
                break;
            case RemoteOperationPending.Push:
                await _gitService.PushAsync(state.RepositoryPath, credentials: credentials);
                break;
            default:
                break;
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
}
