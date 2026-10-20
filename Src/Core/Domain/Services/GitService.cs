namespace GitWerka.Core.Domain.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services.Git;
using LibGit2Sharp;

/// <summary>
/// Service coordinating Git operations and graph topology generation using specialized operation delegates.
/// </summary>
public class GitService : IGitService
{
    private readonly GitBranchOperations _branches;
    private readonly GitCommitOperations _commits;
    private readonly GitConflictOperations _conflicts;
    private readonly GitGraphOperations _graph;
    private readonly GitMergeRebaseOperations _mergeRebase;
    private readonly GitRemoteOperations _remotes;
    private readonly GitWorkingTreeOperations _workingTree;

    public GitService(
        GitBranchOperations? branches = null,
        GitCommitOperations? commits = null,
        GitConflictOperations? conflicts = null,
        GitGraphOperations? graph = null,
        GitMergeRebaseOperations? mergeRebase = null,
        GitRemoteOperations? remotes = null,
        GitWorkingTreeOperations? workingTree = null)
    {
        _branches = branches ?? new GitBranchOperations();
        _commits = commits ?? new GitCommitOperations();
        _conflicts = conflicts ?? new GitConflictOperations();
        _graph = graph ?? new GitGraphOperations();
        _mergeRebase = mergeRebase ?? new GitMergeRebaseOperations();
        _remotes = remotes ?? new GitRemoteOperations();
        _workingTree = workingTree ?? new GitWorkingTreeOperations();
    }

