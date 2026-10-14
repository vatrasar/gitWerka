namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity for storing global AI configuration settings.
/// </summary>
public class AiConfigurationEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(512)]
    public string? ApiKey { get; set; }

    [Required]
    [MaxLength(256)]
    public string Model { get; set; } = "google/gemini-2.5-flash";

    [Required]
    [MaxLength(512)]
    public string CommitStyle { get; set; } = "Conventional Commits: <type>(<scope>): <subject>";

    public int TokenLimit { get; set; } = 4000;

    [Required]
    [MaxLength(512)]
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
}
