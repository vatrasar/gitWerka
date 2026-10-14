namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity representing a Git commit author.
/// </summary>
public class GitAuthorEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
