namespace GitWerka.Infrastructure.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Database entity associating a Git author and remote credentials into a named profile.
/// </summary>
public class GitProfileEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [ForeignKey(nameof(Author))]
    public int AuthorId { get; set; }

    public GitAuthorEntity? Author { get; set; }

    [ForeignKey(nameof(Credentials))]
    public int CredentialsId { get; set; }

    public GitProfileCredentialsEntity? Credentials { get; set; }
}
