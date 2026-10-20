namespace GitWerka.Tests.CoreTests.RepositoriesTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class RecentRepositoriesRepositoryTests : IDisposable
{
    private readonly GitWerkaDbContext _dbContext;
    private readonly RecentRepositoriesRepository _repository;
    private readonly string _databaseFile;

    public RecentRepositoriesRepositoryTests()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"GitWerka_TestDb_{Guid.NewGuid():N}.db");
        DbContextOptions<GitWerkaDbContext> options = new DbContextOptionsBuilder<GitWerkaDbContext>()
            .UseSqlite($"Data Source={_databaseFile}")
            .Options;

        _dbContext = new GitWerkaDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new RecentRepositoriesRepository(_dbContext);
    }

    [Fact]
    public async Task AddOrUpdateRecentRepositoryAsync_AddsAndRetrievesCorrectly()
    {
        RecentRepository model = new()
        {
            Name = "repo1",
            Path = "/dummy/repo1",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };

        await _repository.AddOrUpdateRecentRepositoryAsync(model);
        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();

        Assert.Single(repos);
        Assert.Equal("repo1", repos[0].Name);
        Assert.Equal("/dummy/repo1", repos[0].Path);
    }

    [Fact]
    public async Task TogglePinRecentRepositoryAsync_TogglesPinState()
    {
        RecentRepository model = new()
        {
            Name = "repo2",
            Path = "/dummy/repo2",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };

        await _repository.AddOrUpdateRecentRepositoryAsync(model);
        await _repository.TogglePinRecentRepositoryAsync("/dummy/repo2");

        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();

        Assert.Single(repos);
        Assert.True(repos[0].IsPinned);
    }

    [Fact]
    public async Task RemoveRecentRepositoryAsync_RemovesItem()
    {
        RecentRepository model = new()
        {
            Name = "repo3",
            Path = "/dummy/repo3",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };

        await _repository.AddOrUpdateRecentRepositoryAsync(model);
        await _repository.RemoveRecentRepositoryAsync("/dummy/repo3");

        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();

        Assert.Empty(repos);
    }

    [Fact]
    public async Task SetFavoriteAsync_AddsNewEntityAsFavoriteWithCustomName()
    {
        await _repository.SetFavoriteAsync("/dummy/fav1", true, "Custom Fav 1");

        RecentRepository? repo = await _repository.GetRepositoryByPathAsync("/dummy/fav1");

        Assert.NotNull(repo);
        Assert.True(repo.IsFavorite);
        Assert.Equal("Custom Fav 1", repo.CustomName);
        Assert.Equal("Custom Fav 1", repo.DisplayName);
    }

    [Fact]
    public async Task SetFavoriteAsync_UpdatesExistingEntityToFavorite()
    {
        RecentRepository model = new()
        {
            Name = "existingRepo",
            Path = "/dummy/existingRepo",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };
        await _repository.AddOrUpdateRecentRepositoryAsync(model);

        await _repository.SetFavoriteAsync("/dummy/existingRepo", true, "Renamed Fav");
        RecentRepository? updated = await _repository.GetRepositoryByPathAsync("/dummy/existingRepo");

        Assert.NotNull(updated);
        Assert.True(updated.IsFavorite);
        Assert.Equal("Renamed Fav", updated.CustomName);
    }

    [Fact]
    public async Task SetFavoriteAsync_RemovesFavoriteStatus()
    {
        await _repository.SetFavoriteAsync("/dummy/fav2", true, "Custom Fav 2");
        await _repository.SetFavoriteAsync("/dummy/fav2", false, null);

        RecentRepository? repo = await _repository.GetRepositoryByPathAsync("/dummy/fav2");

        Assert.NotNull(repo);
        Assert.False(repo.IsFavorite);
        Assert.Null(repo.CustomName);
        Assert.Equal("fav2", repo.DisplayName);
    }

    [Fact]
    public async Task GetFavoriteRepositoriesAsync_ReturnsOnlyFavoritesSortedByLastOpened()
    {
        RecentRepository r1 = new()
        {
            Name = "r1",
            Path = "/dummy/r1",
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsFavorite = true,
            CustomName = "Old Favorite"
        };
        RecentRepository r2 = new()
        {
            Name = "r2",
            Path = "/dummy/r2",
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsFavorite = false
        };
        RecentRepository r3 = new()
        {
            Name = "r3",
            Path = "/dummy/r3",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsFavorite = true,
            CustomName = "New Favorite"
        };

        await _repository.AddOrUpdateRecentRepositoryAsync(r1);
        await _repository.AddOrUpdateRecentRepositoryAsync(r2);
        await _repository.AddOrUpdateRecentRepositoryAsync(r3);

        IReadOnlyList<RecentRepository> favorites = await _repository.GetFavoriteRepositoriesAsync();

        Assert.Equal(2, favorites.Count);
        Assert.Equal("/dummy/r3", favorites[0].Path);
        Assert.Equal("/dummy/r1", favorites[1].Path);
    }

    [Fact]
    public async Task GetMostRecentNonFavoriteRepositoryAsync_ReturnsLatestNonFavorite()
    {
        RecentRepository fav = new()
        {
            Name = "fav",
            Path = "/dummy/fav",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsFavorite = true
        };
        RecentRepository nonFavOld = new()
        {
            Name = "nonFavOld",
            Path = "/dummy/nonFavOld",
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            IsFavorite = false
        };
        RecentRepository nonFavNew = new()
        {
            Name = "nonFavNew",
            Path = "/dummy/nonFavNew",
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsFavorite = false
        };

        await _repository.AddOrUpdateRecentRepositoryAsync(fav);
        await _repository.AddOrUpdateRecentRepositoryAsync(nonFavOld);
        await _repository.AddOrUpdateRecentRepositoryAsync(nonFavNew);

        RecentRepository? result = await _repository.GetMostRecentNonFavoriteRepositoryAsync();

        Assert.NotNull(result);
        Assert.Equal("/dummy/nonFavNew", result.Path);
        Assert.False(result.IsFavorite);
    }

    [Fact]
    public async Task SetFavoriteAsync_WithGroupNameProvided_PersistsGroupAndCustomName()
    {
        await _repository.SetFavoriteAsync("/dummy/grouped-repo", true, "Repo Agent 1", "Project Alpha");

        RecentRepository? repo = await _repository.GetRepositoryByPathAsync("/dummy/grouped-repo");

        Assert.NotNull(repo);
        Assert.True(repo.IsFavorite);
        Assert.Equal("Repo Agent 1", repo.CustomName);
        Assert.Equal("Project Alpha", repo.GroupName);
    }

    [Fact]
    public async Task SetFavoriteAsync_WhenFavoriteIsFalse_ClearsGroupAndCustomName()
    {
        await _repository.SetFavoriteAsync("/dummy/grouped-repo-2", true, "Repo Agent 2", "Project Beta");
        await _repository.SetFavoriteAsync("/dummy/grouped-repo-2", false);

        RecentRepository? repo = await _repository.GetRepositoryByPathAsync("/dummy/grouped-repo-2");

        Assert.NotNull(repo);
        Assert.False(repo.IsFavorite);
        Assert.Null(repo.CustomName);
        Assert.Null(repo.GroupName);
    }

    [Fact]
    public async Task AddOrUpdateRecentRepositoryAsync_WithTrailingSlash_NormalizesAndDeduplicates()
    {
        RecentRepository initial = new()
        {
            Name = "repo1",
            Path = "/dummy/repo1",
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsPinned = true
        };
        await _repository.AddOrUpdateRecentRepositoryAsync(initial);

        RecentRepository updated = new()
        {
            Name = "repo1",
            Path = "/dummy/repo1/",
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };
        await _repository.AddOrUpdateRecentRepositoryAsync(updated);

        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();

        Assert.Single(repos);
        Assert.Equal("/dummy/repo1", repos[0].Path);
        Assert.True(repos[0].IsPinned);
    }

    [Fact]
    public async Task GetRecentRepositoriesAsync_DeduplicatesExistingDatabaseEntries()
    {
        _dbContext.RecentRepositories.AddRange(
            new GitWerka.Infrastructure.Data.Entities.RecentRepositoryEntity
            {
                Name = "repoX",
                Path = "/dummy/repoX",
                LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                IsPinned = false
            },
            new GitWerka.Infrastructure.Data.Entities.RecentRepositoryEntity
            {
                Name = "repoX",
                Path = "/dummy/repoX/",
                LastOpenedAtUtc = DateTime.UtcNow,
                IsPinned = true
            });
        await _dbContext.SaveChangesAsync();

        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();

        Assert.Single(repos);
        Assert.Equal("/dummy/repoX", repos[0].Path);
    }

    [Fact]
    public async Task RemoveRecentRepositoryAsync_RemovesBothTrailingSlashAndNormalizedEntries()
    {
        _dbContext.RecentRepositories.AddRange(
            new GitWerka.Infrastructure.Data.Entities.RecentRepositoryEntity
            {
                Name = "repoY",
                Path = "/dummy/repoY",
                LastOpenedAtUtc = DateTime.UtcNow
            },
            new GitWerka.Infrastructure.Data.Entities.RecentRepositoryEntity
            {
                Name = "repoY",
                Path = "/dummy/repoY/",
                LastOpenedAtUtc = DateTime.UtcNow
            });
        await _dbContext.SaveChangesAsync();

        await _repository.RemoveRecentRepositoryAsync("/dummy/repoY/");

        IReadOnlyList<RecentRepository> repos = await _repository.GetRecentRepositoriesAsync();
        Assert.Empty(repos);
    }

    public void Dispose()
    {
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
