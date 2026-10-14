namespace GitWerka.Tests.FeaturesTests.ProfileManagementTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Features.ProfileManagement.Domain.Enums;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using ReactiveUI;
using Xunit;

public class ProfileManagementViewModelTests
{
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockProfileRepository : IProfileRepository
    {
        public List<GitAuthor> Authors { get; } = new();
        public List<GitProfileCredentials> CredentialsList { get; } = new();
        public List<GitProfile> Profiles { get; } = new();
        public Dictionary<string, int> RepositoryMappings { get; } = new();

        private int _authorIdCounter = 1;
        private int _credentialsIdCounter = 1;
        private int _profileIdCounter = 1;

        public Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
        {
            var populated = Profiles.Select(p => p with
            {
                Author = Authors.FirstOrDefault(a => a.Id == p.AuthorId),
                Credentials = CredentialsList.FirstOrDefault(c => c.Id == p.CredentialsId)
            }).ToList();
            return Task.FromResult<IReadOnlyList<GitProfile>>(populated);
        }

        public Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var p = Profiles.FirstOrDefault(x => x.Id == id);
            if (p == null) return Task.FromResult<GitProfile?>(null);
            return Task.FromResult<GitProfile?>(p with
            {
                Author = Authors.FirstOrDefault(a => a.Id == p.AuthorId),
                Credentials = CredentialsList.FirstOrDefault(c => c.Id == p.CredentialsId)
            });
        }

        public Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
        {
            if (profile.Id == 0)
            {
                GitProfile newProfile = profile with { Id = _profileIdCounter++ };
                Profiles.Add(newProfile);
                return Task.FromResult(newProfile with
                {
                    Author = Authors.FirstOrDefault(a => a.Id == newProfile.AuthorId),
                    Credentials = CredentialsList.FirstOrDefault(c => c.Id == newProfile.CredentialsId)
                });
            }

            int index = Profiles.FindIndex(p => p.Id == profile.Id);
            Profiles[index] = profile;
            return Task.FromResult(profile with
            {
                Author = Authors.FirstOrDefault(a => a.Id == profile.AuthorId),
                Credentials = CredentialsList.FirstOrDefault(c => c.Id == profile.CredentialsId)
            });
        }

        public Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
        {
            Profiles.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GitAuthor>>(Authors.ToList());
        }

