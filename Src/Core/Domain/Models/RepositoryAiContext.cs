namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing AI context and project description for a specific repository.
/// </summary>
public record RepositoryAiContext
{
    public string RepositoryPath { get; init; } = string.Empty;

    public string? ProjectDescription { get; init; }
}
