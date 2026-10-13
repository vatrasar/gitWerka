namespace GitWerka.Features.Shell.UI.Screens.Host;

/// <summary>
/// Immutable state record for the top-level Shell Host screen.
/// </summary>
public record HostState
{
    public string CurrentRepositoryName { get; init; } = string.Empty;

    public string CurrentRepositoryPath { get; init; } = string.Empty;

    public bool IsRepositoryLoaded { get; init; }

    public string StatusMessage { get; init; } = "Ready";
}
