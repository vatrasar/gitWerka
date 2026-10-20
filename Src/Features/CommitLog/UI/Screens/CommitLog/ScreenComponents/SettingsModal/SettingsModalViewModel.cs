namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.SettingsModal;

using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// Smart component view model managing repository and application settings modal.
/// </summary>
public partial class SettingsModalViewModel : ViewModelBase<SettingsModalState>
{
    private readonly IGitService _gitService;
    private readonly IApplicationSettingsRepository? _applicationSettingsRepository;
    private readonly IAiConfigurationRepository? _aiConfigurationRepository;
    private readonly Subject<Unit> _settingsSavedSubject = new();

    public IObservable<Unit> SettingsSaved => _settingsSavedSubject;

    public SettingsModalViewModel(
        IGitService gitService,
        IApplicationSettingsRepository? applicationSettingsRepository,
        IAiConfigurationRepository? aiConfigurationRepository,
        string initialRepositoryPath = "")
        : base(new SettingsModalState { RepositoryPath = initialRepositoryPath })
    {
        _gitService = gitService;
        _applicationSettingsRepository = applicationSettingsRepository;
        _aiConfigurationRepository = aiConfigurationRepository;
    }

    /// <summary>
    /// Updates the active repository path in state.
    /// Usages: CommitLogViewModel on repository switch.
    /// </summary>
    /// <param name="path">Current repository path.</param>
    public void SetRepositoryPath(string path)
    {
        UpdateState(s => s with { RepositoryPath = path ?? string.Empty });
    }

    /// <summary>
    /// Updates user name configuration input in settings modal.
    /// Usages: SettingsModalView user name text box.
    /// </summary>
    /// <param name="text">Author user name.</param>
    public void UpdateConfigUserNameInput(string text)
    {
        UpdateState(s => s with { ConfigUserNameInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates user email configuration input in settings modal.
    /// Usages: SettingsModalView user email text box.
    /// </summary>
    /// <param name="text">Author email address.</param>
    public void UpdateConfigUserEmailInput(string text)
    {
        UpdateState(s => s with { ConfigUserEmailInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates default projects directory input in settings modal.
    /// Usages: SettingsModalView default projects directory text box.
    /// </summary>
    /// <param name="text">Default directory path string.</param>
    public void UpdateDefaultProjectsDirectoryInput(string text)
    {
        UpdateState(s => s with { DefaultProjectsDirectoryInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates maximum visible branches input in settings modal.
    /// Usages: SettingsModalView maximum visible branches text box.
    /// </summary>
    /// <param name="text">Maximum visible branches count string.</param>
    public void UpdateMaxVisibleBranchesInput(string text)
    {
        UpdateState(s => s with { MaxVisibleBranchesInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates automatic background fetch interval input in settings modal.
    /// Usages: SettingsModalView auto-fetch interval text box.
    /// </summary>
    /// <param name="text">Auto-fetch interval in seconds string.</param>
    public void UpdateAutoFetchIntervalInput(string text)
    {
        UpdateState(s => s with { AutoFetchIntervalInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates OpenRouter API key configuration input in settings modal.
    /// Usages: SettingsModalView API key text box.
    /// </summary>
    /// <param name="text">API key string.</param>
    public void UpdateAiApiKeyInput(string text)
    {
        UpdateState(s => s with { AiApiKeyInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates AI model identifier input in settings modal.
    /// Usages: SettingsModalView model identifier text box.
    /// </summary>
    /// <param name="text">Model identifier.</param>
    public void UpdateAiModelInput(string text)
    {
        UpdateState(s => s with { AiModelInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates requested commit style input in settings modal.
    /// Usages: SettingsModalView commit style text box.
    /// </summary>
    /// <param name="text">Commit style instructions.</param>
    public void UpdateAiCommitStyleInput(string text)
    {
        UpdateState(s => s with { AiCommitStyleInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates token limit input in settings modal.
    /// Usages: SettingsModalView token limit text box.
    /// </summary>
    /// <param name="text">Token limit number string.</param>
    public void UpdateAiTokenLimitInput(string text)
    {
        UpdateState(s => s with { AiTokenLimitInput = text ?? string.Empty });
    }

    /// <summary>
    /// Updates project description input for the active repository in settings modal.
    /// Usages: SettingsModalView project description text box.
    /// </summary>
    /// <param name="text">Project description context.</param>
    public void UpdateAiProjectDescriptionInput(string text)
    {
        UpdateState(s => s with { AiProjectDescriptionInput = text ?? string.Empty });
    }

    /// <summary>
    /// Loads application settings into state.
    /// Usages: SettingsModalViewModel initialization, CommitLogViewModel startup.
    /// </summary>
    /// <returns>Task tracking async load.</returns>
    public async Task LoadApplicationSettingsAsync()
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
                IsVisible = true,
                ConfigUserNameInput = config.UserName,
                ConfigUserEmailInput = config.UserEmail,
                DefaultProjectsDirectory = appSettings.DefaultProjectsDirectory,
                DefaultProjectsDirectoryInput = appSettings.DefaultProjectsDirectory,
                MaxVisibleBranchesInput = (appSettings.MaxVisibleBranches > 0 ? appSettings.MaxVisibleBranches : 5).ToString(),
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
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void HideSettingsModal()
    {
        UpdateState(s => s with { IsVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteSaveSettings()
    {
        UpdateState(s => s with { IsVisible = false });
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

                int fetchInterval = 60;
                if (int.TryParse(State.AutoFetchIntervalInput, out int parsedInterval) && parsedInterval >= 0)
                {
                    fetchInterval = parsedInterval;
                }

                await _applicationSettingsRepository.SaveSettingsAsync(new ApplicationSettings
                {
                    MaxVisibleBranches = maxBranches,
                    DefaultProjectsDirectory = trimmedDir,
                    AutoFetchIntervalSeconds = fetchInterval
                });
            }

            UpdateState(s => s with
            {
                DefaultProjectsDirectory = trimmedDir
            });

            await SaveAiSettingsAsync();
            _settingsSavedSubject.OnNext(Unit.Default);
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
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
}
