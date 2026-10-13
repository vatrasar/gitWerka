namespace GitWerka.Core.Config;

/// <summary>
/// Strongly-typed configuration options for the application.
/// </summary>
public class AppConfig
{
    public string ApplicationName { get; set; } = "GitWerka";

    public string DatabaseFileName { get; set; } = "gitwerka.db";

    public int MaxRecentRepositories { get; set; } = 20;
}
