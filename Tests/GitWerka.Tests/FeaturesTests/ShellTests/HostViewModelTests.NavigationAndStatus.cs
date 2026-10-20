namespace GitWerka.Tests.FeaturesTests.ShellTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Features.Shell.UI.Screens.Host;
using GitWerka.Infrastructure.Services;
using ReactiveUI;
using Xunit;

public class HostViewModelNavigationAndStatusTests
{
    [Fact]
    public async Task RouterCurrentViewModel_WhenNavigatedBackFromProfileManagement_ReloadsProfilesAutomatically()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.Empty(hostVm.State.AvailableProfiles);

        ProfileManagementViewModel profileMgmtVm = new(screen, profileRepo, "/tmp/sample-repo");
        screen.Router.Navigate.Execute(profileMgmtVm).Subscribe();

        GitAuthor author = new() { Id = 6, Name = "Dave", Email = "dave@test.com" };
        GitProfile newProfile = new() { Id = 60, Name = "DaveProfile", Author = author };
        profileRepo.Profiles.Add(newProfile);

        screen.Router.NavigateBack.Execute().Subscribe();

        await commitLogVm.ReloadProfilesAsync();

        Assert.Single(hostVm.State.AvailableProfiles);
        Assert.Equal("DaveProfile", hostVm.State.AvailableProfiles[0].Name);
    }

    private class TestRecentRepositoriesRepository : IRecentRepositoriesRepository
    {
        public List<RecentRepository> Items { get; } = new();

        public Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentRepository>>(Items);
        }

        public Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Path == repository.Path);
            Items.Add(repository);
            return Task.CompletedTask;
        }

        public Task RemoveRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Path == repositoryPath);
            return Task.CompletedTask;
        }

        public Task TogglePinRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<RecentRepository?> GetRepositoryByPathAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.Path == repositoryPath));
        }

        public Task SetFavoriteAsync(string repositoryPath, bool isFavorite, string? customName = null, string? groupName = null, CancellationToken cancellationToken = default)
        {
            var existing = Items.FirstOrDefault(x => x.Path == repositoryPath);
            if (existing != null)
            {
                Items.Remove(existing);
                Items.Add(existing with { IsFavorite = isFavorite, CustomName = isFavorite ? customName : null, GroupName = isFavorite ? groupName : null });
            }
            else
            {
                Items.Add(new RecentRepository
                {
                    Path = repositoryPath,
                    Name = new System.IO.DirectoryInfo(repositoryPath).Name,
                    IsFavorite = isFavorite,
                    CustomName = isFavorite ? customName : null,
                    GroupName = isFavorite ? groupName : null,
                    LastOpenedAtUtc = DateTime.UtcNow
                });
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RecentRepository> favorites = Items
                .Where(x => x.IsFavorite)
                .OrderByDescending(x => x.LastOpenedAtUtc)
                .ToList();
            return Task.FromResult(favorites);
        }

        public Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default)
        {
            RecentRepository? mostRecent = Items
                .Where(x => !x.IsFavorite)
                .OrderByDescending(x => x.LastOpenedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(mostRecent);
        }
    }

    [Fact]
    public async Task CommitLogRepositoryName_WhenFavoritedOrUnfavorited_UpdatesHostCurrentRepositoryNameReactively()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();
        recentRepo.Items.Add(new RecentRepository
        {
            Path = "/tmp/sample-repo",
            Name = "sample-repo",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo, recentRepositoriesRepository: recentRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();
        await commitLogVm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);

        commitLogVm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        commitLogVm.UpdateNewFavoriteNameInput("Custom Work Repo");
        await commitLogVm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.Equal("Custom Work Repo", commitLogVm.State.RepositoryName);
        Assert.Equal("Custom Work Repo", hostVm.State.CurrentRepositoryName);

        await commitLogVm.RemoveFromFavoritesCommand.Execute("/tmp/sample-repo").FirstAsync();

        Assert.Equal("sample-repo", commitLogVm.State.RepositoryName);
        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);
    }

    [Fact]
    public void HostViewModel_WhenOnWelcomeScreen_SettingsButtonIsVisibleAndTriggersSettingsModal()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();

        RepositoryManagerViewModel repoManagerVm = new(screen, recentRepo, gitService);
        screen.Router.Navigate.Execute(repoManagerVm).Subscribe();

        Assert.True(hostVm.State.IsSettingsButtonVisible);
        Assert.False(repoManagerVm.State.IsSettingsModalVisible);

        hostVm.OpenSettingsCommand.Execute().Subscribe();
        Assert.True(repoManagerVm.State.IsSettingsModalVisible);
    }

    [Fact]
    public void InitialState_FooterInfoText_DisplaysAppVersion()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Equal(vm.State.AppVersion, vm.State.FooterInfoText);
        Assert.StartsWith("v", vm.State.FooterInfoText);
    }

    [Fact]
    public void SetActiveRepository_UpdatesFooterInfoText_ToRepositoryPath()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        vm.SetActiveRepository("RepoTest", "/path/to/my-repo");

        Assert.True(vm.State.IsRepositoryLoaded);
        Assert.Equal("/path/to/my-repo", vm.State.FooterInfoText);

        vm.SetActiveRepository(string.Empty, string.Empty);

        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Equal(vm.State.AppVersion, vm.State.FooterInfoText);
    }

    [Fact]
    public void RouterCurrentViewModel_WhenCommitLogNavigated_UpdatesFooterInfoTextToRepositoryPath_AndRestoresVersionOnBack()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        RepositoryManagerViewModel repoManagerVm = new(screen, new TestRecentRepositoriesRepository(), gitService);
        screen.Router.Navigate.Execute(repoManagerVm).Subscribe();

        Assert.Equal(hostVm.State.AppVersion, hostVm.State.FooterInfoText);

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.True(hostVm.State.IsRepositoryLoaded);
        Assert.Equal("/tmp/sample-repo", hostVm.State.FooterInfoText);

        screen.Router.NavigateBack.Execute().Subscribe();

        Assert.False(hostVm.State.IsRepositoryLoaded);
        Assert.Equal(hostVm.State.AppVersion, hostVm.State.FooterInfoText);
    }

    [Fact]
    public void NavigateBackCommand_WhenMultipleScreensOnStack_NavigatesDirectlyToWelcomeScreen()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();

        RepositoryManagerViewModel welcomeVm = new(screen, recentRepo, gitService);
        screen.Router.Navigate.Execute(welcomeVm).Subscribe();

        CommitLogViewModel commitLogVm1 = new(screen, "/tmp/sample-repo-1", gitService);
        screen.Router.Navigate.Execute(commitLogVm1).Subscribe();

        CommitLogViewModel commitLogVm2 = new(screen, "/tmp/sample-repo-2", gitService);
        screen.Router.Navigate.Execute(commitLogVm2).Subscribe();

        Assert.Equal(3, screen.Router.NavigationStack.Count);
        Assert.True(hostVm.State.IsRepositoryLoaded);
        Assert.Equal("/tmp/sample-repo-2", hostVm.State.FooterInfoText);

        hostVm.NavigateBackCommand.Execute().Subscribe();

        Assert.Single(screen.Router.NavigationStack);
        Assert.Same(welcomeVm, screen.Router.NavigationStack[0]);
        Assert.False(hostVm.State.IsRepositoryLoaded);
        Assert.Empty(hostVm.State.CurrentRepositoryName);
        Assert.Empty(hostVm.State.CurrentRepositoryPath);
        Assert.Equal(hostVm.State.AppVersion, hostVm.State.FooterInfoText);
    }

    [Fact]
    public void NavigateBackCommand_WhenSingleRepositoryOnStack_NavigatesDirectlyToWelcomeScreen()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();

        RepositoryManagerViewModel welcomeVm = new(screen, recentRepo, gitService);
        screen.Router.Navigate.Execute(welcomeVm).Subscribe();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.Equal(2, screen.Router.NavigationStack.Count);
        Assert.True(hostVm.State.IsRepositoryLoaded);

        hostVm.NavigateBackCommand.Execute().Subscribe();

        Assert.Single(screen.Router.NavigationStack);
        Assert.Same(welcomeVm, screen.Router.NavigationStack[0]);
        Assert.False(hostVm.State.IsRepositoryLoaded);
    }

    [Fact]
    public void HostViewModel_WhenStatusServicePublishesStatus_UpdatesHostStateAndFlags()
    {
        MockScreen screen = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        HostViewModel hostVm = new(screen, appStatusService: statusService);

        Assert.Equal("Ready", hostVm.State.StatusMessage);
        Assert.True(hostVm.State.IsStatusReady);
        Assert.False(hostVm.State.IsStatusInProgress);

        statusService.SetInProgress("Pushing changes...");

        Assert.Equal("Pushing changes...", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.InProgress, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusInProgress);
        Assert.False(hostVm.State.IsStatusReady);

        statusService.SetSuccess("Push completed successfully.");

        Assert.Equal("Push completed successfully.", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Success, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusSuccess);
        Assert.False(hostVm.State.IsStatusInProgress);

        statusService.SetError("Push failed: rejected");

        Assert.Equal("Push failed: rejected", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Error, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusError);

        statusService.SetReady();

        Assert.Equal("Ready", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Ready, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusReady);
    }

    [Fact]
    public void SetStatusMessage_WithAppStatusService_ForwardsToService()
    {
        MockScreen screen = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        HostViewModel hostVm = new(screen, appStatusService: statusService);

        hostVm.SetStatusMessage("Working...", AppStatusSeverity.InProgress);

        Assert.Equal("Working...", statusService.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.InProgress, statusService.CurrentStatus.Severity);
        Assert.True(hostVm.State.IsStatusInProgress);
    }
}
