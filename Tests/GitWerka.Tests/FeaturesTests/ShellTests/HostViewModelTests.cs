namespace GitWerka.Tests.FeaturesTests.ShellTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.Shell.UI.Screens.Host;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Infrastructure.Services;
using ReactiveUI;
using Xunit;

public class HostViewModelTests
{
    [Fact]
    public void InitialState_HasReadyStatus_AndNoLoadedRepository()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        Assert.Equal("Ready", vm.State.StatusMessage);
        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Empty(vm.State.CurrentRepositoryName);
    }

    [Fact]
    public void SetActiveRepository_UpdatesStateCorrectly()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        vm.SetActiveRepository("RepoTest", "/path/to/repo");

        Assert.True(vm.State.IsRepositoryLoaded);
        Assert.Equal("RepoTest", vm.State.CurrentRepositoryName);
        Assert.Equal("/path/to/repo", vm.State.CurrentRepositoryPath);
    }

    [Fact]
    public void OpenSettingsCommand_WhenCommitLogScreenActive_OpensSettingsModal()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.False(commitLogVm.State.IsSettingsModalVisible);

        hostVm.OpenSettingsCommand.Execute().Subscribe();

        Assert.True(commitLogVm.State.IsSettingsModalVisible);
    }

    [Fact]
    public void RouterCurrentViewModel_WhenCommitLogNavigated_UpdatesActiveRepositoryInHost()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.True(hostVm.State.IsRepositoryLoaded);
        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);
        Assert.Equal("/tmp/sample-repo", hostVm.State.CurrentRepositoryPath);
    }

    private class TestProfileRepository : IProfileRepository
    {
        public List<GitProfile> Profiles { get; } = new();
        public Dictionary<string, int> Mappings { get; } = new();

        public Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitProfile>>(Profiles.ToList());

        public Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profiles.FirstOrDefault(p => p.Id == id));

        public Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
        {
            Profiles.RemoveAll(p => p.Id == profile.Id);
            Profiles.Add(profile);
            return Task.FromResult(profile);
        }

        public Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
        {
            Profiles.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitAuthor>>(new List<GitAuthor>());

        public Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<GitAuthor?>(null);

        public Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default) =>
            Task.FromResult(author);

        public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitProfileCredentials>>(new List<GitProfileCredentials>());

        public Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<GitProfileCredentials?>(null);

        public Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default) =>
            Task.FromResult(credentials);

        public Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            if (Mappings.TryGetValue(repositoryPath, out int profileId))
            {
                return Task.FromResult(Profiles.FirstOrDefault(p => p.Id == profileId));
            }
            return Task.FromResult<GitProfile?>(null);
        }

        public Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default)
        {
            Mappings[repositoryPath] = profileId;
            return Task.CompletedTask;
        }

        public Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            Mappings.Remove(repositoryPath);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RouterCurrentViewModel_WhenCommitLogNavigated_SynchronizesProfilesInHost()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@test.com" };
        GitProfile profile = new() { Id = 10, Name = "DevProfile", Author = author };
        profileRepo.Profiles.Add(profile);
        profileRepo.Mappings["/tmp/sample-repo"] = 10;

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        await commitLogVm.SelectProfileAsync(profile);

        Assert.NotNull(hostVm.State.ActiveProfile);
        Assert.Equal("DevProfile", hostVm.State.ActiveProfile.Name);
        Assert.Single(hostVm.State.AvailableProfiles);
    }

    [Fact]
    public async Task SelectProfileCommand_InHost_UpdatesActiveProfileAndPropagatesToCommitLog()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        GitAuthor author = new() { Id = 2, Name = "Bob", Email = "bob@test.com" };
        GitProfile profile = new() { Id = 20, Name = "BobProfile", Author = author };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        await hostVm.SelectProfileCommand.Execute(profile).FirstAsync();

        Assert.NotNull(hostVm.State.ActiveProfile);
        Assert.Equal("BobProfile", hostVm.State.ActiveProfile.Name);
        Assert.NotNull(commitLogVm.State.ActiveProfile);
        Assert.Equal("BobProfile", commitLogVm.State.ActiveProfile.Name);
        Assert.Equal(20, profileRepo.Mappings["/tmp/sample-repo"]);
    }

    [Fact]
    public void NavigateToProfilesCommand_InHost_NavigatesToProfileManagementScreen()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        hostVm.NavigateToProfilesCommand.Execute().Subscribe();

        Assert.IsType<ProfileManagementViewModel>(screen.Router.NavigationStack.Last());
    }

    [Fact]
    public async Task RefreshProfilesCommand_WhenNewProfileAdded_UpdatesAvailableProfilesInHostAndCommitLog()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.Empty(hostVm.State.AvailableProfiles);

        GitAuthor author = new() { Id = 5, Name = "Charlie", Email = "charlie@test.com" };
        GitProfile newProfile = new() { Id = 50, Name = "CharlieProfile", Author = author };
        profileRepo.Profiles.Add(newProfile);

        await hostVm.RefreshProfilesCommand.Execute().FirstAsync();

        Assert.Single(hostVm.State.AvailableProfiles);
        Assert.Equal("CharlieProfile", hostVm.State.AvailableProfiles[0].Name);
        Assert.Single(commitLogVm.State.AvailableProfiles);
    }
}
