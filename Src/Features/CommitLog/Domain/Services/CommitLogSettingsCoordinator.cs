namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Service coordinating Git author, project defaults, and AI commit assistance configuration for CommitLog.
/// </summary>
public class CommitLogSettingsCoordinator
{
    private readonly IGitService _gitService;
    private readonly IApplicationSettingsRepository? _appSettingsRepo;
    private readonly IAiConfigurationRepository? _aiConfigRepo;
    private readonly Action<Func<CommitLogState, CommitLogState>> _updateState;
    private readonly Func<CommitLogState> _getState;

    public CommitLogSettingsCoordinator(
        IGitService gitService,
        IApplicationSettingsRepository? appSettingsRepo,
        IAiConfigurationRepository? aiConfigRepo,
        Action<Func<CommitLogState, CommitLogState>> updateState,
        Func<CommitLogState> getState)
    {
        _gitService = gitService;
        _appSettingsRepo = appSettingsRepo;
        _aiConfigRepo = aiConfigRepo;
        _updateState = updateState;
        _getState = getState;
    }

    public void SetActiveSidebarTab(CommitLogSidebarTab tab) =>
        _updateState(s => s with { ActiveSidebarTab = tab });

    public void UpdateConfigUserNameInput(string text) =>
        _updateState(s => s with { ConfigUserNameInput = text ?? string.Empty });

    public void UpdateConfigUserEmailInput(string text) =>
        _updateState(s => s with { ConfigUserEmailInput = text ?? string.Empty });

    public void UpdateDefaultProjectsDirectoryInput(string text) =>
        _updateState(s => s with { DefaultProjectsDirectoryInput = text ?? string.Empty });

    public void UpdateMaxVisibleBranchesInput(string text) =>
        _updateState(s => s with { MaxVisibleBranchesInput = text ?? string.Empty });

    /// <summary>
    /// Updates automatic background fetch interval input in state.
    /// Usages: CommitLogViewModel and SettingsModal.
    /// </summary>
    /// <param name="text">Auto-fetch interval in seconds string.</param>
    public void UpdateAutoFetchIntervalInput(string text) =>
        _updateState(s => s with { AutoFetchIntervalInput = text ?? string.Empty });

    public void UpdateAiApiKeyInput(string text) =>
        _updateState(s => s with { AiApiKeyInput = text ?? string.Empty });

    public void UpdateAiModelInput(string text) =>
        _updateState(s => s with { AiModelInput = text ?? string.Empty });

    public void UpdateAiCommitStyleInput(string text) =>
        _updateState(s => s with { AiCommitStyleInput = text ?? string.Empty });

    public void UpdateAiTokenLimitInput(string text) =>
        _updateState(s => s with { AiTokenLimitInput = text ?? string.Empty });

    public void UpdateAiProjectDescriptionInput(string text) =>
        _updateState(s => s with { AiProjectDescriptionInput = text ?? string.Empty });

    public async Task LoadApplicationSettingsAsync()
    {
        if (_appSettingsRepo == null)
        {
            return;
        }

        ApplicationSettings settings = await _appSettingsRepo.GetSettingsAsync();
        _updateState(s => s with
        {
            DefaultProjectsDirectory = settings.DefaultProjectsDirectory ?? string.Empty,
            DefaultProjectsDirectoryInput = settings.DefaultProjectsDirectory ?? string.Empty,
            AutoFetchIntervalSeconds = settings.AutoFetchIntervalSeconds,
            AutoFetchIntervalInput = settings.AutoFetchIntervalSeconds.ToString()
        });
    }

