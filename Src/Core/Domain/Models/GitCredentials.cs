namespace GitWerka.Core.Domain.Models;

using GitWerka.Core.Domain.Enums;

/// <summary>
/// Domain model containing authentication credentials for remote Git operations.
/// </summary>
public record GitCredentials
{
    public GitCredentialType Type { get; init; } = GitCredentialType.Https;

    public string Username { get; init; } = string.Empty;

    public string PasswordOrToken { get; init; } = string.Empty;

    public string Passphrase { get; init; } = string.Empty;
}
