namespace GitWerka.Core.Domain.RepositoryContracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Repository contract for managing Git profiles, authors, and remote credentials persistence.
/// </summary>
public interface IProfileRepository
{
    /// <summary>
    /// Retrieves all configured Git profiles with populated author and credentials data.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of Git profiles.</returns>
    Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific profile by unique identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Profile identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitProfile model or null if not found.</returns>
    Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new Git profile or updates an existing profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="profile">The profile domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitProfile domain model with populated ID.</returns>
    Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a Git profile and any repository active mappings pointing to it.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Identifier of the profile to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all configured authors for commit signatures.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of GitAuthor models.</returns>
    Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an author by unique identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Author identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitAuthor model or null if not found.</returns>
    Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a Git author model.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="author">The author domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitAuthor model.</returns>
    Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an author if not in use by any profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Author identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all configured remote credentials.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of GitProfileCredentials models.</returns>
    Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves remote credentials by identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Credentials identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitProfileCredentials model or null if not found.</returns>
    Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates remote credentials.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="credentials">The credentials model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitProfileCredentials model.</returns>
    Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes remote credentials if not in use by any profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Credentials identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active profile mapped to a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The active GitProfile if one is associated; otherwise null.</returns>
    Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active profile for a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="profileId">Identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the active profile mapping for a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
