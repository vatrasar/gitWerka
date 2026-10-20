namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for creating, cherry-picking, reverting, and resetting Git commits.
/// </summary>
public class GitCommitOperations
{
    /// <summary>
    /// Creates a commit with the staged changes in the Git index.
    /// </summary>
    public Task<string> CreateCommitAsync(
        string repositoryPath,
        string message,
        string authorName,
        string authorEmail,
        bool amendPreviousCommit = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Signature author = new(authorName, authorEmail, DateTimeOffset.Now);
            Signature committer = author;

            CommitOptions options = new() { AmendPreviousCommit = amendPreviousCommit };
            Commit commit = repository.Commit(message, author, committer, options);
            return commit.Sha;
        }, cancellationToken);
    }

    /// <summary>
    /// Applies changes from an existing commit to the current HEAD branch without advancing history.
    /// </summary>
    public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit commit = repository.Lookup<Commit>(commitSha)
                ?? throw new InvalidOperationException($"Commit '{commitSha}' not found.");

            Signature committer = GitServiceHelper.BuildSignatureOrDefault(repository);
            repository.CherryPick(commit, committer);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a new commit that reverts the changes introduced by the specified commit.
    /// </summary>
    public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit commit = repository.Lookup<Commit>(commitSha)
                ?? throw new InvalidOperationException($"Commit '{commitSha}' not found.");

            Signature revertAuthor = GitServiceHelper.BuildSignatureOrDefault(repository);
            repository.Revert(commit, revertAuthor);
        }, cancellationToken);
    }

    /// <summary>
    /// Resets the current branch HEAD to a specific commit using the requested reset mode (Soft, Mixed, Hard).
    /// </summary>
    public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit commit = repository.Lookup<Commit>(commitSha)
                ?? throw new InvalidOperationException($"Commit '{commitSha}' not found.");

            ResetMode mode = MapToResetMode(resetType);
            repository.Reset(mode, commit);
        }, cancellationToken);
    }

    private static ResetMode MapToResetMode(GitResetType resetType) => resetType switch
    {
        GitResetType.Soft => ResetMode.Soft,
        GitResetType.Mixed => ResetMode.Mixed,
        GitResetType.Hard => ResetMode.Hard,
        _ => ResetMode.Mixed
    };
}
