namespace GitWerka.Infrastructure.Data.Entities;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Database entity representing a recently opened repository.
/// </summary>
public class RecentRepositoryEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1024)]
    public string Path { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public DateTime LastOpenedAtUtc { get; set; }

    public bool IsPinned { get; set; }

    public bool IsFavorite { get; set; }

    [MaxLength(256)]
    public string? CustomName { get; set; }

    [MaxLength(256)]
    public string? GroupName { get; set; }
}
