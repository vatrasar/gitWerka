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

    /// <summary>
    /// Retrieves a single recent repository by path if it exists.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Repository domain model or null.</returns>
    Task<RecentRepository?> GetRepositoryByPathAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or clears favorite status and optional custom display name for a repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="isFavorite">True to add to favorites, false to remove.</param>
    /// <param name="customName">Optional custom display name when adding to favorites.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SetFavoriteAsync(string repositoryPath, bool isFavorite, string? customName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all favorite repositories ordered by last opened timestamp.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of favorite repository domain models.</returns>
    Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recently opened repository that is not marked as favorite.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Recent repository model or null if none exist.</returns>
    Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default);
}
