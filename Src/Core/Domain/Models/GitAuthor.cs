namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a Git commit author for signature generation.
/// </summary>
public record GitAuthor
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
