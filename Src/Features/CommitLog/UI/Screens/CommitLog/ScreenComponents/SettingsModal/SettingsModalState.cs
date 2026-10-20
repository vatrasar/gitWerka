namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.SettingsModal;

/// <summary>
/// Immutable state record for the SettingsModal smart component.
/// </summary>
public record SettingsModalState
{
    public bool IsVisible { get; init; }

    public string RepositoryPath { get; init; } = string.Empty;

    public string ConfigUserNameInput { get; init; } = string.Empty;

    public string ConfigUserEmailInput { get; init; } = string.Empty;

    public string DefaultProjectsDirectory { get; init; } = string.Empty;

    public string DefaultProjectsDirectoryInput { get; init; } = string.Empty;

    public string MaxVisibleBranchesInput { get; init; } = "5";

    public string AutoFetchIntervalInput { get; init; } = "60";

    public string AiApiKeyInput { get; init; } = string.Empty;

    public string AiModelInput { get; init; } = "google/gemini-2.5-flash";

    public string AiCommitStyleInput { get; init; } = "Conventional Commits: <type>(<scope>): <subject>";

    public string AiTokenLimitInput { get; init; } = "4000";

    public string AiProjectDescriptionInput { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }
}
