namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git branches, tags, and stashes.
/// </summary>
public class GitBranchOperations
{
    /// <summary>
    /// Reads all local and remote branches.
    /// </summary>
    public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            List<BranchInfo> list = repository.Branches.Select(b => new BranchInfo
            {
                Name = b.FriendlyName,
                IsRemote = b.IsRemote,
                IsCurrentHead = b.IsCurrentRepositoryHead,
                TipSha = b.Tip?.Sha ?? string.Empty
            }).ToList();

            return (IReadOnlyList<BranchInfo>)list;
        }, cancellationToken);
    }

    /// <summary>
    /// Checks out the specified branch.
    /// </summary>
    public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[branchName];
            if (branch == null)
            {
                branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
            }

            if (branch == null)
            {
                throw new InvalidOperationException($"Branch '{branchName}' not found in repository.");
            }

            Commands.Checkout(repository, branch);
        }, cancellationToken);
    }

    /// <summary>
    /// Checks out the specified commit into a detached HEAD state.
    /// Usages: GitService, CommitLogViewModel.
    /// </summary>
    public Task CheckoutCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                throw new InvalidOperationException($"Commit '{commitSha}' not found in repository.");
            }

            Commands.Checkout(repository, commit);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a new branch pointing to HEAD or a specified commit.
    /// </summary>
    public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit targetCommit = ResolveBranchTarget(repository, targetShaOrRef);
            repository.CreateBranch(branchName, targetCommit);
        }, cancellationToken);
    }

    /// <summary>
    /// Deletes a branch from the repository.
    /// </summary>
    public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[branchName]
                ?? repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);

            if (branch != null)
            {
                repository.Branches.Remove(branch);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Renames a local branch.
    /// </summary>
    public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[oldBranchName]
                ?? repository.Branches.FirstOrDefault(b => b.FriendlyName == oldBranchName);

            if (branch != null)
            {
                repository.Branches.Rename(branch, newBranchName);
            }
        }, cancellationToken);
    }

    private static Commit ResolveBranchTarget(Repository repo, string? targetShaOrRef)
    {
        if (string.IsNullOrWhiteSpace(targetShaOrRef))
        {
            return repo.Head.Tip;
        }

        return repo.Lookup<Commit>(targetShaOrRef) ?? repo.Head.Tip;
    }

    /// <summary>
    /// Reads all tags in the repository.
    /// </summary>
    public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            List<TagInfo> tags = repository.Tags.Select(MapToTagInfo).ToList();
            return (IReadOnlyList<TagInfo>)tags;
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a lightweight or annotated tag on the specified target commit.
    /// </summary>
    public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            GitObject target = ResolveTagTarget(repository, commitSha);
            ApplyTagToRepository(repository, tagName, target.Sha, message);
        }, cancellationToken);
    }

    /// <summary>
    /// Deletes a tag from the repository.
    /// </summary>
    public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Tags.Remove(tagName);
        }, cancellationToken);
    }

    private static TagInfo MapToTagInfo(Tag tag)
    {
        return new TagInfo
        {
            Name = tag.FriendlyName,
            TargetSha = tag.Target?.Sha ?? string.Empty,
            Message = tag.IsAnnotated ? tag.Annotation?.Message : null,
            IsAnnotated = tag.IsAnnotated
        };
    }

    private static GitObject ResolveTagTarget(Repository repo, string? commitSha)
    {
        if (string.IsNullOrWhiteSpace(commitSha))
        {
            return repo.Head.Tip;
        }

        return repo.Lookup(commitSha) ?? repo.Head.Tip;
    }

    private static void ApplyTagToRepository(Repository repo, string tagName, string targetSha, string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Signature sig = GitServiceHelper.BuildSignatureOrDefault(repo);
            repo.ApplyTag(tagName, targetSha, sig, message);
            return;
        }

        repo.ApplyTag(tagName, targetSha);
    }

    /// <summary>
    /// Reads all stashes saved in the repository.
    /// </summary>
    public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run<IReadOnlyList<StashInfo>>(() =>
        {
            using Repository repository = new(repositoryPath);
            return repository.Stashes.Select((stash, index) => new StashInfo
            {
                Index = index,
                Message = stash.Message,
                Sha = stash.WorkTree?.Sha ?? string.Empty
            }).ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// Saves current working tree and index changes into a new stash.
    /// </summary>
    public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Signature sign = GitServiceHelper.BuildSignatureOrDefault(repository);
            repository.Stashes.Add(sign, message);
        }, cancellationToken);
    }

    /// <summary>
    /// Applies a stash at the specified index to the working directory without removing it.
    /// </summary>
    public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Stashes.Apply(stashIndex);
        }, cancellationToken);
    }

    /// <summary>
    /// Applies a stash at the specified index and drops it from the stash stack.
    /// </summary>
    public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Stashes.Pop(stashIndex);
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a stash entry from the stack without applying it.
    /// </summary>
    public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Stashes.Remove(stashIndex);
        }, cancellationToken);
    }
}
