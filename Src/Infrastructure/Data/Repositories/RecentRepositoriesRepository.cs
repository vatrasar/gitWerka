namespace GitWerka.Infrastructure.Data.Repositories;

using System;
using System.Collections.Generic;
using System.IO;
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

        return entities
            .GroupBy(e => NormalizePath(e.Path))
            .Select(g => g.First())
            .Select(MapToDomain)
            .ToList();
    }

    /// <summary>
    /// Adds a new repository entry or updates an existing repository's last opened timestamp.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="repository">The repository domain model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePath(repository.Path);
        string withSlash = normalizedPath + Path.DirectorySeparatorChar;

        List<RecentRepositoryEntity> existingEntities = await _dbContext.RecentRepositories
            .Where(r => r.Path == normalizedPath || r.Path == withSlash || r.Path == repository.Path)
            .ToListAsync(cancellationToken);

        RecentRepository normalizedRepo = repository with { Path = normalizedPath };

        if (existingEntities.Count > 0)
        {
            RecentRepositoryEntity primary = existingEntities[0];
            primary.Path = normalizedPath;
            UpdateExistingEntity(primary, normalizedRepo);

            if (existingEntities.Count > 1)
            {
                MergeDuplicates(primary, existingEntities.Skip(1));
            }
        }
        else
        {
            await InsertNewEntityAsync(normalizedRepo, cancellationToken);
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
        string normalizedPath = NormalizePath(repositoryPath);
        string withSlash = normalizedPath + Path.DirectorySeparatorChar;

        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .Where(r => r.Path == normalizedPath || r.Path == withSlash || r.Path == repositoryPath)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return;
        }

        _dbContext.RecentRepositories.RemoveRange(entities);
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
        string normalizedPath = NormalizePath(repositoryPath);
        string withSlash = normalizedPath + Path.DirectorySeparatorChar;

        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .Where(r => r.Path == normalizedPath || r.Path == withSlash || r.Path == repositoryPath)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            return;
        }

        RecentRepositoryEntity primary = entities[0];
        primary.Path = normalizedPath;
        primary.IsPinned = !primary.IsPinned;

        if (entities.Count > 1)
        {
            _dbContext.RecentRepositories.RemoveRange(entities.Skip(1));
        }

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
        string normalizedPath = NormalizePath(repositoryPath);
        string withSlash = normalizedPath + Path.DirectorySeparatorChar;

        RecentRepositoryEntity? entity = await _dbContext.RecentRepositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Path == normalizedPath || r.Path == withSlash || r.Path == repositoryPath, cancellationToken);

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
        string normalizedPath = NormalizePath(repositoryPath);
        string withSlash = normalizedPath + Path.DirectorySeparatorChar;

        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .Where(r => r.Path == normalizedPath || r.Path == withSlash || r.Path == repositoryPath)
            .ToListAsync(cancellationToken);

        if (entities.Count == 0)
        {
            await CreateAndAddFavoriteEntityAsync(normalizedPath, isFavorite, customName, groupName, cancellationToken);
        }
        else
        {
            RecentRepositoryEntity primary = entities[0];
            primary.Path = normalizedPath;
            UpdateFavoriteProperties(primary, isFavorite, customName, groupName);

            if (entities.Count > 1)
            {
                _dbContext.RecentRepositories.RemoveRange(entities.Skip(1));
            }
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

        return entities
            .GroupBy(e => NormalizePath(e.Path))
            .Select(g => g.First())
            .Select(MapToDomain)
            .ToList();
    }

    /// <summary>
    /// Retrieves the most recently opened repository that is not marked as favorite.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Recent repository model or null if none exist.</returns>
    public async Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default)
    {
        List<RecentRepositoryEntity> entities = await _dbContext.RecentRepositories
            .AsNoTracking()
            .Where(r => !r.IsFavorite)
            .OrderByDescending(r => r.LastOpenedAtUtc)
            .ToListAsync(cancellationToken);

        RecentRepositoryEntity? entity = entities
            .GroupBy(e => NormalizePath(e.Path))
            .Select(g => g.First())
            .FirstOrDefault();

        return entity != null ? MapToDomain(entity) : null;
    }


    private static RecentRepository MapToDomain(RecentRepositoryEntity entity)
    {
        return new RecentRepository
        {
            Path = NormalizePath(entity.Path),
            Name = entity.Name,
            LastOpenedAtUtc = entity.LastOpenedAtUtc,
            IsPinned = entity.IsPinned,
            IsFavorite = entity.IsFavorite,
            CustomName = entity.CustomName,
            GroupName = entity.GroupName
        };
    }

    private static string NormalizePath(string path) =>
        string.IsNullOrWhiteSpace(path) ? string.Empty : Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private void MergeDuplicates(RecentRepositoryEntity primary, IEnumerable<RecentRepositoryEntity> duplicates)
    {
        foreach (RecentRepositoryEntity duplicate in duplicates)
        {
            if (duplicate.IsPinned)
            {
                primary.IsPinned = true;
            }

            if (duplicate.IsFavorite)
            {
                primary.IsFavorite = true;
                primary.CustomName ??= duplicate.CustomName;
                primary.GroupName ??= duplicate.GroupName;
            }

            _dbContext.RecentRepositories.Remove(duplicate);
        }
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
        string normalized = NormalizePath(path);
        string dirName = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(dirName))
        {
            dirName = normalized;
        }

        RecentRepositoryEntity entity = new()
        {
            Path = normalized,
            Name = dirName,
            LastOpenedAtUtc = DateTime.UtcNow,
            IsFavorite = isFavorite,
            CustomName = isFavorite ? customName : null,
            GroupName = isFavorite ? groupName : null
        };

        await _dbContext.RecentRepositories.AddAsync(entity, cancellationToken);
    }
}
