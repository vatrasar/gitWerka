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

public class ProfileRepositoryTests : IDisposable
{
    private readonly GitWerkaDbContext _dbContext;
    private readonly ProfileRepository _repository;
    private readonly string _databaseFile;

    public ProfileRepositoryTests()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"GitWerka_ProfileTestDb_{Guid.NewGuid():N}.db");
        DbContextOptions<GitWerkaDbContext> options = new DbContextOptionsBuilder<GitWerkaDbContext>()
            .UseSqlite($"Data Source={_databaseFile}")
            .Options;

        _dbContext = new GitWerkaDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new ProfileRepository(_dbContext);
    }

    [Fact]
    public async Task SaveAuthorAsync_AddsAndRetrievesAuthors()
    {
        GitAuthor author = new()
        {
            Name = "Alice Developer",
            Email = "alice@example.com"
        };

        GitAuthor saved = await _repository.SaveAuthorAsync(author);
        Assert.True(saved.Id > 0);

        IReadOnlyList<GitAuthor> authors = await _repository.GetAllAuthorsAsync();
        Assert.Single(authors);
        Assert.Equal("Alice Developer", authors[0].Name);
        Assert.Equal("alice@example.com", authors[0].Email);
    }

    [Fact]
    public async Task SaveCredentialsAsync_AddsAndRetrievesCredentials()
    {
        GitProfileCredentials creds = new()
        {
            Username = "octocat",
            AccessToken = "ghp_secrettoken123"
        };

        GitProfileCredentials saved = await _repository.SaveCredentialsAsync(creds);
        Assert.True(saved.Id > 0);

        IReadOnlyList<GitProfileCredentials> list = await _repository.GetAllCredentialsAsync();
        Assert.Single(list);
        Assert.Equal("octocat", list[0].Username);
        Assert.Equal("ghp_secrettoken123", list[0].AccessToken);
    }

    [Fact]
    public async Task SaveProfileAsync_PairsAuthorAndCredentials()
    {
        GitAuthor author = await _repository.SaveAuthorAsync(new GitAuthor
        {
            Name = "Bob Smith",
            Email = "bob@example.com"
        });

        GitProfileCredentials creds = await _repository.SaveCredentialsAsync(new GitProfileCredentials
        {
            Username = "bobgithub",
            AccessToken = "ghp_bobtoken456"
        });

        GitProfile profile = new()
        {
            Name = "Bob Work",
            AuthorId = author.Id,
            CredentialsId = creds.Id
        };

        GitProfile saved = await _repository.SaveProfileAsync(profile);
        Assert.True(saved.Id > 0);
        Assert.NotNull(saved.Author);
        Assert.Equal("Bob Smith", saved.Author.Name);
        Assert.NotNull(saved.Credentials);
        Assert.Equal("bobgithub", saved.Credentials.Username);

        IReadOnlyList<GitProfile> all = await _repository.GetAllProfilesAsync();
        Assert.Single(all);
        Assert.Equal("Bob Work", all[0].Name);
    }

    [Fact]
    public async Task SetActiveProfileForRepositoryAsync_PersistsAndRetrievesMapping()
    {
        GitAuthor author = await _repository.SaveAuthorAsync(new GitAuthor { Name = "Dev", Email = "dev@example.com" });
        GitProfileCredentials creds = await _repository.SaveCredentialsAsync(new GitProfileCredentials { Username = "dev", AccessToken = "tok" });
        GitProfile profile = await _repository.SaveProfileAsync(new GitProfile { Name = "Main", AuthorId = author.Id, CredentialsId = creds.Id });

        string repoPath = "/path/to/my/repo";
        await _repository.SetActiveProfileForRepositoryAsync(repoPath, profile.Id);

        GitProfile? active = await _repository.GetActiveProfileForRepositoryAsync(repoPath);
        Assert.NotNull(active);
        Assert.Equal(profile.Id, active.Id);
        Assert.Equal("Main", active.Name);

        await _repository.ClearActiveProfileForRepositoryAsync(repoPath);
        GitProfile? cleared = await _repository.GetActiveProfileForRepositoryAsync(repoPath);
        Assert.Null(cleared);
    }

    [Fact]
    public async Task DeleteAuthorAsync_ThrowsWhenUsedByProfile()
    {
        GitAuthor author = await _repository.SaveAuthorAsync(new GitAuthor { Name = "InUse", Email = "inuse@example.com" });
        GitProfileCredentials creds = await _repository.SaveCredentialsAsync(new GitProfileCredentials { Username = "u", AccessToken = "t" });
        await _repository.SaveProfileAsync(new GitProfile { Name = "ProfileUsingAuthor", AuthorId = author.Id, CredentialsId = creds.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.DeleteAuthorAsync(author.Id));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        if (File.Exists(_databaseFile))
        {
            File.Delete(_databaseFile);
        }
    }
}
