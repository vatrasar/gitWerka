namespace GitWerka.Infrastructure.Data.Repositories;

using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the AI configuration repository contract.
/// </summary>
public class AiConfigurationRepository : IAiConfigurationRepository
{
    private readonly GitWerkaDbContext _dbContext;

    public AiConfigurationRepository(GitWerkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves the current global AI integration configuration.
    /// Usages: OpenRouterAiCommitMessageService, CommitLogViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>AiConfiguration domain model containing API settings.</returns>
    public async Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        AiConfigurationEntity? entity = await _dbContext.AiConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return new AiConfiguration();
        }

        return MapToDomain(entity);
    }

    /// <summary>
    /// Saves or updates the global AI integration configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="configuration">Configuration model with updated values.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task SaveConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default)
    {
        AiConfigurationEntity? entity = await _dbContext.AiConfigurations
            .FirstOrDefaultAsync(cancellationToken);

        if (entity != null)
        {
            UpdateEntityValues(entity, configuration);
        }
        else
        {
            await InsertNewConfigurationAsync(configuration, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the custom project description configured for a specific repository path.
    /// Usages: OpenRouterAiCommitMessageService, CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Project description text if configured; otherwise null.</returns>
    public async Task<string?> GetProjectDescriptionAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        RepositoryAiContextEntity? entity = await _dbContext.RepositoryAiContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.RepositoryPath == repositoryPath, cancellationToken);

        return entity?.ProjectDescription;
    }

    /// <summary>
    /// Saves or updates the project description for a specific repository path.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="description">Project description text.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public async Task SaveProjectDescriptionAsync(string repositoryPath, string? description, CancellationToken cancellationToken = default)
    {
        RepositoryAiContextEntity? entity = await _dbContext.RepositoryAiContexts
            .FirstOrDefaultAsync(c => c.RepositoryPath == repositoryPath, cancellationToken);

        if (entity != null)
        {
            entity.ProjectDescription = description;
        }
        else
        {
            await InsertNewContextAsync(repositoryPath, description, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AiConfiguration MapToDomain(AiConfigurationEntity entity)
    {
        return new AiConfiguration
        {
            ApiKey = entity.ApiKey,
            Model = entity.Model,
            CommitStyle = entity.CommitStyle,
            TokenLimit = entity.TokenLimit,
            BaseUrl = entity.BaseUrl
        };
    }

    private static void UpdateEntityValues(AiConfigurationEntity entity, AiConfiguration configuration)
    {
        entity.ApiKey = configuration.ApiKey;
        entity.Model = configuration.Model;
        entity.CommitStyle = configuration.CommitStyle;
        entity.TokenLimit = configuration.TokenLimit;
        entity.BaseUrl = configuration.BaseUrl;
    }

    private async Task InsertNewConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken)
    {
        AiConfigurationEntity newEntity = new()
        {
            ApiKey = configuration.ApiKey,
            Model = configuration.Model,
            CommitStyle = configuration.CommitStyle,
            TokenLimit = configuration.TokenLimit,
            BaseUrl = configuration.BaseUrl
        };

        await _dbContext.AiConfigurations.AddAsync(newEntity, cancellationToken);
    }

    private async Task InsertNewContextAsync(string repositoryPath, string? description, CancellationToken cancellationToken)
    {
        RepositoryAiContextEntity newEntity = new()
        {
            RepositoryPath = repositoryPath,
            ProjectDescription = description
        };

        await _dbContext.RepositoryAiContexts.AddAsync(newEntity, cancellationToken);
    }
}
