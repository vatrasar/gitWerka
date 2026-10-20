namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Resources;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;
using ReactiveUI;

/// <summary>
/// Coordinator managing working tree modifications, staging, AI commit message generation, and commit creation for CommitLog.
/// Usages: CommitLogViewModel working tree operations and commit composer.
/// </summary>
public sealed class CommitLogWorkingTreeCoordinator
{
    private readonly IGitService _gitService;
    private readonly IScreen _hostScreen;
    private readonly IAiCommitMessageService? _aiCommitMessageService;
    private readonly IAppStatusService? _appStatusService;
    private readonly Func<CommitLogState> _getState;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<Task> _loadRepositoryData;
    private CancellationTokenSource? _aiCancellationTokenSource;

    public CommitLogWorkingTreeCoordinator(
        IGitService gitService,
        IScreen hostScreen,
        IAiCommitMessageService? aiCommitMessageService,
        IAppStatusService? appStatusService,
        Func<CommitLogState> getState,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<Task> loadRepositoryData)
    {
        _gitService = gitService;
        _hostScreen = hostScreen;
        _aiCommitMessageService = aiCommitMessageService;
        _appStatusService = appStatusService;
        _getState = getState;
        _updateState = updateState;
        _loadRepositoryData = loadRepositoryData;
    }

    /// <summary>
    /// Opens the 3-panel conflict resolution screen for the selected conflicted or resolved file.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="file">The working tree file change.</param>
    public void OpenConflictResolver(WorkingTreeChange? file)
    {
        if (file == null)
        {
            return;
        }

        var state = _getState();
        _hostScreen.Router.Navigate.Execute(new ConflictResolverViewModel(_hostScreen, _gitService, state.RepositoryPath, file.Path)).Subscribe();
    }

