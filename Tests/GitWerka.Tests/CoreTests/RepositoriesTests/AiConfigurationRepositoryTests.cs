namespace GitWerka.Tests.CoreTests.RepositoriesTests;

using System;
using System.IO;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Infrastructure.Data;
using GitWerka.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class AiConfigurationRepositoryTests : IDisposable
{
    private readonly GitWerkaDbContext _dbContext;
    private readonly AiConfigurationRepository _repository;
    private readonly string _databaseFile;

    public AiConfigurationRepositoryTests()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"GitWerka_AiTestDb_{Guid.NewGuid():N}.db");
        DbContextOptions<GitWerkaDbContext> options = new DbContextOptionsBuilder<GitWerkaDbContext>()
            .UseSqlite($"Data Source={_databaseFile}")
            .Options;

        _dbContext = new GitWerkaDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new AiConfigurationRepository(_dbContext);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenEmpty_ReturnsDefaultConfiguration()
    {
        AiConfiguration config = await _repository.GetConfigurationAsync();

        Assert.NotNull(config);
        Assert.Null(config.ApiKey);
        Assert.Equal("google/gemini-2.5-flash", config.Model);
        Assert.Equal(4000, config.TokenLimit);
    }

    [Fact]
    public async Task SaveConfigurationAsync_InsertsAndUpdatesConfiguration()
    {
        AiConfiguration initialConfig = new()
        {
            ApiKey = "sk-test-123",
            Model = "anthropic/claude-3.5-sonnet",
            CommitStyle = "gitmoji",
            TokenLimit = 8000,
            BaseUrl = "https://custom.api/v1"
        };

        await _repository.SaveConfigurationAsync(initialConfig);
        AiConfiguration retrieved = await _repository.GetConfigurationAsync();

        Assert.Equal("sk-test-123", retrieved.ApiKey);
        Assert.Equal("anthropic/claude-3.5-sonnet", retrieved.Model);
        Assert.Equal("gitmoji", retrieved.CommitStyle);
        Assert.Equal(8000, retrieved.TokenLimit);
        Assert.Equal("https://custom.api/v1", retrieved.BaseUrl);

        AiConfiguration updatedConfig = retrieved with
        {
            ApiKey = "sk-updated-456",
            TokenLimit = 5000
        };

        await _repository.SaveConfigurationAsync(updatedConfig);
        AiConfiguration updatedRetrieved = await _repository.GetConfigurationAsync();

        Assert.Equal("sk-updated-456", updatedRetrieved.ApiKey);
        Assert.Equal(5000, updatedRetrieved.TokenLimit);
    }

    [Fact]
    public async Task SaveProjectDescriptionAsync_SavesAndRetrievesPerRepository()
    {
        string repo1 = "/path/to/repo1";
        string repo2 = "/path/to/repo2";

        await _repository.SaveProjectDescriptionAsync(repo1, "Git client desktop app in Avalonia");
        await _repository.SaveProjectDescriptionAsync(repo2, "E-commerce backend in ASP.NET Core");

        string? desc1 = await _repository.GetProjectDescriptionAsync(repo1);
        string? desc2 = await _repository.GetProjectDescriptionAsync(repo2);
        string? desc3 = await _repository.GetProjectDescriptionAsync("/path/to/repo3");

        Assert.Equal("Git client desktop app in Avalonia", desc1);
        Assert.Equal("E-commerce backend in ASP.NET Core", desc2);
        Assert.Null(desc3);

        await _repository.SaveProjectDescriptionAsync(repo1, "Updated Avalonia Git client");
        string? updatedDesc1 = await _repository.GetProjectDescriptionAsync(repo1);
        Assert.Equal("Updated Avalonia Git client", updatedDesc1);
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
