namespace GitWerka.Core.Domain.Enums;

/// <summary>
/// Type of credential required for remote Git operations.
/// </summary>
public enum GitCredentialType
{
    Https,
    SshKeyPassphrase
}
