namespace GitWerka.Core.Domain.RepositoryContracts;

using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Repository contract for persisting application-wide user configuration and preferences.
/// </summary>
public interface IApplicationSettingsRepository
{
    /// <summary>
    /// Retrieves current application settings or default values.
    /// Usages: RepositoryManagerViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async execution.</param>
    /// <returns>ApplicationSettings domain model.</returns>
    Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists updated application settings.
    /// Usages: RepositoryManagerViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="settings">Settings model containing updated values.</param>
    /// <param name="cancellationToken">Cancellation token for async execution.</param>
    /// <returns>Task representing the async save operation.</returns>
    Task SaveSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
