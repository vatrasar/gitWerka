namespace GitWerka.Infrastructure.Data.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core repository implementation for Git profiles, authors, and credentials.
/// </summary>
public class ProfileRepository : IProfileRepository
{
    private readonly GitWerkaDbContext _dbContext;

    public ProfileRepository(GitWerkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves all configured Git profiles with authors and credentials loaded.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of GitProfile domain models.</returns>
    public async Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        List<GitProfileEntity> entities = await _dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Credentials)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapProfileToDomain).ToList();
    }

    /// <summary>
    /// Retrieves a specific profile by unique identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Profile identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitProfile model or null if not found.</returns>
    public async Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        GitProfileEntity? entity = await _dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Credentials)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return entity == null ? null : MapProfileToDomain(entity);
    }

    /// <summary>
    /// Adds a new Git profile or updates an existing profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="profile">The profile domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitProfile domain model with populated ID.</returns>
    public async Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile.Id == 0)
        {
            return await InsertProfileAsync(profile, cancellationToken);
        }

        return await UpdateProfileAsync(profile, cancellationToken);
    }

    /// <summary>
    /// Removes a Git profile and any repository active mappings pointing to it.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Identifier of the profile to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        GitProfileEntity? entity = await _dbContext.Profiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity == null)
        {
            return;
        }

        _dbContext.Profiles.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all configured authors for commit signatures.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of GitAuthor models.</returns>
    public async Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default)
    {
        List<GitAuthorEntity> entities = await _dbContext.Authors
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapAuthorToDomain).ToList();
    }

    /// <summary>
    /// Retrieves an author by unique identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Author identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitAuthor model or null if not found.</returns>
    public async Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        GitAuthorEntity? entity = await _dbContext.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity == null ? null : MapAuthorToDomain(entity);
    }

    /// <summary>
    /// Adds or updates a Git author model.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="author">The author domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitAuthor model.</returns>
    public async Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default)
    {
        if (author.Id == 0)
        {
            return await InsertAuthorAsync(author, cancellationToken);
        }

        return await UpdateAuthorAsync(author, cancellationToken);
    }

    /// <summary>
    /// Deletes an author if not in use by any profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Author identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default)
    {
        bool isInUse = await _dbContext.Profiles.AnyAsync(p => p.AuthorId == id, cancellationToken);
        if (isInUse)
        {
            throw new InvalidOperationException("Author is assigned to one or more profiles and cannot be deleted.");
        }

        GitAuthorEntity? entity = await _dbContext.Authors
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity == null)
        {
            return;
        }

        _dbContext.Authors.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all configured remote credentials.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of GitProfileCredentials models.</returns>
    public async Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default)
    {
        List<GitProfileCredentialsEntity> entities = await _dbContext.ProfileCredentials
            .AsNoTracking()
            .OrderBy(c => c.Username)
            .ToListAsync(cancellationToken);

        return entities.Select(MapCredentialsToDomain).ToList();
    }

    /// <summary>
    /// Retrieves remote credentials by identifier.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Credentials identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Matching GitProfileCredentials model or null if not found.</returns>
    public async Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        GitProfileCredentialsEntity? entity = await _dbContext.ProfileCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapCredentialsToDomain(entity);
    }

    /// <summary>
    /// Adds or updates remote credentials.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="credentials">The credentials model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The saved GitProfileCredentials model.</returns>
    public async Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default)
    {
        if (credentials.Id == 0)
        {
            return await InsertCredentialsAsync(credentials, cancellationToken);
        }

        return await UpdateCredentialsAsync(credentials, cancellationToken);
    }

    /// <summary>
    /// Deletes remote credentials if not in use by any profile.
    /// Usages: ProfileManagementViewModel.
    /// </summary>
    /// <param name="id">Credentials identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default)
    {
        bool isInUse = await _dbContext.Profiles.AnyAsync(p => p.CredentialsId == id, cancellationToken);
        if (isInUse)
        {
            throw new InvalidOperationException("Credentials are assigned to one or more profiles and cannot be deleted.");
        }

        GitProfileCredentialsEntity? entity = await _dbContext.ProfileCredentials
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            return;
        }

        _dbContext.ProfileCredentials.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the active profile mapped to a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The active GitProfile if one is associated; otherwise null.</returns>
    public async Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return null;
        }

        RepositoryProfileMappingEntity? mapping = await _dbContext.RepositoryProfiles
            .AsNoTracking()
            .Include(m => m.Profile)
            .ThenInclude(p => p!.Author)
            .Include(m => m.Profile)
            .ThenInclude(p => p!.Credentials)
            .FirstOrDefaultAsync(m => m.RepositoryPath == repositoryPath, cancellationToken);

        return mapping?.Profile == null ? null : MapProfileToDomain(mapping.Profile);
    }

    /// <summary>
    /// Sets the active profile for a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="profileId">Identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return;
        }

        RepositoryProfileMappingEntity? existing = await _dbContext.RepositoryProfiles
            .FirstOrDefaultAsync(m => m.RepositoryPath == repositoryPath, cancellationToken);

        if (existing != null)
        {
            existing.ProfileId = profileId;
        }
        else
        {
            RepositoryProfileMappingEntity mapping = new()
            {
                RepositoryPath = repositoryPath,
                ProfileId = profileId
            };
            await _dbContext.RepositoryProfiles.AddAsync(mapping, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the active profile mapping for a specific repository path.
    /// Usages: CommitLogViewModel, ProfileManagementViewModel.
    /// </summary>
    /// <param name="repositoryPath">Filesystem path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return;
        }

        RepositoryProfileMappingEntity? existing = await _dbContext.RepositoryProfiles
            .FirstOrDefaultAsync(m => m.RepositoryPath == repositoryPath, cancellationToken);

        if (existing == null)
        {
            return;
        }

        _dbContext.RepositoryProfiles.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GitProfile> InsertProfileAsync(GitProfile profile, CancellationToken cancellationToken)
    {
        GitProfileEntity entity = new()
        {
            Name = profile.Name.Trim(),
            AuthorId = profile.AuthorId,
            CredentialsId = profile.CredentialsId
        };

        await _dbContext.Profiles.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await FetchPopulatedProfileAsync(entity.Id, cancellationToken);
    }

    private async Task<GitProfile> UpdateProfileAsync(GitProfile profile, CancellationToken cancellationToken)
    {
        GitProfileEntity? entity = await _dbContext.Profiles
            .FirstOrDefaultAsync(p => p.Id == profile.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Profile with ID {profile.Id} was not found.");
        }

        entity.Name = profile.Name.Trim();
        entity.AuthorId = profile.AuthorId;
        entity.CredentialsId = profile.CredentialsId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await FetchPopulatedProfileAsync(entity.Id, cancellationToken);
    }

    private async Task<GitProfile> FetchPopulatedProfileAsync(int id, CancellationToken cancellationToken)
    {
        GitProfileEntity populated = await _dbContext.Profiles
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Credentials)
            .FirstAsync(p => p.Id == id, cancellationToken);

        return MapProfileToDomain(populated);
    }

    private async Task<GitAuthor> InsertAuthorAsync(GitAuthor author, CancellationToken cancellationToken)
    {
        GitAuthorEntity entity = new()
        {
            Name = author.Name.Trim(),
            Email = author.Email.Trim()
        };

        await _dbContext.Authors.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAuthorToDomain(entity);
    }

    private async Task<GitAuthor> UpdateAuthorAsync(GitAuthor author, CancellationToken cancellationToken)
    {
        GitAuthorEntity? entity = await _dbContext.Authors
            .FirstOrDefaultAsync(a => a.Id == author.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Author with ID {author.Id} was not found.");
        }

        entity.Name = author.Name.Trim();
        entity.Email = author.Email.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAuthorToDomain(entity);
    }

    private async Task<GitProfileCredentials> InsertCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken)
    {
        GitProfileCredentialsEntity entity = new()
        {
            Username = credentials.Username.Trim(),
            AccessToken = credentials.AccessToken.Trim()
        };

        await _dbContext.ProfileCredentials.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCredentialsToDomain(entity);
    }

    private async Task<GitProfileCredentials> UpdateCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken)
    {
        GitProfileCredentialsEntity? entity = await _dbContext.ProfileCredentials
            .FirstOrDefaultAsync(c => c.Id == credentials.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Credentials with ID {credentials.Id} was not found.");
        }

        entity.Username = credentials.Username.Trim();
        entity.AccessToken = credentials.AccessToken.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCredentialsToDomain(entity);
    }

    private static GitProfile MapProfileToDomain(GitProfileEntity entity)
    {
        return new GitProfile
        {
            Id = entity.Id,
            Name = entity.Name,
            AuthorId = entity.AuthorId,
            Author = entity.Author == null ? null : MapAuthorToDomain(entity.Author),
            CredentialsId = entity.CredentialsId,
            Credentials = entity.Credentials == null ? null : MapCredentialsToDomain(entity.Credentials)
        };
    }

    private static GitAuthor MapAuthorToDomain(GitAuthorEntity entity)
    {
        return new GitAuthor
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email
        };
    }

    private static GitProfileCredentials MapCredentialsToDomain(GitProfileCredentialsEntity entity)
    {
        return new GitProfileCredentials
        {
            Id = entity.Id,
            Username = entity.Username,
            AccessToken = entity.AccessToken
        };
    }
}
