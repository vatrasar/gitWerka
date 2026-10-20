namespace GitWerka.Tests.FeaturesTests.RepositoryManagerTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using ReactiveUI;
using Xunit;

public class RepositoryManagerFavoritesAndSettingsTests
{
    [Fact]
    public void FavoriteGrouping_WhenFavoritesContainGroupsAndUngrouped_PopulatesStateCorrectly()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "agent1", CustomName = "Agent 1", Path = "/path/agent1", IsFavorite = true, GroupName = "AI Project" });
        repo.Items.Add(new RecentRepository { Name = "agent2", CustomName = "Agent 2", Path = "/path/agent2", IsFavorite = true, GroupName = "AI Project" });
        repo.Items.Add(new RecentRepository { Name = "solo", CustomName = "Solo Repo", Path = "/path/solo", IsFavorite = true, GroupName = null });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("AI Project", vm.State.FavoriteGroups[0].Name);
        Assert.Equal(2, vm.State.FavoriteGroups[0].Repositories.Count);
        Assert.Single(vm.State.UngroupedFavoriteRepositories);
        Assert.Equal("Solo Repo", vm.State.UngroupedFavoriteRepositories[0].DisplayName);
    }

    [Fact]
    public void ExistingGroupNames_WhenFavoritesExist_ReturnsAllDistinctSortedGroupNamesEvenWithSearchQuery()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "r1", Path = "/path/1", IsFavorite = true, GroupName = "Backend" });
        repo.Items.Add(new RecentRepository { Name = "r2", Path = "/path/2", IsFavorite = true, GroupName = "Frontend" });
        repo.Items.Add(new RecentRepository { Name = "r3", Path = "/path/3", IsFavorite = true, GroupName = "frontend" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.Equal(2, vm.State.ExistingGroupNames.Count);
        Assert.Equal("Backend", vm.State.ExistingGroupNames[0]);
        Assert.Equal("Frontend", vm.State.ExistingGroupNames[1]);

        vm.UpdateSearchQuery("Backend");

        Assert.Equal(2, vm.State.ExistingGroupNames.Count);
    }

    [Fact]
    public void ToggleFavoriteGroupCommand_WhenGroupIsCollapsedByDefault_TogglesIsExpandedState()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "repoA", Path = "/path/a", IsFavorite = true, GroupName = "Group Alpha" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        Assert.False(vm.State.FavoriteGroups[0].IsExpanded);

        vm.ToggleFavoriteGroupCommand.Execute("Group Alpha").Subscribe();

        Assert.True(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.Contains("Group Alpha", vm.State.ExpandedFavoriteGroupNames);

        vm.ToggleFavoriteGroupCommand.Execute("Group Alpha").Subscribe();

        Assert.False(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.DoesNotContain("Group Alpha", vm.State.ExpandedFavoriteGroupNames);
    }

    [Fact]
    public void ShowEditFavoriteModalCommand_WhenInvoked_PopulatesModalInputs()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "repoA", CustomName = "Custom A", Path = "/path/a", IsFavorite = true, GroupName = "Group Alpha" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.ShowEditFavoriteModalCommand.Execute("/path/a").Subscribe();

        Assert.True(vm.State.IsEditFavoriteModalVisible);
        Assert.Equal("/path/a", vm.State.TargetFavoritePath);
        Assert.Equal("Custom A", vm.State.EditFavoriteNameInput);
        Assert.Equal("Group Alpha", vm.State.EditFavoriteGroupInput);
    }

    [Fact]
    public async Task ConfirmEditFavoriteCommand_WhenExecuted_UpdatesCustomNameAndGroup()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "repoA", CustomName = "Original Name", Path = "/path/a", IsFavorite = true, GroupName = "Old Group" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.ShowEditFavoriteModalCommand.Execute("/path/a").Subscribe();
        vm.SetEditFavoriteNameInput("New Name");
        vm.SetEditFavoriteGroupInput("New Group");

        await vm.ConfirmEditFavoriteCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsEditFavoriteModalVisible);
        RecentRepository updated = repo.Items.First(x => x.Path == "/path/a");
        Assert.Equal("New Name", updated.CustomName);
        Assert.Equal("New Group", updated.GroupName);
        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("New Group", vm.State.FavoriteGroups[0].Name);
    }

    [Fact]
    public void UpdateSearchQuery_WhenFilteringFavoriteGroups_MatchesGroupNameOrChildren()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "worker1", CustomName = "Worker 1", Path = "/path/w1", IsFavorite = true, GroupName = "Agent Fleet" });
        repo.Items.Add(new RecentRepository { Name = "other", CustomName = "Other Repo", Path = "/path/other", IsFavorite = true, GroupName = "Misc" });
        repo.Items.Add(new RecentRepository { Name = "standalone", CustomName = "Standalone Worker", Path = "/path/standalone", IsFavorite = true });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        vm.UpdateSearchQuery("Worker");

        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("Agent Fleet", vm.State.FavoriteGroups[0].Name);
        Assert.Single(vm.State.FavoriteGroups[0].Repositories);
        Assert.Single(vm.State.UngroupedFavoriteRepositories);
        Assert.Equal("Standalone Worker", vm.State.UngroupedFavoriteRepositories[0].DisplayName);
    }

    [Fact]
    public async Task RemoveFromFavoritesCommand_WhenInvoked_RemovesRepositoryFromFavorites()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository { Name = "fav", CustomName = "My Fav", Path = "/path/fav", IsFavorite = true, GroupName = "Projects" });

        MockGitService gitService = new();
        RepositoryManagerViewModel vm = new(screen, repo, gitService);

        await vm.RemoveFromFavoritesCommand.Execute("/path/fav").FirstAsync();

        Assert.Empty(vm.State.FavoriteGroups);
        Assert.Empty(vm.State.FavoriteRepositories);
        RecentRepository? updated = repo.Items.FirstOrDefault(x => x.Path == "/path/fav");
        Assert.NotNull(updated);
        Assert.False(updated.IsFavorite);
    }

    private class MockApplicationSettingsRepository : IApplicationSettingsRepository
    {
        public ApplicationSettings Settings { get; set; } = new();

        public Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Settings);

        public Task SaveSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }

    private class MockAiConfigurationRepository : IAiConfigurationRepository
    {
        public AiConfiguration Configuration { get; set; } = new();

        public Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Configuration);

        public Task SaveConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Configuration = configuration;
            return Task.CompletedTask;
        }

        public Task<string?> GetProjectDescriptionAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task SaveProjectDescriptionAsync(string repositoryPath, string? description, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [Fact]
    public async Task SettingsWorkflow_OnWelcomeScreen_DisplaysAndSavesGlobalSettingsOnly()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository appSettingsRepo = new();
        appSettingsRepo.Settings = new ApplicationSettings { DefaultProjectsDirectory = "/home/vatrasar/projekty" };
        MockAiConfigurationRepository aiRepo = new();
        aiRepo.Configuration = new AiConfiguration { ApiKey = "sk-test-key", Model = "custom-model" };

        RepositoryManagerViewModel vm = new(
            screen,
            repo,
            gitService,
            applicationSettingsRepository: appSettingsRepo,
            aiConfigurationRepository: aiRepo);

        await vm.LoadApplicationSettingsCommand.Execute().FirstAsync();

        Assert.Equal("/home/vatrasar/projekty", vm.State.DefaultProjectsDirectory);

        await vm.ShowSettingsModalCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsSettingsModalVisible);
        Assert.Equal("/home/vatrasar/projekty", vm.State.DefaultProjectsDirectoryInput);
        Assert.Equal("sk-test-key", vm.State.AiApiKeyInput);
        Assert.Equal("custom-model", vm.State.AiModelInput);

        vm.SetDefaultProjectsDirectoryInput("/home/vatrasar/inne_projekty");
        vm.SetAiApiKeyInput("sk-updated-key");

        await vm.ExecuteSaveSettingsCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsSettingsModalVisible);
        Assert.Equal("/home/vatrasar/inne_projekty", vm.State.DefaultProjectsDirectory);
        Assert.Equal("/home/vatrasar/inne_projekty", appSettingsRepo.Settings.DefaultProjectsDirectory);
        Assert.Equal("sk-updated-key", aiRepo.Configuration.ApiKey);
    }

    [Fact]
    public async Task HideSettingsModal_ClosesModalWithoutSaving()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository appSettingsRepo = new();
        appSettingsRepo.Settings = new ApplicationSettings { DefaultProjectsDirectory = "/home/vatrasar/projekty" };

        RepositoryManagerViewModel vm = new(
            screen,
            repo,
            gitService,
            applicationSettingsRepository: appSettingsRepo);

        await vm.ShowSettingsModalCommand.Execute().FirstAsync();
        Assert.True(vm.State.IsSettingsModalVisible);

        vm.HideSettingsModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsSettingsModalVisible);
    }

    private class MockAiCommitMessageService : IAiCommitMessageService
    {
        public Task<string> GenerateCommitMessageAsync(
            string repositoryPath,
            IProgress<AiRetryAttempt>? retryProgress = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("feat: test commit message");
    }

    [Fact]
    public async Task OpenRepositoryPath_WithAiCommitMessageService_NavigatesToCommitLog()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new();

        RepositoryManagerViewModel vm = new(
            screen,
            repo,
            gitService,
            aiCommitMessageService: aiService);

        await vm.OpenRepositoryPathAsync("/valid/repo");

        Assert.Single(screen.Router.NavigationStack);
        Assert.IsType<CommitLogViewModel>(screen.Router.NavigationStack[0]);
    }

    [Fact]
    public async Task OpenRepositoryPath_ResolvesRootFromSubdirectory()
    {
        MockScreen screen = new();
        MockRecentRepositoriesRepository repo = new();
        MockGitService gitService = new();

        RepositoryManagerViewModel vm = new(
            screen,
            repo,
            gitService);

        await vm.OpenRepositoryPathAsync("/valid/repo/sub/directory");

        Assert.Single(screen.Router.NavigationStack);
        CommitLogViewModel commitLogVm = Assert.IsType<CommitLogViewModel>(screen.Router.NavigationStack[0]);
        Assert.NotNull(commitLogVm);
    }
}
