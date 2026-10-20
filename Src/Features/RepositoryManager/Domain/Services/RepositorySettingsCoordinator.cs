namespace GitWerka.Features.RepositoryManager.Domain.Services;

using System;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;

/// <summary>
/// Service coordinating settings loading, preparation, and saving for RepositoryManager.
/// </summary>
public class RepositorySettingsCoordinator
{
    private readonly IApplicationSettingsRepository? _appSettingsRepo;
    private readonly IAiConfigurationRepository? _aiConfigRepo;

    public RepositorySettingsCoordinator(
        IApplicationSettingsRepository? appSettingsRepo,
        IAiConfigurationRepository? aiConfigRepo)
    {
        _appSettingsRepo = appSettingsRepo;
        _aiConfigRepo = aiConfigRepo;
    }

    /// <summary>
    /// Loads application settings from repository.
    /// </summary>
    public async Task<string> LoadDefaultProjectsDirectoryAsync()
    {
        if (_appSettingsRepo == null)
        {
            return string.Empty;
        }

        var settings = await _appSettingsRepo.GetSettingsAsync();
        return settings.DefaultProjectsDirectory ?? string.Empty;
    }

    /// <summary>
    /// Loads AI and application settings for editing in modal dialog.
    /// </summary>
    public async Task<(string DefaultDir, string ApiKey, string Model, string CommitStyle, string TokenLimit)> LoadModalSettingsAsync(string fallbackDefaultDir)
    {
        var aiConfig = _aiConfigRepo != null
            ? await _aiConfigRepo.GetConfigurationAsync()
            : new AiConfiguration();

        var appSettings = _appSettingsRepo != null
            ? await _appSettingsRepo.GetSettingsAsync()
            : new ApplicationSettings { DefaultProjectsDirectory = fallbackDefaultDir };

        return (
            appSettings.DefaultProjectsDirectory ?? string.Empty,
            aiConfig.ApiKey ?? string.Empty,
            aiConfig.Model,
            aiConfig.CommitStyle,
            aiConfig.TokenLimit.ToString()
        );
    }

    /// <summary>
    /// Saves updated settings from modal dialog.
    /// </summary>
    public async Task<string> SaveModalSettingsAsync(string defaultDirInput, string apiKeyInput, string modelInput, string styleInput, string tokenLimitInput)
    {
        string trimmedDir = defaultDirInput?.Trim() ?? string.Empty;

        if (_appSettingsRepo != null)
        {
            await _appSettingsRepo.SaveSettingsAsync(new ApplicationSettings
            {
                DefaultProjectsDirectory = trimmedDir
            });
        }

        if (_aiConfigRepo != null)
        {
            int.TryParse(tokenLimitInput, out int tokenLimit);
            if (tokenLimit <= 0)
            {
                tokenLimit = 4000;
            }

            AiConfiguration updatedAiConfig = new()
            {
                ApiKey = string.IsNullOrWhiteSpace(apiKeyInput) ? null : apiKeyInput.Trim(),
                Model = string.IsNullOrWhiteSpace(modelInput) ? "google/gemini-2.5-flash" : modelInput.Trim(),
                CommitStyle = string.IsNullOrWhiteSpace(styleInput) ? "Conventional Commits: <type>(<scope>): <subject>" : styleInput.Trim(),
                TokenLimit = tokenLimit
            };

            await _aiConfigRepo.SaveConfigurationAsync(updatedAiConfig);
        }

        return trimmedDir;
    }
}