    public async Task ShowSettingsModalAsync()
    {
        try
        {
            var state = _getState();
            GitConfigInfo config = await _gitService.GetGitConfigAsync(state.RepositoryPath);
            (AiConfiguration aiConfig, string? projectDesc) = await LoadAiSettingsAsync();
            ApplicationSettings appSettings = _appSettingsRepo != null
                ? await _appSettingsRepo.GetSettingsAsync()
                : new ApplicationSettings { DefaultProjectsDirectory = state.DefaultProjectsDirectory };

            _updateState(s => s with
            {
                IsSettingsModalVisible = true,
                ConfigUserNameInput = config.UserName,
                ConfigUserEmailInput = config.UserEmail,
                DefaultProjectsDirectory = appSettings.DefaultProjectsDirectory,
                DefaultProjectsDirectoryInput = appSettings.DefaultProjectsDirectory,
                MaxVisibleBranchesInput = (appSettings.MaxVisibleBranches > 0 ? appSettings.MaxVisibleBranches : 5).ToString(),
                AutoFetchIntervalSeconds = appSettings.AutoFetchIntervalSeconds >= 0 ? appSettings.AutoFetchIntervalSeconds : 60,
                AutoFetchIntervalInput = (appSettings.AutoFetchIntervalSeconds >= 0 ? appSettings.AutoFetchIntervalSeconds : 60).ToString(),
                AiApiKeyInput = aiConfig.ApiKey ?? string.Empty,
                AiModelInput = aiConfig.Model,
                AiCommitStyleInput = aiConfig.CommitStyle,
                AiTokenLimitInput = aiConfig.TokenLimit.ToString(),
                AiProjectDescriptionInput = projectDesc ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    public void HideSettingsModal() =>
        _updateState(s => s with { IsSettingsModalVisible = false });

    public Task SaveSettingsAsync(Func<Task> reloadCallback) => ExecuteSaveSettingsAsync(reloadCallback);

    public async Task ExecuteSaveSettingsAsync(Func<Task> reloadCallback)
    {
        _updateState(s => s with { IsSettingsModalVisible = false });
        try
        {
            var state = _getState();
            await _gitService.SetGitConfigAsync(
                state.RepositoryPath,
                state.ConfigUserNameInput.Trim(),
                state.ConfigUserEmailInput.Trim());

            string trimmedDir = state.DefaultProjectsDirectoryInput?.Trim() ?? string.Empty;
            int fetchInterval = 60;
            if (int.TryParse(state.AutoFetchIntervalInput, out int parsedInterval) && parsedInterval >= 0)
            {
                fetchInterval = parsedInterval;
            }

            if (_appSettingsRepo != null)
            {
                int maxBranches = 5;
                if (int.TryParse(state.MaxVisibleBranchesInput, out int parsedBranches) && parsedBranches > 0)
                {
                    maxBranches = parsedBranches;
                }

                await _appSettingsRepo.SaveSettingsAsync(new ApplicationSettings
                {
                    MaxVisibleBranches = maxBranches,
                    DefaultProjectsDirectory = trimmedDir,
                    AutoFetchIntervalSeconds = fetchInterval
                });
            }

            _updateState(s => s with
            {
                DefaultProjectsDirectory = trimmedDir,
                AutoFetchIntervalSeconds = fetchInterval
            });

            await SaveAiSettingsAsync();
            await reloadCallback();
        }
        catch (Exception ex)
        {
            _updateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    public async Task<int> ResolveMaxVisibleBranchesAsync()
    {
        if (_appSettingsRepo == null)
        {
            return 5;
        }

        ApplicationSettings appSettings = await _appSettingsRepo.GetSettingsAsync();
        return appSettings.MaxVisibleBranches > 0 ? appSettings.MaxVisibleBranches : 5;
    }

    private async Task<(AiConfiguration, string?)> LoadAiSettingsAsync()
    {
        if (_aiConfigRepo == null)
        {
            return (new AiConfiguration(), null);
        }

        var state = _getState();
        AiConfiguration aiConfig = await _aiConfigRepo.GetConfigurationAsync();
        string? projectDesc = await _aiConfigRepo.GetProjectDescriptionAsync(state.RepositoryPath);

        return (aiConfig, projectDesc);
    }

    private async Task SaveAiSettingsAsync()
    {
        if (_aiConfigRepo == null)
        {
            return;
        }

        var state = _getState();
        int.TryParse(state.AiTokenLimitInput, out int tokenLimit);
        if (tokenLimit <= 0)
        {
            tokenLimit = 4000;
        }

        AiConfiguration updatedAiConfig = new()
        {
            ApiKey = string.IsNullOrWhiteSpace(state.AiApiKeyInput) ? null : state.AiApiKeyInput.Trim(),
            Model = string.IsNullOrWhiteSpace(state.AiModelInput) ? "google/gemini-2.5-flash" : state.AiModelInput.Trim(),
            CommitStyle = string.IsNullOrWhiteSpace(state.AiCommitStyleInput) ? "Conventional Commits: <type>(<scope>): <subject>" : state.AiCommitStyleInput.Trim(),
            TokenLimit = tokenLimit
        };

        await _aiConfigRepo.SaveConfigurationAsync(updatedAiConfig);
        await _aiConfigRepo.SaveProjectDescriptionAsync(
            state.RepositoryPath,
            string.IsNullOrWhiteSpace(state.AiProjectDescriptionInput) ? null : state.AiProjectDescriptionInput.Trim());
    }
}
