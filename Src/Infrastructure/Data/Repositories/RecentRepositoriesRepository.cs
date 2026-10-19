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
/// Entity Framework Core implementation of the recent repositories repository contract.
/// </summary>
public class RecentRepositoriesRepository : IRecentRepositoriesRepository
{
    private readonly GitWerkaDbContext _dbContext;

    public RecentRepositoriesRepository(GitWerkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves the collection of recently opened repositories ordered by pinned status and last opened timestamp.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of recent repository models.</returns>
    public async Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .AsNoTracking()
            .OrderByDescending(r => r.IsPinned)
            .ThenByDescending(r => r.LastOpenedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    /// <summary>
    /// Adds a new repository entry or updates an existing repository's last opened timestamp.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repository">The repository domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? existingEntity = await _dbContext.RecentRepositories
            .FirstOrDefaultAsync(r => r.Path == repository.Path, cancellationToken);

        if (existingEntity != null)
        {
            UpdateExistingEntity(existingEntity, repository);
        }
        else
        {
            await InsertNewEntityAsync(repository, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Removes a repository from the recent history by path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task RemoveRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .FirstOrDefaultAsync(r => r.Path == repositoryPath, cancellationToken);

        if (entity == null)
        {
            return;
        }

        _dbContext.RecentRepositories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Toggles the pinned status of a repository in the history list.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task TogglePinRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .FirstOrDefaultAsync(r => r.Path == repositoryPath, cancellationToken);

        if (entity == null)
        {
            return;
        }

        entity.IsPinned = !entity.IsPinned;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a single recent repository by path if it exists.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Repository domain model or null.</returns>
    public async Task<RecentRepository?> GetRepositoryByPathAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Path == repositoryPath, cancellationToken);

        return entity != null ? MapToDomain(entity) : null;
    }

    /// <summary>
    /// Sets or clears favorite status, optional custom display name, and optional group name for a repository.
    /// Usages: CommitLogViewModel, RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repositoryPath">The file system path of the repository.</param>
    /// <param name="isFavorite">True to add to favorites, false to remove.</param>
    /// <param name="customName">Optional custom display name when adding to favorites.</param>
    /// <param name="groupName">Optional group name for categorizing favorite repositories.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task SetFavoriteAsync(string repositoryPath, bool isFavorite, string? customName = null, string? groupName = null, CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .FirstOrDefaultAsync(r => r.Path == repositoryPath, cancellationToken);

        if (entity == null)
        {
            await CreateAndAddFavoriteEntityAsync(repositoryPath, isFavorite, customName, groupName, cancellationToken);
        }
        else
        {
            UpdateFavoriteProperties(entity, isFavorite, customName, groupName);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all favorite repositories ordered by last opened timestamp.
    /// Usages: CommitLogViewModel, RepositoryManagerViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of favorite repository domain models.</returns>
    public async Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .AsNoTracking()
            .Where(r => r.IsFavorite)
            .OrderByDescending(r => r.LastOpenedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    /// <summary>
    /// Retrieves the most recently opened repository that is not marked as favorite.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Recent repository model or null if none exist.</returns>
    public async Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default)
    {
        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .AsNoTracking()
            .Where(r => !r.IsFavorite)
            .OrderByDescending(r => r.LastOpenedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return entity != null ? MapToDomain(entity) : null;
    }

    private static RecentRepository MapToDomain(RecentRepositoryEntity entity)
    {
        return new RecentRepository
        {
            Path = entity.Path,
            Name = entity.Name,
            LastOpenedAtUtc = entity.LastOpenedAtUtc,
            IsPinned = entity.IsPinned,
            IsFavorite = entity.IsFavorite,
            CustomName = entity.CustomName,
            GroupName = entity.GroupName
        };
    }

    private static void UpdateExistingEntity(RecentRepositoryEntity entity, RecentRepository model)
    {
        entity.Name = model.Name;
        entity.LastOpenedAtUtc = model.LastOpenedAtUtc;
        if (model.IsFavorite)
        {
            entity.IsFavorite = true;
            if (!string.IsNullOrWhiteSpace(model.CustomName))
            {
                entity.CustomName = model.CustomName;
            }
            if (!string.IsNullOrWhiteSpace(model.GroupName))
            {
                entity.GroupName = model.GroupName;
            }
        }
    }

    private async Task InsertNewEntityAsync(RecentRepository model, CancellationToken cancellationToken)
    {
        RecentRepositoryEntity entity = new()
        {
            Path = model.Path,
            Name = model.Name,
            LastOpenedAtUtc = model.LastOpenedAtUtc,
            IsPinned = model.IsPinned,
            IsFavorite = model.IsFavorite,
            CustomName = model.CustomName,
            GroupName = model.GroupName
        };

        await _dbContext.RecentRepositories.AddAsync(entity, cancellationToken);
    }

    private static void UpdateFavoriteProperties(RecentRepositoryEntity entity, bool isFavorite, string? customName, string? groupName)
    {
        entity.IsFavorite = isFavorite;
        entity.CustomName = isFavorite ? customName : null;
        entity.GroupName = isFavorite ? groupName : null;
    }

    private async Task CreateAndAddFavoriteEntityAsync(string path, bool isFavorite, string? customName, string? groupName, CancellationToken cancellationToken)
    {
        string dirName = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        RecentRepositoryEntity entity = new()
        {
            Path = path,
            Name = dirName,
            LastOpenedAtUtc = DateTime.UtcNow,
            IsFavorite = isFavorite,
            CustomName = isFavorite ? customName : null,
            GroupName = isFavorite ? groupName : null
        };

        await _dbContext.RecentRepositories.AddAsync(entity, cancellationToken);
    }
}