        public Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Authors.FirstOrDefault(a => a.Id == id));
        }

        public Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default)
        {
            if (author.Id == 0)
            {
                GitAuthor newAuthor = author with { Id = _authorIdCounter++ };
                Authors.Add(newAuthor);
                return Task.FromResult(newAuthor);
            }

            int index = Authors.FindIndex(a => a.Id == author.Id);
            Authors[index] = author;
            return Task.FromResult(author);
        }

        public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default)
        {
            Authors.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GitProfileCredentials>>(CredentialsList.ToList());
        }

        public Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CredentialsList.FirstOrDefault(c => c.Id == id));
        }

        public Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default)
        {
            if (credentials.Id == 0)
            {
                GitProfileCredentials newCreds = credentials with { Id = _credentialsIdCounter++ };
                CredentialsList.Add(newCreds);
                return Task.FromResult(newCreds);
            }

            int index = CredentialsList.FindIndex(c => c.Id == credentials.Id);
            CredentialsList[index] = credentials;
            return Task.FromResult(credentials);
        }

        public Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default)
        {
            CredentialsList.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            if (RepositoryMappings.TryGetValue(repositoryPath, out int profileId))
            {
                return GetProfileByIdAsync(profileId, cancellationToken);
            }
            return Task.FromResult<GitProfile?>(null);
        }

        public Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default)
        {
            RepositoryMappings[repositoryPath] = profileId;
            return Task.CompletedTask;
        }

        public Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            RepositoryMappings.Remove(repositoryPath);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void SetActiveTab_SwitchesActiveTab()
    {
        MockScreen screen = new();
        MockProfileRepository repo = new();
        ProfileManagementViewModel vm = new(screen, repo);

        Assert.Equal(ProfileManagementTab.Profiles, vm.State.ActiveTab);

        vm.SetActiveTab(ProfileManagementTab.Authors);
        Assert.Equal(ProfileManagementTab.Authors, vm.State.ActiveTab);

        vm.SetActiveTab(ProfileManagementTab.Credentials);
        Assert.Equal(ProfileManagementTab.Credentials, vm.State.ActiveTab);
    }

    [Fact]
    public async Task SaveAuthor_AddsNewAuthorAndUpdatesState()
    {
        MockScreen screen = new();
        MockProfileRepository repo = new();
        ProfileManagementViewModel vm = new(screen, repo);

        vm.StartCreateAuthorCommand.Execute().Subscribe();
        Assert.True(vm.State.IsEditingAuthor);

        vm.UpdateAuthorNameInput("Alice");
        vm.UpdateAuthorEmailInput("alice@work.com");

        await vm.SaveAuthorCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsEditingAuthor);
        Assert.Single(vm.State.Authors);
        Assert.Equal("Alice", vm.State.Authors[0].Name);
        Assert.Equal("alice@work.com", vm.State.Authors[0].Email);
    }

    [Fact]
    public async Task SaveCredentials_AddsNewCredentialsAndUpdatesState()
    {
        MockScreen screen = new();
        MockProfileRepository repo = new();
        ProfileManagementViewModel vm = new(screen, repo);

        vm.StartCreateCredentialsCommand.Execute().Subscribe();
        Assert.True(vm.State.IsEditingCredentials);

        vm.UpdateCredentialsUsernameInput("alice_gh");
        vm.UpdateCredentialsAccessTokenInput("ghp_token_xyz");

        await vm.SaveCredentialsCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsEditingCredentials);
        Assert.Single(vm.State.CredentialsList);
        Assert.Equal("alice_gh", vm.State.CredentialsList[0].Username);
        Assert.Equal("ghp_token_xyz", vm.State.CredentialsList[0].AccessToken);
    }

    [Fact]
    public async Task SaveProfile_CreatesProfileWithSelectedAuthorAndCredentials()
    {
        MockScreen screen = new();
        MockProfileRepository repo = new();

        repo.Authors.Add(new GitAuthor { Id = 1, Name = "Alice", Email = "alice@work.com" });
        repo.CredentialsList.Add(new GitProfileCredentials { Id = 1, Username = "alice_gh", AccessToken = "ghp_tok" });

        ProfileManagementViewModel vm = new(screen, repo, "/my/repo");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.StartCreateProfileCommand.Execute().Subscribe();
        Assert.True(vm.State.IsEditingProfile);

        vm.UpdateProfileNameInput("Work Profile");
        vm.SelectAuthorForProfile(vm.State.Authors[0]);
        vm.SelectCredentialsForProfile(vm.State.CredentialsList[0]);

        await vm.SaveProfileCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsEditingProfile);
        Assert.Single(vm.State.Profiles);
        Assert.Equal("Work Profile", vm.State.Profiles[0].Name);
        Assert.NotNull(vm.State.Profiles[0].Author);
        Assert.Equal("Alice", vm.State.Profiles[0].Author?.Name);
        Assert.NotNull(vm.State.Profiles[0].Credentials);
        Assert.Equal("alice_gh", vm.State.Profiles[0].Credentials?.Username);
    }

    [Fact]
    public async Task SetAsActiveProfile_SetsActiveProfileForCurrentRepository()
    {
        MockScreen screen = new();
        MockProfileRepository repo = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@work.com" };
        GitProfileCredentials creds = new() { Id = 1, Username = "alice_gh", AccessToken = "ghp_tok" };
        GitProfile profile = new() { Id = 10, Name = "Dev", AuthorId = 1, CredentialsId = 1, Author = author, Credentials = creds };

        repo.Authors.Add(author);
        repo.CredentialsList.Add(creds);
        repo.Profiles.Add(profile);

        ProfileManagementViewModel vm = new(screen, repo, "/current/repo/path");
        await vm.LoadDataCommand.Execute().FirstAsync();

        await vm.SetAsActiveProfileCommand.Execute(profile).FirstAsync();

        Assert.NotNull(vm.State.ActiveRepositoryProfile);
        Assert.Equal(10, vm.State.ActiveRepositoryProfile.Id);
    }
}
