namespace GitWerka.Tests.CoreTests.RepositoriesTests;

using System;
using System.IO;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ApplicationSettingsRepositoryTests : IDisposable
{
    private readonly GitWerkaDbContext _dbContext;
    private readonly ApplicationSettingsRepository _repository;
    private readonly string _databaseFile;

    public ApplicationSettingsRepositoryTests()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"GitWerka_SettingsTestDb_{Guid.NewGuid():N}.db");
        DbContextOptions<GitWerkaDbContext> options = new DbContextOptionsBuilder<GitWerkaDbContext>()
            .UseSqlite($"Data Source={_databaseFile}")
            .Options;

        _dbContext = new GitWerkaDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new ApplicationSettingsRepository(_dbContext);
    }

    [Fact]
    public async Task GetSettingsAsync_WhenEmpty_ReturnsDefaultSettings()
    {
        ApplicationSettings settings = await _repository.GetSettingsAsync();

        Assert.NotNull(settings);
        Assert.Equal(string.Empty, settings.DefaultProjectsDirectory);
    }

    [Fact]
    public async Task SaveSettingsAsync_InsertsAndRetrievesSettings()
    {
        ApplicationSettings initialSettings = new()
        {
            DefaultProjectsDirectory = "/home/user/my-projects"
        };

        await _repository.SaveSettingsAsync(initialSettings);
        ApplicationSettings retrieved = await _repository.GetSettingsAsync();

        Assert.Equal("/home/user/my-projects", retrieved.DefaultProjectsDirectory);
    }

    [Fact]
    public async Task SaveSettingsAsync_WhenUpdated_OverwritesExistingSettings()
    {
        await _repository.SaveSettingsAsync(new ApplicationSettings { DefaultProjectsDirectory = "/initial/path" });
        await _repository.SaveSettingsAsync(new ApplicationSettings { DefaultProjectsDirectory = "/updated/path" });

        ApplicationSettings retrieved = await _repository.GetSettingsAsync();

        Assert.Equal("/updated/path", retrieved.DefaultProjectsDirectory);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();

        if (File.Exists(_databaseFile))
        {
            try
            {
                File.Delete(_databaseFile);
            }
            catch
            {
            }
        }
    }
}
