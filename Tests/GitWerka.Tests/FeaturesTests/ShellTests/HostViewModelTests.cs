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
using ReactiveUI;
using Xunit;

public class HostViewModelTests
{
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockGitService : IGitService
    {
        public bool IsValidRepository(string path) => true;

        public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(directoryPath);

        public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(targetDirectoryPath);

        public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitRepositoryInfo
            {
                Name = "TestRepo",
                Path = repositoryPath,
                CurrentBranch = "main"
            });

        public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommitSummary>>(new List<CommitSummary>());

        public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.FromResult<CommitDetail?>(null);

        public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiffLine>>(new List<DiffLine>());

        public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkingTreeChange>>(new List<WorkingTreeChange>());

        public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchInfo>>(new List<BranchInfo>());

        public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
            Task.FromResult("new-sha");

        public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TagInfo>>(Array.Empty<TagInfo>());

        public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StashInfo>>(Array.Empty<StashInfo>());

        public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RemoteInfo>>(Array.Empty<RemoteInfo>());

        public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitConfigInfo { UserName = "Tester", UserEmail = "test@example.com" });

        public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryOperationStatus { CurrentOperation = RepositoryOperationType.None });

        public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode = GitMergeFastForwardMode.FastForward, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitMergeResult());

        public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RebaseStep>>(new List<RebaseStep>());

        public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

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
}
