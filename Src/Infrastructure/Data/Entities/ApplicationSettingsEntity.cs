namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity for persisting global application settings.
/// </summary>
public class ApplicationSettingsEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(1024)]
    public string DefaultProjectsDirectory { get; set; } = string.Empty;

    public int MaxVisibleBranches { get; set; } = 5;

    public int AutoFetchIntervalSeconds { get; set; } = 60;
}
