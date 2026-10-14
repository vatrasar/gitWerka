namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Database entity associating a repository path with an active profile.
/// </summary>
public class RepositoryProfileMappingEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1024)]
    public string RepositoryPath { get; set; } = string.Empty;

    [ForeignKey(nameof(Profile))]
    public int ProfileId { get; set; }

    public GitProfileEntity? Profile { get; set; }
}
