namespace GitWerka.Tests.FeaturesTests.RepositoryManagerTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
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
}