    /// <summary>
    /// Marks all detected conflict files as resolved.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public async Task MarkAllResolvedAsync()
    {
        var state = _getState();
        try
        {
            await _gitService.MarkAllConflictsResolvedAsync(state.RepositoryPath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            RepositoryOperationStatus opStatus = await _gitService.GetRepositoryOperationStatusAsync(state.RepositoryPath);

            _updateState(s => s with
            {
                WorkingTreeChanges = changes.ToImmutableList(),
                OperationStatus = opStatus
            });
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Stages an individual file in the repository.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="filePath">Relative file path.</param>
    public async Task StageFileAsync(string filePath)
    {
        var state = _getState();
        try
        {
            await _gitService.StageFileAsync(state.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            _updateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
            await RefreshWipCommitIfSelectedAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Unstages an individual file from the index.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="filePath">Relative file path.</param>
    public async Task UnstageFileAsync(string filePath)
    {
        var state = _getState();
        try
        {
            await _gitService.UnstageFileAsync(state.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            _updateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
            await RefreshWipCommitIfSelectedAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Discards working tree changes for a given file.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="filePath">Relative file path.</param>
    public async Task DiscardFileAsync(string filePath)
    {
        var state = _getState();
        try
        {
            await _gitService.DiscardFileChangesAsync(state.RepositoryPath, filePath);
            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            _updateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
            await RefreshWipCommitIfSelectedAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Stages all unstaged changes in the repository.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public async Task StageAllAsync()
    {
        var state = _getState();
        try
        {
            foreach (var change in state.UnstagedChanges)
            {
                await _gitService.StageFileAsync(state.RepositoryPath, change.Path);
            }

            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            _updateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
            await RefreshWipCommitIfSelectedAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Unstages all staged files from the index.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public async Task UnstageAllAsync()
    {
        var state = _getState();
        try
        {
            foreach (var change in state.StagedChanges)
            {
                await _gitService.UnstageFileAsync(state.RepositoryPath, change.Path);
            }

            IReadOnlyList<WorkingTreeChange> changes = await _gitService.GetWorkingTreeChangesAsync(state.RepositoryPath);
            _updateState(s => s with { WorkingTreeChanges = changes.ToImmutableList() });
            await RefreshWipCommitIfSelectedAsync();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    private async Task RefreshWipCommitIfSelectedAsync()
    {
        var state = _getState();
        if (state.SelectedCommit?.IsWip != true)
        {
            return;
        }

        var wipDetail = await _gitService.GetCommitDetailAsync(state.RepositoryPath, "WIP")
            ?? CommitLogHistoryHelper.BuildWipCommitDetail(state.WorkingTreeChanges, state.SelectedCommit.ParentShas);
        _updateState(s => s with { SelectedCommitDetail = wipDetail });

        if (wipDetail.ChangedFiles.Count > 0)
        {
            var fileToSelect = wipDetail.ChangedFiles.FirstOrDefault(f => f.Path == state.SelectedFileDiff?.Path)
                ?? wipDetail.ChangedFiles[0];
            var lines = await _gitService.GetFileDiffLinesAsync(state.RepositoryPath, "WIP", fileToSelect.Path);
            _updateState(s => s with { SelectedFileDiff = fileToSelect, DiffLines = lines.ToImmutableList() });
        }
        else
        {
            _updateState(s => s with { SelectedFileDiff = null, DiffLines = ImmutableList<DiffLine>.Empty });
        }
    }

    /// <summary>
    /// Updates the commit message composer input text.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="message">New commit message input text.</param>
    public void UpdateCommitMessageInput(string message)
    {
        _updateState(s => s with { CommitMessageInput = message ?? string.Empty });
    }

    /// <summary>
    /// Toggles the amend previous commit flag and initializes message if needed.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    /// <param name="isAmend">True to amend, false otherwise.</param>
    public void ToggleAmendCommit(bool isAmend)
    {
        var state = _getState();
        if (isAmend)
        {
            string headMessage = state.AllCommits.FirstOrDefault()?.Message ?? string.Empty;
            string messageToUse = string.IsNullOrWhiteSpace(state.CommitMessageInput) ? headMessage : state.CommitMessageInput;

            _updateState(s => s with
            {
                IsAmendCommit = true,
                CommitMessageInput = messageToUse
            });
            return;
        }

        _updateState(s => s with { IsAmendCommit = false });
    }

    /// <summary>
    /// Requests AI generation of a commit message from staged changes.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public async Task ExecuteGenerateAiCommitMessageAsync()
    {
        var state = _getState();
        if (state.IsGeneratingAiCommit)
        {
            return;
        }

        if (_aiCommitMessageService == null)
        {
            _updateState(s => s with
            {
                AiErrorMessage = CommitLogStrings.AiServiceUnavailableError,
                IsAiErrorSettingsActionVisible = false,
                IsAiRetrying = false
            });
            return;
        }

        _aiCancellationTokenSource?.Cancel();
        _aiCancellationTokenSource?.Dispose();
        _aiCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _aiCancellationTokenSource.Token;

        _updateState(s => s with
        {
            IsGeneratingAiCommit = true,
            AiErrorMessage = null,
            IsAiErrorSettingsActionVisible = false,
            IsAiRetrying = false
        });

        _appStatusService?.SetInProgress(CommitLogStrings.GeneratingAiCommitMessage);
        SynchronousProgress<AiRetryAttempt> progress = new(HandleAiRetryProgress);

        try
        {
            string proposal = await _aiCommitMessageService.GenerateCommitMessageAsync(
                state.RepositoryPath,
                progress,
                cancellationToken);

            string sanitizedProposal = IAiCommitMessageService.TruncateCommitSubject(proposal);

            _updateState(s => s with
            {
                CommitMessageInput = sanitizedProposal,
                IsGeneratingAiCommit = false,
                AiErrorMessage = null,
                IsAiErrorSettingsActionVisible = false,
                IsAiRetrying = false
            });

            _appStatusService?.SetSuccess(CommitLogStrings.AiCommitMessageGeneratedSuccess, TimeSpan.FromSeconds(3));
        }
        catch (OperationCanceledException)
        {
            _updateState(s => s with
            {
                IsGeneratingAiCommit = false,
                AiErrorMessage = null,
                IsAiErrorSettingsActionVisible = false,
                IsAiRetrying = false
            });

            _appStatusService?.SetReady();
        }
        catch (Exception ex)
        {
            bool isMissingApiKey = ex.Message.Contains("API key", StringComparison.OrdinalIgnoreCase);

            _updateState(s => s with
            {
                IsGeneratingAiCommit = false,
                AiErrorMessage = ex.Message,
                IsAiErrorSettingsActionVisible = isMissingApiKey,
                IsAiRetrying = false
            });

            _appStatusService?.SetError(ex.Message, TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Dismisses active AI generation error banner.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public void DismissAiError()
    {
        _aiCancellationTokenSource?.Cancel();

        _updateState(s => s with
        {
            AiErrorMessage = null,
            IsAiErrorSettingsActionVisible = false,
            IsAiRetrying = false
        });

        _appStatusService?.SetReady();
    }

    /// <summary>
    /// Commits staged changes using active Git profile author information.
    /// Usages: CommitLogViewModel and WorkingTreePanel.
    /// </summary>
    public async Task ExecuteCommitAsync()
    {
        var state = _getState();
        if (state.ActiveProfile == null)
        {
            _updateState(s => s with { ErrorMessage = CommitLogStrings.ProfileRequiredToCommit });
            _appStatusService?.SetError(CommitLogStrings.ProfileRequiredToCommit);
            return;
        }

        if (string.IsNullOrWhiteSpace(state.CommitMessageInput))
        {
            _updateState(s => s with { ErrorMessage = "Commit message cannot be empty." });
            return;
        }

        _updateState(s => s with { IsLoading = true, ErrorMessage = null });
        _appStatusService?.SetInProgress(CommitLogStrings.CommittingChanges);

        try
        {
            string authorName = !string.IsNullOrWhiteSpace(state.ActiveProfile.Author?.Name)
                ? state.ActiveProfile.Author.Name
                : state.ActiveProfile.Name;
            string authorEmail = !string.IsNullOrWhiteSpace(state.ActiveProfile.Author?.Email)
                ? state.ActiveProfile.Author.Email
                : $"{state.ActiveProfile.Name.ToLowerInvariant().Replace(' ', '.')}@gitwerka.local";

            await _gitService.CreateCommitAsync(
                state.RepositoryPath,
                state.CommitMessageInput.Trim(),
                authorName,
                authorEmail,
                state.IsAmendCommit);

            _updateState(s => s with
            {
                CommitMessageInput = string.Empty,
                IsAmendCommit = false
            });
            await _loadRepositoryData();
            _appStatusService?.SetSuccess(CommitLogStrings.CommitSucceeded);
        }
        catch (Exception ex)
        {
            _updateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
            _appStatusService?.SetError($"{CommitLogStrings.CommitFailed}: {ex.Message}");
        }
    }

    private void HandleAiRetryProgress(AiRetryAttempt retry)
    {
        var state = _getState();
        if (!state.IsGeneratingAiCommit)
        {
            return;
        }

        string message = FormatRetryProgressMessage(retry);

        _updateState(s => s with
        {
            AiErrorMessage = message,
            IsAiRetrying = true,
            IsAiErrorSettingsActionVisible = false
        });

        _appStatusService?.SetInProgress(message);
    }

    private static string FormatRetryProgressMessage(AiRetryAttempt retry)
    {
        if (retry.IsWaiting)
        {
            int seconds = Math.Max(1, (int)Math.Ceiling(retry.Delay.TotalSeconds));
            return string.Format(
                CommitLogStrings.AiRateLimitRetryingCountdown,
                seconds,
                retry.AttemptNumber,
                retry.MaxRetries);
        }

        return string.Format(
            CommitLogStrings.AiRateLimitRetryingAttempt,
            retry.AttemptNumber,
            retry.MaxRetries);
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value) => _handler(value);
    }
}
