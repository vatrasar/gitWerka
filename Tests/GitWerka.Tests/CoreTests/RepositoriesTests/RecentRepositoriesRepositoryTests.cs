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
