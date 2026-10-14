namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing GitHub authentication credentials (username and access token).
/// </summary>
public record GitProfileCredentials
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string AccessToken { get; init; } = string.Empty;
}
