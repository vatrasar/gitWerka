namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing application-wide user preferences and settings.
/// </summary>
public class ApplicationSettings
{
    public string DefaultProjectsDirectory { get; set; } = string.Empty;

    public int MaxVisibleBranches { get; set; } = 5;

    public int AutoFetchIntervalSeconds { get; set; } = 60;
}

