namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing a Git profile combining an author identity and remote credentials.
/// </summary>
public record GitProfile
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int AuthorId { get; init; }

    public GitAuthor? Author { get; init; }

    public int CredentialsId { get; init; }

    public GitProfileCredentials? Credentials { get; init; }
}
