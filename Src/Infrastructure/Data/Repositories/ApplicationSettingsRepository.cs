namespace GitWerka.Infrastructure.Data.Repositories;

using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core repository implementation for application configuration.
/// </summary>
public class ApplicationSettingsRepository : IApplicationSettingsRepository
{
    private readonly GitWerkaDbContext _dbContext;

    public ApplicationSettingsRepository(GitWerkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves application settings from SQLite database.
    /// Usages: RepositoryManagerViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ApplicationSettings domain model.</returns>
    public async Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        ApplicationSettingsEntity? entity = await _dbContext.ApplicationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return new ApplicationSettings();
        }

        return new ApplicationSettings
        {
            DefaultProjectsDirectory = entity.DefaultProjectsDirectory ?? string.Empty,
            MaxVisibleBranches = entity.MaxVisibleBranches > 0 ? entity.MaxVisibleBranches : 5
        };
    }

    /// <summary>
    /// Saves or updates application settings in SQLite database.
    /// Usages: RepositoryManagerViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="settings">Application settings to persist.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Task tracking async persistence.</returns>
    public async Task SaveSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        ApplicationSettingsEntity? entity = await _dbContext.ApplicationSettings
            .FirstOrDefaultAsync(cancellationToken);

        int branchLimit = settings.MaxVisibleBranches > 0 ? settings.MaxVisibleBranches : 5;

        if (entity != null)
        {
            entity.DefaultProjectsDirectory = settings.DefaultProjectsDirectory ?? string.Empty;
            entity.MaxVisibleBranches = branchLimit;
        }
        else
        {
            entity = new ApplicationSettingsEntity
            {
                DefaultProjectsDirectory = settings.DefaultProjectsDirectory ?? string.Empty,
                MaxVisibleBranches = branchLimit
            };
            await _dbContext.ApplicationSettings.AddAsync(entity, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