    /// <summary>
    /// Checks whether the specified directory is a valid Git repository.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    public bool IsValidRepository(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        return Repository.IsValid(path);
    }

    /// <summary>
    /// Finds the Git repository root directory for the given path.
    /// Usages: RepositoryManagerViewModel, HostViewModel.
    /// </summary>
    public string? FindRepositoryRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            string? discoveredGitPath = Repository.Discover(path);
            if (string.IsNullOrEmpty(discoveredGitPath))
            {
                return null;
            }

            using Repository repository = new(discoveredGitPath);
            string? workingDirectory = repository.Info.WorkingDirectory;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(workingDirectory));
            }

            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(discoveredGitPath));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error discovering repository root for '{path}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initializes a new Git repository at the specified directory path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            Directory.CreateDirectory(directoryPath);
            Repository.Init(directoryPath, isBare);
            return Path.GetFullPath(directoryPath);
        }, cancellationToken);
    }

    /// <summary>
    /// Clones a remote Git repository to the target directory path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            CloneOptions options = new();
            return Repository.Clone(sourceUrl, targetDirectoryPath, options);
        }, cancellationToken);
    }

    /// <summary>
    /// Reads summary information from an existing Git repository.
    /// Usages: RepositoryManagerViewModel, HostViewModel, CommitLogViewModel.
    /// </summary>
    public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            string repoName = new DirectoryInfo(repository.Info.WorkingDirectory ?? repositoryPath).Name;
            Branch? head = repository.Head;
            Commit? tip = head?.Tip;

            return new GitRepositoryInfo
            {
                Path = repository.Info.WorkingDirectory ?? repositoryPath,
                Name = repoName,
                CurrentBranch = head?.FriendlyName ?? string.Empty,
                HeadCommitSha = tip?.Sha ?? string.Empty,
                HeadCommitMessage = tip?.MessageShort ?? string.Empty,
                IsHeadDetached = repository.Info.IsHeadDetached,
                AheadBy = head?.TrackingDetails?.AheadBy ?? 0,
                BehindBy = head?.TrackingDetails?.BehindBy ?? 0
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Reads commits and computes visual graph lane indices.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, string? branchName = null, CancellationToken cancellationToken = default) =>
        _graph.GetCommitLogAsync(repositoryPath, maxCount, maxBranches, branchName, cancellationToken);

    /// <summary>
    /// Retrieves full details for a commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
        _graph.GetCommitDetailAsync(repositoryPath, commitSha, cancellationToken);

    /// <summary>
    /// Computes the diff lines for a specific file.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
        _workingTree.GetFileDiffLinesAsync(repositoryPath, commitSha, filePath, cancellationToken);

    /// <summary>
    /// Reads working tree modifications including line addition and deletion counts.
    /// Usages: CommitLogWorkingTreeCoordinator, CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _workingTree.GetWorkingTreeChangesAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Stages a file in the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        _workingTree.StageFileAsync(repositoryPath, filePath, cancellationToken);

    /// <summary>
    /// Unstages a file from the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        _workingTree.UnstageFileAsync(repositoryPath, filePath, cancellationToken);

    /// <summary>
    /// Discards uncommitted modifications to a file.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        _workingTree.DiscardFileChangesAsync(repositoryPath, filePath, cancellationToken);

    /// <summary>
    /// Creates a commit with the staged changes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
        _commits.CreateCommitAsync(repositoryPath, message, authorName, authorEmail, amendPreviousCommit, cancellationToken);

    /// <summary>
    /// Cherry-picks a commit onto current HEAD.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
        _commits.CherryPickCommitAsync(repositoryPath, commitSha, cancellationToken);

    /// <summary>
    /// Reverts an existing commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
        _commits.RevertCommitAsync(repositoryPath, commitSha, cancellationToken);

    /// <summary>
    /// Resets the current branch to a specific commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default) =>
        _commits.ResetBranchToCommitAsync(repositoryPath, commitSha, resetType, cancellationToken);

    /// <summary>
    /// Reads all branches.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _branches.GetBranchesAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Checks out the specified branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default) =>
        _branches.CheckoutBranchAsync(repositoryPath, branchName, cancellationToken);

    /// <summary>
    /// Creates a new branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default) =>
        _branches.CreateBranchAsync(repositoryPath, branchName, targetShaOrRef, cancellationToken);

    /// <summary>
    /// Deletes a branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) =>
        _branches.DeleteBranchAsync(repositoryPath, branchName, isRemote, cancellationToken);

    /// <summary>
    /// Renames a branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) =>
        _branches.RenameBranchAsync(repositoryPath, oldBranchName, newBranchName, cancellationToken);

    /// <summary>
    /// Reads all tags.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _branches.GetTagsAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Creates a tag.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default) =>
        _branches.CreateTagAsync(repositoryPath, tagName, commitSha, message, cancellationToken);

    /// <summary>
    /// Deletes a tag.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) =>
        _branches.DeleteTagAsync(repositoryPath, tagName, cancellationToken);

    /// <summary>
    /// Reads all stashes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _branches.GetStashesAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Creates a new stash.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) =>
        _branches.CreateStashAsync(repositoryPath, message, cancellationToken);

    /// <summary>
    /// Applies a stash.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
        _branches.ApplyStashAsync(repositoryPath, stashIndex, cancellationToken);

    /// <summary>
    /// Pops a stash.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
        _branches.PopStashAsync(repositoryPath, stashIndex, cancellationToken);

    /// <summary>
    /// Drops a stash.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
        _branches.DropStashAsync(repositoryPath, stashIndex, cancellationToken);

    /// <summary>
    /// Reads all remotes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _remotes.GetRemotesAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Adds a remote.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default) =>
        _remotes.AddRemoteAsync(repositoryPath, remoteName, url, cancellationToken);

    /// <summary>
    /// Removes a remote.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default) =>
        _remotes.RemoveRemoteAsync(repositoryPath, remoteName, cancellationToken);

    /// <summary>
    /// Fetches from remotes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
        _remotes.FetchAsync(repositoryPath, remoteName, credentials, cancellationToken);

    /// <summary>
    /// Pulls changes into current HEAD branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
        _remotes.PullAsync(repositoryPath, remoteName, branchName, credentials, cancellationToken);

    /// <summary>
    /// Pushes current branch to remote.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
        _remotes.PushAsync(repositoryPath, remoteName, branchName, credentials, cancellationToken);

    /// <summary>
    /// Reads Git author configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _remotes.GetGitConfigAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Sets Git author configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) =>
        _remotes.SetGitConfigAsync(repositoryPath, userName, userEmail, cancellationToken);

    /// <summary>
    /// Checks repository operation status.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _mergeRebase.GetRepositoryOperationStatusAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Merges branch into current branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default) =>
        _mergeRebase.MergeBranchAsync(repositoryPath, sourceBranchName, fastForwardMode, cancellationToken);

    /// <summary>
    /// Aborts ongoing merge.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _mergeRebase.AbortMergeAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Continues merge after resolving conflicts.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default) =>
        _mergeRebase.ContinueMergeAsync(repositoryPath, commitMessage, cancellationToken);

    /// <summary>
    /// Retrieves rebase steps.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
        _mergeRebase.GetInteractiveRebaseStepsAsync(repositoryPath, upstreamCommitSha, cancellationToken);

    /// <summary>
    /// Executes interactive rebase.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default) =>
        _mergeRebase.ExecuteInteractiveRebaseAsync(repositoryPath, upstreamCommitSha, steps, cancellationToken);

    /// <summary>
    /// Aborts ongoing rebase.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _mergeRebase.AbortRebaseAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Continues rebase after resolving conflicts.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _mergeRebase.ContinueRebaseAsync(repositoryPath, cancellationToken);

    /// <summary>
    /// Retrieves conflict file details.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        _conflicts.GetConflictFileAsync(repositoryPath, filePath, cancellationToken);

    /// <summary>
    /// Saves resolved conflict file.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default) =>
        _conflicts.SaveConflictResolutionAsync(repositoryPath, filePath, resolvedContent, cancellationToken);

    /// <summary>
    /// Marks all conflicts resolved.
    /// Usages: CommitLogViewModel.
    /// </summary>
    public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        _conflicts.MarkAllConflictsResolvedAsync(repositoryPath, cancellationToken);
}
