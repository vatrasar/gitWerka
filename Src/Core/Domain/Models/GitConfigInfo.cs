namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing Git user identity configuration.
/// </summary>
public record GitConfigInfo
{
    public string UserName { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;
}
