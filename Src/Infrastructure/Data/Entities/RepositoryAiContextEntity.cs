namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity for storing per-repository project descriptions and AI context.
/// </summary>
public class RepositoryAiContextEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1024)]
    public string RepositoryPath { get; set; } = string.Empty;

    public string? ProjectDescription { get; set; }
}
