using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;

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

    public ImmutableList<GitProfile> AvailableProfiles { get; init; } = ImmutableList<GitProfile>.Empty;

    public GitProfile? ActiveProfile { get; init; }

    public bool IsSettingsButtonVisible { get; init; } = true;

    public string AppVersion { get; init; } = "v1.0.0";

    public string FooterInfoText => IsRepositoryLoaded && !string.IsNullOrWhiteSpace(CurrentRepositoryPath)
        ? CurrentRepositoryPath
        : AppVersion;
}
