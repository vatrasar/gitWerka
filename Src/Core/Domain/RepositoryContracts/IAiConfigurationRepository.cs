namespace GitWerka.Core.Domain.RepositoryContracts;

using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Repository contract for retrieving and persisting AI configuration and per-repository context.
/// </summary>
public interface IAiConfigurationRepository
{
    /// <summary>
    /// Retrieves the current global AI integration configuration.
    /// Usages: OpenRouterAiCommitMessageService, CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>AiConfiguration domain model containing API settings.</returns>
    Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the global AI integration configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="configuration">Configuration model with updated values.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SaveConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the custom project description configured for a specific repository path.
    /// Usages: OpenRouterAiCommitMessageService, CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Project description text if configured; otherwise null.</returns>
    Task<string?> GetProjectDescriptionAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the project description for a specific repository path.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="description">Project description text.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SaveProjectDescriptionAsync(string repositoryPath, string? description, CancellationToken cancellationToken = default);
}
