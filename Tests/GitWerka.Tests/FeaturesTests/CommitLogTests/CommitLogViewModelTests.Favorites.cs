namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using Xunit;

public class CommitLogFavoritesTests
{

    [Fact]
    public async Task ToggleFavoritesDockCommand_WhenExecuted_TogglesIsFavoritesDockOpenAndRefreshesData()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/fav",
            Name = "fav",
            IsFavorite = true,
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/recent",
            Name = "recent",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        CommitLogViewModel vm = new(screen, "/repo/fav", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsFavoritesDockOpen);

        await vm.ToggleFavoritesDockCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsFavoritesDockOpen);
        Assert.Single(vm.State.FavoriteRepositories);
        Assert.Equal("/repo/fav", vm.State.FavoriteRepositories[0].Path);
        Assert.NotNull(vm.State.RecentNonFavoriteRepository);
        Assert.Equal("/repo/recent", vm.State.RecentNonFavoriteRepository.Path);

        vm.CloseFavoritesDockCommand.Execute().Subscribe();

        Assert.False(vm.State.IsFavoritesDockOpen);
    }

    [Fact]
    public void ShowAddFavoriteModalCommand_And_HideAddFavoriteModalCommand_TogglesModalAndInitializesInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();

        Assert.True(vm.State.IsAddFavoriteModalVisible);
        Assert.Equal("/repo/my-project", vm.State.TargetFavoritePath);
        Assert.Equal("my-project", vm.State.NewFavoriteNameInput);

        vm.UpdateNewFavoriteNameInput("Custom Project Name");
        Assert.Equal("Custom Project Name", vm.State.NewFavoriteNameInput);

        vm.HideAddFavoriteModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsAddFavoriteModalVisible);
    }

    [Fact]
    public async Task ConfirmAddFavoriteCommand_WhenCurrentRepositoryFavorited_UpdatesRepositoryNameAndFavoriteList()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/my-project",
            Name = "my-project",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        vm.UpdateNewFavoriteNameInput("Renamed Project");

        await vm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsAddFavoriteModalVisible);
        Assert.Equal("Renamed Project", vm.State.RepositoryName);
        Assert.Single(vm.State.FavoriteRepositories);
        Assert.Equal("/repo/my-project", vm.State.FavoriteRepositories[0].Path);
        Assert.Equal("Renamed Project", vm.State.FavoriteRepositories[0].DisplayName);
    }

    [Fact]
    public async Task RefreshFavoritesDockData_WhenFavoritesContainGroups_BuildsFavoriteGroupsAndUngroupedLists()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent1",
            Name = "agent1",
            CustomName = "Agent 1",
            IsFavorite = true,
            GroupName = "Project Fleet",
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent2",
            Name = "agent2",
            CustomName = "Agent 2",
            IsFavorite = true,
            GroupName = "Project Fleet",
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/solo",
            Name = "solo",
            CustomName = "Solo Project",
            IsFavorite = true,
            GroupName = null,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("Project Fleet", vm.State.FavoriteGroups[0].Name);
        Assert.Equal(2, vm.State.FavoriteGroups[0].Repositories.Count);
        Assert.Single(vm.State.UngroupedFavoriteRepositories);
        Assert.Equal("Solo Project", vm.State.UngroupedFavoriteRepositories[0].DisplayName);
    }

    [Fact]
    public async Task ExistingGroupNames_WhenFavoritesContainDistinctGroups_ReturnsSortedUniqueGroupNames()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r1",
            Name = "r1",
            IsFavorite = true,
            GroupName = "Zeta Project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r2",
            Name = "r2",
            IsFavorite = true,
            GroupName = "alpha project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r3",
            Name = "r3",
            IsFavorite = true,
            GroupName = "Alpha Project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r4",
            Name = "r4",
            IsFavorite = true,
            GroupName = "   "
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.Equal(2, vm.State.ExistingGroupNames.Count);
        Assert.Equal("alpha project", vm.State.ExistingGroupNames[0]);
        Assert.Equal("Zeta Project", vm.State.ExistingGroupNames[1]);
    }

    [Fact]
    public async Task ToggleFavoriteGroupCommand_WhenGroupIsCollapsedByDefault_TogglesIsExpandedState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/sub1",
            Name = "sub1",
            IsFavorite = true,
            GroupName = "Backend Team",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.False(vm.State.FavoriteGroups[0].IsExpanded);

        vm.ToggleFavoriteGroupCommand.Execute("Backend Team").Subscribe();

        Assert.True(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.Contains("Backend Team", vm.State.ExpandedFavoriteGroupNames);

        vm.ToggleFavoriteGroupCommand.Execute("Backend Team").Subscribe();

        Assert.False(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.DoesNotContain("Backend Team", vm.State.ExpandedFavoriteGroupNames);
    }

    [Fact]
    public async Task ShowEditFavoriteModalCommand_WhenExistingFavoriteSelected_PopulatesModalInputsWithGroup()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/existing",
            Name = "existing",
            CustomName = "Custom Existing",
            IsFavorite = true,
            GroupName = "Core Group",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        vm.ShowEditFavoriteModalCommand.Execute("/repo/existing").Subscribe();

        Assert.True(vm.State.IsAddFavoriteModalVisible);
        Assert.True(vm.State.IsEditingExistingFavorite);
        Assert.Equal("/repo/existing", vm.State.TargetFavoritePath);
        Assert.Equal("Custom Existing", vm.State.NewFavoriteNameInput);
        Assert.Equal("Core Group", vm.State.NewFavoriteGroupInput);
    }

    [Fact]
    public async Task ConfirmAddFavoriteCommand_WithGroupNameProvided_PersistsGroupAndCustomName()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent-repo",
            Name = "agent-repo",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/agent-repo", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        vm.UpdateNewFavoriteNameInput("Agent Worker A");
        vm.UpdateNewFavoriteGroupInput("Cluster X");

        await vm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsAddFavoriteModalVisible);
        RecentRepository? updated = repo.Items.FirstOrDefault(x => x.Path == "/repo/agent-repo");
        Assert.NotNull(updated);
        Assert.True(updated.IsFavorite);
        Assert.Equal("Agent Worker A", updated.CustomName);
        Assert.Equal("Cluster X", updated.GroupName);
        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("Cluster X", vm.State.FavoriteGroups[0].Name);
    }

    [Fact]
    public async Task RemoveFromFavoritesCommand_WhenCurrentRepositoryUnfavorited_RevertsRepositoryNameToDirectoryName()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/my-project",
            Name = "my-project",
            IsFavorite = true,
            CustomName = "Custom Name",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Equal("Custom Name", vm.State.RepositoryName);

        await vm.RemoveFromFavoritesCommand.Execute("/repo/my-project").FirstAsync();

        Assert.Equal("my-project", vm.State.RepositoryName);
        Assert.Empty(vm.State.FavoriteRepositories);
        Assert.NotNull(vm.State.RecentNonFavoriteRepository);
        Assert.Equal("/repo/my-project", vm.State.RecentNonFavoriteRepository.Path);
    }

    [Fact]
    public void OpenRepositoryFromDockCommand_WithDifferentValidRepository_NavigatesToNewCommitLog()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        CommitLogViewModel vm = new(screen, "/repo/first", gitService, recentRepositoriesRepository: repo);

        vm.ToggleFavoritesDockCommand.Execute().Subscribe();
        Assert.True(vm.State.IsFavoritesDockOpen);

        vm.OpenRepositoryFromDockCommand.Execute("/repo/second").Subscribe();

        Assert.False(vm.State.IsFavoritesDockOpen);
        Assert.IsType<CommitLogViewModel>(screen.Router.NavigationStack.Last());
        CommitLogViewModel nextVm = (CommitLogViewModel)screen.Router.NavigationStack.Last();
        Assert.Equal("/repo/second", nextVm.State.RepositoryPath);
    }

    [Fact]
    public void FavoritesDockViewModel_OpenRepository_WhenNotGitRepository_ShowsNotGitRepositoryModal()
    {
        MockGitService gitService = new() { IsValidRepositoryFunc = _ => false };
        TestRecentRepositoriesRepository repo = new();
        GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock.FavoritesDockViewModel dockVm = new(repo, gitService, "/repo/current");

        dockVm.OpenRepositoryCommand.Execute("/path/to/folder").Subscribe();

        Assert.True(dockVm.State.IsNotGitRepositoryModalVisible);
        Assert.Equal("/path/to/folder", dockVm.State.NotGitRepositoryPath);
    }

    [Fact]
    public void FavoritesDockViewModel_HideNotGitRepositoryModal_HidesModal()
    {
        MockGitService gitService = new() { IsValidRepositoryFunc = _ => false };
        TestRecentRepositoriesRepository repo = new();
        GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock.FavoritesDockViewModel dockVm = new(repo, gitService, "/repo/current");

        dockVm.OpenRepositoryCommand.Execute("/path/to/folder").Subscribe();
        Assert.True(dockVm.State.IsNotGitRepositoryModalVisible);

        dockVm.HideNotGitRepositoryModalCommand.Execute().Subscribe();

        Assert.False(dockVm.State.IsNotGitRepositoryModalVisible);
        Assert.Equal(string.Empty, dockVm.State.NotGitRepositoryPath);
    }

    [Fact]
    public async Task FavoritesDockViewModel_ConfirmInitializeNotGitRepository_InitializesAndEmitsRepositorySelected()
    {
        MockGitService gitService = new() { IsValidRepositoryFunc = _ => false };
        TestRecentRepositoriesRepository repo = new();
        GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock.FavoritesDockViewModel dockVm = new(repo, gitService, "/repo/current");

        string? selectedRepo = null;
        dockVm.RepositorySelected.Subscribe(p => selectedRepo = p);

        dockVm.OpenRepositoryCommand.Execute("/path/to/folder").Subscribe();

        gitService.IsValidRepositoryFunc = _ => true;
        await dockVm.ConfirmInitializeNotGitRepositoryCommand.Execute();

        Assert.True(gitService.InitializeCalled);
        Assert.Equal("/path/to/folder", gitService.LastInitializedPath);
        Assert.False(dockVm.State.IsNotGitRepositoryModalVisible);
        Assert.Equal("/path/to/folder", selectedRepo);
    }
}
