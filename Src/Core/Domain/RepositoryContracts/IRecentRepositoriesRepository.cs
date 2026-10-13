namespace GitWerka.Core.Domain.RepositoryContracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Contract for persisting and retrieving recently opened repositories.
/// </summary>
public interface IRecentRepositoriesRepository
{
    /// <summary>
    /// Retrieves the collection of recently opened repositories ordered by pinned status and last opened timestamp.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of recent repository models.</returns>
    Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new repository entry or updates an existing repository's last opened timestamp.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repository">The repository domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a repository from the recent history by path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task RemoveRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the pinned status of a repository in the history list.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task TogglePinRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
