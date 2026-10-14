namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity representing Git remote credentials (GitHub username and personal access token).
/// </summary>
public class GitProfileCredentialsEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string AccessToken { get; set; } = string.Empty;
}
