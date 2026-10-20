namespace GitWerka.Tests.FeaturesTests.MergeRebaseTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using ReactiveUI;

internal sealed class MockScreen : IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}

internal sealed class MockGitService : IGitService
{
    public ConflictFileModel ConflictModelToReturn { get; set; } = new();
    public string? SavedRepositoryPath { get; private set; }
    public string? SavedFilePath { get; private set; }
    public string? SavedResolvedContent { get; private set; }
    public bool MarkAllConflictsResolvedCalled { get; private set; }

    public bool IsValidRepository(string path) => true;

    public string? FindRepositoryRoot(string path) => path;

    public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(directoryPath);

    public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(targetDirectoryPath);

    public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitRepositoryInfo());

    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, string? branchName = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommitSummary>>(Array.Empty<CommitSummary>());

    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string sha, CancellationToken cancellationToken = default) =>
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

    public Task CheckoutCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
        Task.FromResult("sha");

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

    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitMergeResult { Status = "Merged", HasConflicts = false });

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

    public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ConflictModelToReturn);
    }

    public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default)
    {
        SavedRepositoryPath = repositoryPath;
        SavedFilePath = filePath;
        SavedResolvedContent = resolvedContent;
        return Task.CompletedTask;
    }

    public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        MarkAllConflictsResolvedCalled = true;
        return Task.CompletedTask;
    }

    public Task<GitRepositoryStateSnapshot> GetRepositoryStateSnapshotAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitRepositoryStateSnapshot());
}

internal static class ConflictResolverTestHelper
{
    public static ConflictFileModel CreateSampleConflictModel()
    {
        return new ConflictFileModel
        {
            FilePath = "src/index.js",
            FileName = "index.js",
            TotalConflicts = 1,
            OursCommitSha = "aaaaaaa",
            OursBranch = "main",
            TheirsCommitSha = "bbbbbbb",
            TheirsBranch = "feature/login",
            Chunks = ImmutableList.Create(
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("line 1", "line 2")
                },
                new ConflictFileChunk
                {
                    IsConflict = true,
                    HunkIndex = 1,
                    OursLines = ImmutableList.Create("ours modification"),
                    TheirsLines = ImmutableList.Create("theirs modification"),
                    IsOursSelected = true,
                    IsTheirsSelected = false
                },
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("line 4")
                })
        };
    }
}
