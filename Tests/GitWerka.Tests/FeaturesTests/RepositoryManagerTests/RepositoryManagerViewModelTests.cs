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
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockRecentRepositoriesRepository : IRecentRepositoriesRepository
    {
        public List<RecentRepository> Items { get; } = new();

        public Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentRepository>>(Items);
        }

        public Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default)
        {
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

        public Task SetFavoriteAsync(string repositoryPath, bool isFavorite, string? customName = null, CancellationToken cancellationToken = default)
        {
            var existing = Items.FirstOrDefault(x => x.Path == repositoryPath);
            if (existing != null)
            {
                Items.Remove(existing);
                Items.Add(existing with { IsFavorite = isFavorite, CustomName = isFavorite ? customName : null });
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RecentRepository> favorites = Items.Where(x => x.IsFavorite).OrderByDescending(x => x.LastOpenedAtUtc).ToList();
            return Task.FromResult(favorites);
        }

        public Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.Where(x => !x.IsFavorite).OrderByDescending(x => x.LastOpenedAtUtc).FirstOrDefault());
        }
    }

    private class MockGitService : IGitService
    {
        public bool IsValidRepository(string path) => true;

        public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(directoryPath);

        public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(targetDirectoryPath);

        public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitRepositoryInfo());

        public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommitSummary>>(Array.Empty<CommitSummary>());

        public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.FromResult<CommitDetail?>(null);

        public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiffLine>>(Array.Empty<DiffLine>());

        public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkingTreeChange>>(Array.Empty<WorkingTreeChange>());

        public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchInfo>>(Array.Empty<BranchInfo>());

        public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
            Task.FromResult("mock-sha");

        public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TagInfo>>(Array.Empty<TagInfo>());

        public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StashInfo>>(Array.Empty<StashInfo>());

        public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RemoteInfo>>(Array.Empty<RemoteInfo>());

        public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitConfigInfo());

        public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryOperationStatus());

        public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode = GitMergeFastForwardMode.FastForward, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitMergeResult());

        public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RebaseStep>>(Array.Empty<RebaseStep>());

        public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

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
        Assert.Single(vm.State.DisplayedRepositories);
        Assert.Equal("SuperProject", vm.State.DisplayedRepositories[0].DisplayName);
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
        public Task<string> GenerateCommitMessageAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
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
}
