namespace GitWerka.Tests.FeaturesTests.RepositoryManagerTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.RepositoryManager.Domain.Enums;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using ReactiveUI;
using Xunit;

public class RepositoryManagerViewModelTests
{

    [Fact]
    public void UpdateSearchQuery_FiltersRepositoriesByName()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "Alpha", Path = "/path/alpha" });
        repo.Items.Add(new RecentRepository { Name = "Beta", Path = "/path/beta" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.UpdateSearchQuery("alph");

        Assert.Single(vm.State.FilteredRepositories);
        Assert.Equal("Alpha", vm.State.FilteredRepositories[0].Name);
    }

    [Fact]
    public void ShowAndHideCloneModal_TogglesVisibilityState()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.False(vm.State.IsCloneModalVisible);

        vm.ShowCloneModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCloneModalVisible);

        vm.HideCloneModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsCloneModalVisible);
    }

    [Fact]
    public void ShowAndHideInitModal_TogglesVisibilityState()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.False(vm.State.IsInitModalVisible);

        vm.ShowInitModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsInitModalVisible);

        vm.HideInitModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsInitModalVisible);
    }

    [Fact]
    public void SelectedTab_DefaultsToFavorites_AndFiltersDisplayedRepositories()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "FavRepo", Path = "/path/fav", IsFavorite = true });
        repo.Items.Add(new RecentRepository { Name = "OtherRepo", Path = "/path/other", IsFavorite = false });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.Equal(RepositoryManagerTab.Favorites, vm.State.SelectedTab);
        Assert.Single(vm.State.DisplayedRepositories);
        Assert.Equal("FavRepo", vm.State.DisplayedRepositories[0].Name);
    }

    [Fact]
    public void SelectRecentTabCommand_And_SelectFavoritesTabCommand_SwitchesTabAndDisplayedRepositories()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "FavRepo", Path = "/path/fav", IsFavorite = true });
        repo.Items.Add(new RecentRepository { Name = "OtherRepo", Path = "/path/other", IsFavorite = false });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.SelectRecentTabCommand.Execute().Subscribe();
        Assert.Equal(RepositoryManagerTab.Recent, vm.State.SelectedTab);
        Assert.Equal(2, vm.State.DisplayedRepositories.Count);

        vm.SelectFavoritesTabCommand.Execute().Subscribe();
        Assert.Equal(RepositoryManagerTab.Favorites, vm.State.SelectedTab);
        Assert.Single(vm.State.DisplayedRepositories);
        Assert.Equal("FavRepo", vm.State.DisplayedRepositories[0].Name);
    }

    [Fact]
    public void UpdateSearchQuery_WhenOnFavoritesTab_FiltersByDisplayNameOrPath()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "repo1", CustomName = "SuperProject", Path = "/path/repo1", IsFavorite = true });
        repo.Items.Add(new RecentRepository { Name = "repo2", CustomName = "AnotherOne", Path = "/path/repo2", IsFavorite = true });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.UpdateSearchQuery("super");

        Assert.Single(vm.State.DisplayedRepositories);
        Assert.Equal("SuperProject", vm.State.DisplayedRepositories[0].DisplayName);

        vm.SelectRecentTabCommand.Execute().Subscribe();
    }
}
