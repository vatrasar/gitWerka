namespace GitWerka.Core.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Service implementation providing Git operations and graph topology generation through LibGit2Sharp.
/// </summary>
public class GitService : IGitService
{
    /// <summary>
    /// Checks whether the specified file system directory is a valid Git repository.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the path contains a valid repository; otherwise false.</returns>
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
    /// Usages: MainWindowViewModel, RepositoryManagerViewModel.
    /// </summary>
    /// <param name="path">The file system path to search from.</param>
    /// <returns>The root path of the Git repository if found; otherwise null.</returns>
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
    /// <param name="directoryPath">Directory path where the repository will be created.</param>
    /// <param name="isBare">Whether to create a bare repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the initialized repository.</returns>
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
    /// <param name="sourceUrl">Remote repository URL.</param>
    /// <param name="targetDirectoryPath">Target local folder for cloning.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the cloned repository.</returns>
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
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitRepositoryInfo model with summary data.</returns>
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
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="maxCount">Maximum number of commits to retrieve.</param>
    /// <param name="maxBranches">Maximum number of visible branches to include in graph topology.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of commit summary objects with graph topology.</returns>
    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            var branchMap = BuildReferenceMap(repository.Branches);
            var tagMap = BuildReferenceMap(repository.Tags);

            int effectiveMaxBranches = maxBranches > 0 ? maxBranches : 5;
            var reachableCommits = BuildReachableCommitList(repository, effectiveMaxBranches);
            CommitFilter filter = new()
            {
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
                IncludeReachableFrom = reachableCommits.Count > 0 ? reachableCommits : null
            };

            List<CommitSummary> summaries = new();
            Dictionary<int, string> reservedLanes = new();

            StatusOptions statusOptions = new() { IncludeIgnored = false };
            RepositoryStatus status = repository.RetrieveStatus(statusOptions);
            int uncommittedCount = status.Count();

            if (uncommittedCount > 0)
            {
                string headSha = repository.Head.Tip?.Sha ?? string.Empty;
                if (!string.IsNullOrEmpty(headSha))
                {
                    reservedLanes[0] = headSha;
                }

                summaries.Add(CreateWipCommitSummary(repository, uncommittedCount));
            }

            foreach (Commit commit in repository.Commits.QueryBy(filter).Take(maxCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CommitSummary node = ProcessCommitNode(commit, reservedLanes, branchMap, tagMap);
                summaries.Add(node);
            }

            ApplyGraphWidth(summaries);

            return (IReadOnlyList<CommitSummary>)summaries;
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves full commit details and changed files for a specific commit SHA.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Full commit SHA identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>CommitDetail model containing full metadata and changed files.</returns>
    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return null;
            }

            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            TreeChanges changes = repository.Diff.Compare<TreeChanges>(parentTree, commit.Tree);
            List<FileDiffSummary> changedFiles = changes.Select(c => MapToFileDiffSummary(c, repository, parentTree, commit.Tree)).ToList();

            return new CommitDetail
            {
                Sha = commit.Sha,
                ShortSha = commit.Sha.Substring(0, Math.Min(7, commit.Sha.Length)),
                Message = commit.Message.TrimEnd('\r', '\n'),
                AuthorName = commit.Author.Name,
                AuthorEmail = commit.Author.Email,
                AuthorDate = commit.Author.When,
                CommitterName = commit.Committer.Name,
                CommitterEmail = commit.Committer.Email,
                CommitterDate = commit.Committer.When,
                ParentShas = commit.Parents.Select(p => p.Sha).ToImmutableList(),
                ChangedFiles = changedFiles.ToImmutableList()
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Computes diff lines for a specific file in a commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Commit SHA identifier.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of parsed diff lines.</returns>
    public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);

            if (string.Equals(commitSha, "WIP", StringComparison.OrdinalIgnoreCase))
            {
                return GetWorkingTreeFileDiffLines(repository, repositoryPath, filePath);
            }

            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return (IReadOnlyList<DiffLine>)Array.Empty<DiffLine>();
            }

            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            CompareOptions options = new() { ContextLines = 3 };
            Patch patch = repository.Diff.Compare<Patch>(parentTree, commit.Tree, new[] { filePath }, null, options);
            PatchEntryChanges? entry = patch[filePath];

            if (entry == null)
            {
                return (IReadOnlyList<DiffLine>)Array.Empty<DiffLine>();
            }

            return ParsePatchLines(entry.Patch);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves all staged and unstaged file modifications in the working tree.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of working tree file modifications.</returns>
    public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            StatusOptions statusOptions = new() { IncludeIgnored = false };
            RepositoryStatus status = repository.RetrieveStatus(statusOptions);
            List<WorkingTreeChange> changes = new();

            foreach (StatusEntry entry in status)
            {
                AppendWorkingTreeEntries(entry, changes);
            }

            foreach (Conflict conflict in repository.Index.Conflicts)
            {
                string? conflictPath = conflict.Ours?.Path ?? conflict.Theirs?.Path ?? conflict.Ancestor?.Path;
                if (!string.IsNullOrEmpty(conflictPath) && !changes.Any(c => c.Path == conflictPath && c.Status == FileChangeStatus.Conflicted))
                {
                    changes.RemoveAll(c => c.Path == conflictPath);
                    changes.Add(new WorkingTreeChange
                    {
                        Path = conflictPath,
                        Status = FileChangeStatus.Conflicted,
                        IsStaged = false
                    });
                }
            }

            return (IReadOnlyList<WorkingTreeChange>)changes;
        }, cancellationToken);
    }

    /// <summary>
    /// Stages an unstaged file in the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to stage.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commands.Stage(repository, filePath);
        }, cancellationToken);
    }

    /// <summary>
    /// Unstages a staged file from the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to unstage.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commands.Unstage(repository, filePath);
        }, cancellationToken);
    }

    /// <summary>
    /// Discards uncommitted modifications of a file in the working tree.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to revert.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            CheckoutOptions options = new() { CheckoutModifiers = CheckoutModifiers.Force };
            repository.CheckoutPaths("HEAD", new[] { filePath }, options);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves all local and remote branches in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of branch descriptions.</returns>
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
    /// Checks out a branch by friendly name.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">Branch name to checkout.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[branchName];
            if (branch == null)
            {
                throw new InvalidOperationException($"Branch '{branchName}' not found.");
            }

            Commands.Checkout(repository, branch);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a new commit or amends the previous commit from currently staged changes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="message">Commit message.</param>
    /// <param name="authorName">Author name.</param>
    /// <param name="authorEmail">Author email.</param>
    /// <param name="amendPreviousCommit">Whether to amend the previous commit instead of creating a new commit.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The newly created or amended commit SHA.</returns>
    public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Signature signature = new(authorName, authorEmail, DateTimeOffset.Now);
            return CommitChanges(repository, message, signature, amendPreviousCommit);
        }, cancellationToken);
    }

    private static string CommitChanges(Repository repository, string message, Signature signature, bool amendPreviousCommit)
    {
        if (amendPreviousCommit)
        {
            return CommitAmend(repository, message, signature);
        }

        Commit commit = repository.Commit(message, signature, signature);
        return commit.Sha;
    }

    private static string CommitAmend(Repository repository, string message, Signature signature)
    {
        if (repository.Head.Tip == null)
        {
            throw new InvalidOperationException("Cannot amend commit: No previous commit found.");
        }

        CommitOptions options = new() { AmendPreviousCommit = true };
        Commit commit = repository.Commit(message, signature, signature, options);
        return commit.Sha;
    }

    /// <summary>
    /// Creates a new local branch pointing to a specific commit or HEAD.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">New branch name.</param>
    /// <param name="targetShaOrRef">Target commit SHA or reference.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
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
    /// Deletes a local or remote branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">Branch name to delete.</param>
    /// <param name="isRemote">Whether the branch is remote.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[branchName];
            if (branch != null)
            {
                repository.Branches.Remove(branch);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Renames an existing local branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="oldBranchName">Current branch name.</param>
    /// <param name="newBranchName">New target branch name.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? branch = repository.Branches[oldBranchName];
            if (branch != null)
            {
                repository.Branches.Rename(branch, newBranchName);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves all tags in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of tag information.</returns>
    public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run<IReadOnlyList<TagInfo>>(() =>
        {
            using Repository repository = new(repositoryPath);
            return repository.Tags.Select(MapToTagInfo).ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a lightweight or annotated tag on a commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="tagName">Tag name to create.</param>
    /// <param name="commitSha">Target commit SHA (defaults to HEAD if null).</param>
    /// <param name="message">Optional tag annotation message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
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
    /// Deletes a tag by name.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="tagName">Tag name to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Tags.Remove(tagName);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves all stash entries in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of stash entries.</returns>
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
    /// Saves current working tree changes to a new stash entry.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="message">Stash descriptive message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Signature sig = BuildSignatureOrDefault(repository);
            repository.Stashes.Add(sig, message, StashModifiers.IncludeUntracked);
        }, cancellationToken);
    }

    /// <summary>
    /// Applies a stash entry without removing it from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
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
    /// Pops a stash entry, applying changes and removing it from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to pop.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
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
    /// Drops a stash entry from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to drop.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Stashes.Remove(stashIndex);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves configured remote repositories.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of remotes.</returns>
    public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run<IReadOnlyList<RemoteInfo>>(() =>
        {
            using Repository repository = new(repositoryPath);
            return repository.Network.Remotes.Select(r => new RemoteInfo
            {
                Name = r.Name,
                Url = r.Url,
                PushUrl = r.PushUrl
            }).ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// Adds a new remote repository configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Name of the remote.</param>
    /// <param name="url">URL of the remote repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Network.Remotes.Add(remoteName, url);
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a configured remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Name of the remote to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Network.Remotes.Remove(remoteName);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetches changes from a remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name to fetch (or all remotes if null).</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            FetchRemotesInternal(repository, remoteName, credentials);
        }, cancellationToken);
    }

    /// <summary>
    /// Pulls changes from the tracked remote branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Signature sig = BuildSignatureOrDefault(repository);
            PullOptions options = new()
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = BuildCredentialsHandler(credentials)
                }
            };
            Commands.Pull(repository, sig, options);
        }, cancellationToken);
    }

    /// <summary>
    /// Pushes current branch commits to the remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            PushBranchInternal(repository, remoteName, credentials);
        }, cancellationToken);
    }

    /// <summary>
    /// Reads user.name and user.email configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitConfigInfo containing user identity settings.</returns>
    public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            string name = repository.Config.Get<string>("user.name")?.Value ?? string.Empty;
            string email = repository.Config.Get<string>("user.email")?.Value ?? string.Empty;
            return new GitConfigInfo { UserName = name, UserEmail = email };
        }, cancellationToken);
    }

    /// <summary>
    /// Updates user.name and user.email in local repository config.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="userName">Author name.</param>
    /// <param name="userEmail">Author email address.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Config.Set("user.name", userName);
            repository.Config.Set("user.email", userEmail);
        }, cancellationToken);
    }

    /// <summary>
    /// Cherry-picks a specific commit onto the current HEAD.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">SHA of the commit to cherry-pick.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                throw new InvalidOperationException($"Commit '{commitSha}' not found.");
            }

            Signature sig = BuildSignatureOrDefault(repository);
            repository.CherryPick(commit, sig);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a revert commit that inverts the changes of a specified commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">SHA of the commit to revert.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                throw new InvalidOperationException($"Commit '{commitSha}' not found.");
            }

            Signature sig = BuildSignatureOrDefault(repository);
            repository.Revert(commit, sig);
        }, cancellationToken);
    }

    /// <summary>
    /// Resets the current branch tip to a specific commit using Soft, Mixed, or Hard mode.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Target commit SHA.</param>
    /// <param name="resetType">Type of reset (Soft, Mixed, Hard).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                throw new InvalidOperationException($"Commit '{commitSha}' not found.");
            }

            ResetMode mode = MapToResetMode(resetType);
            repository.Reset(mode, commit);
        }, cancellationToken);
    }

    /// <summary>
    /// Checks whether the repository has an ongoing long-running operation (such as merge or rebase) and conflict status.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>RepositoryOperationStatus containing active operation state.</returns>
    public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            bool hasConflicts = repository.Index.Conflicts.Any();
            bool hasRebaseState = File.Exists(GetRebaseStateFilePath(repository));
            bool hasMergeHead = File.Exists(Path.Combine(repository.Info.Path, "MERGE_HEAD"));

            RepositoryOperationType operationType = ResolveOperationType(repository.Info.CurrentOperation, hasRebaseState, hasMergeHead);
            string message = BuildOperationStatusMessage(operationType, hasConflicts);

            return new RepositoryOperationStatus
            {
                CurrentOperation = operationType,
                HasConflicts = hasConflicts,
                Message = message
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Merges the specified branch into the current HEAD branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="sourceBranchName">Source branch to merge into current branch.</param>
    /// <param name="fastForwardMode">Fast-forward merge preference.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitMergeResult containing merge status and conflict indicator.</returns>
    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? sourceBranch = repository.Branches[sourceBranchName];
            if (sourceBranch == null)
            {
                throw new InvalidOperationException($"Branch '{sourceBranchName}' not found.");
            }

            Signature signature = BuildSignatureOrDefault(repository);
            MergeOptions options = new()
            {
                FastForwardStrategy = fastForwardMode == GitMergeFastForwardMode.NoFastForward
                    ? FastForwardStrategy.NoFastForward
                    : FastForwardStrategy.Default
            };

            MergeResult result = repository.Merge(sourceBranch, signature, options);
            bool hasConflicts = result.Status == MergeStatus.Conflicts;

            return new GitMergeResult
            {
                Status = result.Status.ToString(),
                HasConflicts = hasConflicts,
                CommitSha = result.Commit?.Sha,
                Message = $"Merge outcome: {result.Status}"
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Aborts an ongoing merge operation and restores HEAD to pre-merge state.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Reset(ResetMode.Hard, "HEAD");
            DeleteMergeMetadataFiles(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Completes an ongoing merge after conflicts have been resolved and staged.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitMessage">Optional custom merge commit message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            if (repository.Index.Conflicts.Any())
            {
                throw new InvalidOperationException("Cannot continue merge: unresolved conflicts remain in index.");
            }

            Signature signature = BuildSignatureOrDefault(repository);
            string message = ReadMergeMessageOrDefault(repository, commitMessage);
            repository.Commit(message, signature, signature);
            DeleteMergeMetadataFiles(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves commits between HEAD and the specified upstream commit as rebase steps for interactive editing.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="upstreamCommitSha">Base commit SHA onto which commits will be rebased.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of rebase steps in chronological order (oldest to newest).</returns>
    public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? upstream = repository.Lookup<Commit>(upstreamCommitSha);
            Commit? head = repository.Head.Tip;
            if (head == null || upstream == null)
            {
                return (IReadOnlyList<RebaseStep>)Array.Empty<RebaseStep>();
            }

            CommitFilter filter = new()
            {
                IncludeReachableFrom = head,
                ExcludeReachableFrom = upstream,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commits = repository.Commits.QueryBy(filter).ToList();
            commits.Reverse();

            var steps = commits.Select(c => new RebaseStep
            {
                Action = RebaseStepAction.Pick,
                CommitSha = c.Sha,
                ShortSha = c.Sha.Length > 7 ? c.Sha.Substring(0, 7) : c.Sha,
                OriginalMessage = c.Message.TrimEnd('\r', '\n'),
                NewMessage = c.Message.TrimEnd('\r', '\n'),
                AuthorName = c.Author.Name,
                AuthorDate = c.Author.When
            }).ToList();

            return (IReadOnlyList<RebaseStep>)steps;
        }, cancellationToken);
    }

    /// <summary>
    /// Executes an interactive rebase sequence using the provided list of rebase steps.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="upstreamCommitSha">Base commit SHA onto which commits will be rebased.</param>
    /// <param name="steps">Reordered and configured rebase steps.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if rebase completed successfully, or false if conflicts occurred requiring user resolution.</returns>
    public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? upstream = repository.Lookup<Commit>(upstreamCommitSha);
            if (upstream == null)
            {
                throw new InvalidOperationException($"Upstream commit '{upstreamCommitSha}' not found.");
            }

            string originalBranch = repository.Head.FriendlyName;
            string originalHeadSha = repository.Head.Tip?.Sha ?? string.Empty;

            repository.Reset(ResetMode.Hard, upstream);

            WerkaRebaseState state = new()
            {
                OriginalBranchName = originalBranch,
                OriginalHeadSha = originalHeadSha,
                UpstreamSha = upstreamCommitSha,
                RemainingSteps = steps.ToList()
            };

            SaveRebaseState(repository, state);
            return RunRebaseStepsSequence(repository, state);
        }, cancellationToken);
    }

    /// <summary>
    /// Aborts an in-progress rebase and restores the original branch tip.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            WerkaRebaseState? state = LoadRebaseState(repository);
            if (state != null && !string.IsNullOrWhiteSpace(state.OriginalHeadSha))
            {
                repository.Reset(ResetMode.Hard, state.OriginalHeadSha);
                DeleteRebaseState(repository);
                return;
            }

            repository.Reset(ResetMode.Hard, "HEAD");
            DeleteRebaseState(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Continues an in-progress interactive rebase after resolving current conflict.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if all remaining steps completed, or false if another conflict was encountered.</returns>
    public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            if (repository.Index.Conflicts.Any())
            {
                throw new InvalidOperationException("Cannot continue rebase: unresolved conflicts remain in index.");
            }

            StatusOptions statusOptions = new() { IncludeIgnored = false };
            if (repository.RetrieveStatus(statusOptions).IsDirty)
            {
                Signature sig = BuildSignatureOrDefault(repository);
                repository.Commit("Resolved rebase step", sig, sig);
            }

            WerkaRebaseState? state = LoadRebaseState(repository);
            if (state == null || state.RemainingSteps.Count == 0)
            {
                DeleteRebaseState(repository);
                return true;
            }

            state.RemainingSteps.RemoveAt(0);
            SaveRebaseState(repository, state);

            return RunRebaseStepsSequence(repository, state);
        }, cancellationToken);
    }

    private record WerkaRebaseState
    {
        public string OriginalBranchName { get; init; } = string.Empty;
        public string OriginalHeadSha { get; init; } = string.Empty;
        public string UpstreamSha { get; init; } = string.Empty;
        public List<RebaseStep> RemainingSteps { get; init; } = new();
    }

    private static string GetRebaseStateFilePath(Repository repo) =>
        Path.Combine(repo.Info.Path, "werka_rebase_state.json");

    private static void SaveRebaseState(Repository repo, WerkaRebaseState state)
    {
        string filePath = GetRebaseStateFilePath(repo);
        string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private static WerkaRebaseState? LoadRebaseState(Repository repo)
    {
        string filePath = GetRebaseStateFilePath(repo);
        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<WerkaRebaseState>(json);
    }

    private static void DeleteRebaseState(Repository repo)
    {
        string filePath = GetRebaseStateFilePath(repo);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static void DeleteMergeMetadataFiles(Repository repo)
    {
        DeleteFileIfExists(Path.Combine(repo.Info.Path, "MERGE_HEAD"));
        DeleteFileIfExists(Path.Combine(repo.Info.Path, "MERGE_MSG"));
        DeleteFileIfExists(Path.Combine(repo.Info.Path, "MERGE_MODE"));
    }

    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string ReadMergeMessageOrDefault(Repository repo, string? fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(fallbackMessage))
        {
            return fallbackMessage;
        }

        string mergeMsgPath = Path.Combine(repo.Info.Path, "MERGE_MSG");
        if (File.Exists(mergeMsgPath))
        {
            return File.ReadAllText(mergeMsgPath).Trim();
        }

        return "Merge commit";
    }

    private static RepositoryOperationType ResolveOperationType(CurrentOperation op, bool hasRebaseState, bool hasMergeHead)
    {
        if (op == CurrentOperation.Merge || hasMergeHead)
        {
            return RepositoryOperationType.Merge;
        }

        if (op == CurrentOperation.Rebase || op == CurrentOperation.RebaseInteractive || op == CurrentOperation.RebaseMerge || hasRebaseState)
        {
            return RepositoryOperationType.Rebase;
        }

        if (op == CurrentOperation.CherryPick)
        {
            return RepositoryOperationType.CherryPick;
        }

        if (op == CurrentOperation.Revert)
        {
            return RepositoryOperationType.Revert;
        }

        return RepositoryOperationType.None;
    }

    private static string BuildOperationStatusMessage(RepositoryOperationType operation, bool hasConflicts)
    {
        if (operation == RepositoryOperationType.None)
        {
            return string.Empty;
        }

        string opName = operation.ToString();
        return hasConflicts
            ? $"{opName} in progress - conflicts detected"
            : $"{opName} in progress";
    }

    private bool RunRebaseStepsSequence(Repository repo, WerkaRebaseState state)
    {
        while (state.RemainingSteps.Count > 0)
        {
            RebaseStep currentStep = state.RemainingSteps[0];
            bool success = ExecuteSingleRebaseStep(repo, currentStep);
            if (!success)
            {
                SaveRebaseState(repo, state);
                return false;
            }

            state.RemainingSteps.RemoveAt(0);
            SaveRebaseState(repo, state);
        }

        DeleteRebaseState(repo);
        return true;
    }

    private bool ExecuteSingleRebaseStep(Repository repo, RebaseStep step)
    {
        if (step.Action == RebaseStepAction.Drop)
        {
            return true;
        }

        Commit? commit = repo.Lookup<Commit>(step.CommitSha);
        if (commit == null)
        {
            return true;
        }

        Signature committer = BuildSignatureOrDefault(repo);
        CherryPickResult cherryPickResult = repo.CherryPick(commit, committer);

        if (cherryPickResult.Status == CherryPickStatus.Conflicts || repo.Index.Conflicts.Any())
        {
            return false;
        }

        ApplyPostCherryPickAction(repo, step, commit, committer);
        return step.Action != RebaseStepAction.Edit;
    }

    private static void ApplyPostCherryPickAction(Repository repo, RebaseStep step, Commit originalCommit, Signature committer)
    {
        if (step.Action == RebaseStepAction.Reword && step.NewMessage != step.OriginalMessage)
        {
            Commit head = repo.Head.Tip;
            repo.Commit(step.NewMessage, originalCommit.Author, committer, new CommitOptions { AmendPreviousCommit = true });
            return;
        }

        if (step.Action == RebaseStepAction.Squash)
        {
            ApplySquashCommit(repo, step, committer);
            return;
        }

        if (step.Action == RebaseStepAction.Fixup)
        {
            ApplyFixupCommit(repo, committer);
        }
    }

    private static void ApplySquashCommit(Repository repo, RebaseStep step, Signature committer)
    {
        Commit head = repo.Head.Tip;
        Commit? parent = head.Parents.FirstOrDefault();
        if (parent == null)
        {
            return;
        }

        string previousMsg = parent.Message.TrimEnd('\r', '\n');
        string stepMsg = string.IsNullOrWhiteSpace(step.NewMessage) ? step.OriginalMessage : step.NewMessage;
        string combinedMsg = $"{previousMsg}\n\n{stepMsg}";

        repo.Reset(ResetMode.Soft, parent.Sha);
        repo.Commit(combinedMsg, parent.Author, committer);
    }

    private static void ApplyFixupCommit(Repository repo, Signature committer)
    {
        Commit head = repo.Head.Tip;
        Commit? parent = head.Parents.FirstOrDefault();
        if (parent == null)
        {
            return;
        }

        string previousMsg = parent.Message.TrimEnd('\r', '\n');
        repo.Reset(ResetMode.Soft, parent.Sha);
        repo.Commit(previousMsg, parent.Author, committer);
    }

    private static ResetMode MapToResetMode(GitResetType resetType) => resetType switch
    {
        GitResetType.Soft => ResetMode.Soft,
        GitResetType.Mixed => ResetMode.Mixed,
        GitResetType.Hard => ResetMode.Hard,
        _ => ResetMode.Mixed
    };

    private static Commit ResolveBranchTarget(Repository repo, string? targetShaOrRef)
    {
        if (string.IsNullOrWhiteSpace(targetShaOrRef))
        {
            return repo.Head.Tip;
        }

        return repo.Lookup<Commit>(targetShaOrRef) ?? repo.Head.Tip;
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
            Signature sig = BuildSignatureOrDefault(repo);
            repo.ApplyTag(tagName, targetSha, sig, message);
            return;
        }

        repo.ApplyTag(tagName, targetSha);
    }

    private static Signature BuildSignatureOrDefault(Repository repo)
    {
        return repo.Config.BuildSignature(DateTimeOffset.Now) ?? new Signature("GitWerka", "gitwerka@local", DateTimeOffset.Now);
    }

    private static void FetchRemotesInternal(Repository repo, string? remoteName, GitCredentials? credentials)
    {
        var remotes = string.IsNullOrWhiteSpace(remoteName)
            ? repo.Network.Remotes.ToList()
            : repo.Network.Remotes.Where(r => r.Name == remoteName).ToList();

        FetchOptions options = new()
        {
            CredentialsProvider = BuildCredentialsHandler(credentials)
        };

        foreach (Remote remote in remotes)
        {
            Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(s => s.Specification), options, null);
        }
    }

    private static void PushBranchInternal(Repository repo, string? remoteName, GitCredentials? credentials)
    {
        Remote? remote = string.IsNullOrWhiteSpace(remoteName)
            ? (repo.Network.Remotes["origin"] ?? repo.Network.Remotes.FirstOrDefault())
            : repo.Network.Remotes[remoteName];

        if (remote != null && repo.Head != null)
        {
            PushOptions options = new()
            {
                CredentialsProvider = BuildCredentialsHandler(credentials)
            };
            repo.Network.Push(remote, repo.Head.CanonicalName, options);
        }
    }

    private static LibGit2Sharp.Handlers.CredentialsHandler? BuildCredentialsHandler(GitCredentials? credentials)
    {
        if (credentials == null)
        {
            return null;
        }

        return (url, userFromUrl, types) =>
        {
            if (credentials.Type == GitCredentialType.SshKeyPassphrase)
            {
                if (!string.IsNullOrWhiteSpace(credentials.Passphrase))
                {
                    return new UsernamePasswordCredentials
                    {
                        Username = string.IsNullOrWhiteSpace(credentials.Username) ? userFromUrl : credentials.Username,
                        Password = credentials.Passphrase
                    };
                }

                return new DefaultCredentials();
            }

            return new UsernamePasswordCredentials
            {
                Username = string.IsNullOrWhiteSpace(credentials.Username) ? userFromUrl : credentials.Username,
                Password = credentials.PasswordOrToken
            };
        };
    }

    private static Dictionary<string, List<string>> BuildReferenceMap<T>(IEnumerable<T> references)
    {
        Dictionary<string, List<string>> map = new();

        foreach (T reference in references)
        {
            string? targetSha = null;
            string? name = null;

            if (reference is Branch branch && branch.Tip != null)
            {
                targetSha = branch.Tip.Sha;
                name = branch.FriendlyName;
            }
            else if (reference is Tag tag && tag.Target != null)
            {
                targetSha = tag.Target.Sha;
                name = tag.FriendlyName;
            }

            if (!string.IsNullOrEmpty(targetSha) && !string.IsNullOrEmpty(name))
            {
                if (!map.TryGetValue(targetSha, out List<string>? list))
                {
                    list = new List<string>();
                    map[targetSha] = list;
                }

                list.Add(name);
            }
        }

        return map;
    }

    private static CommitSummary ProcessCommitNode(
        Commit commit,
        Dictionary<int, string> reservedLanes,
        Dictionary<string, List<string>> branchMap,
        Dictionary<string, List<string>> tagMap)
    {
        List<int> reservedLanesForCommit = GetReservedLanesForHash(reservedLanes, commit.Sha);

        int lane = reservedLanesForCommit.Count == 0 ? NextAvailableLane(reservedLanes, 0) : reservedLanesForCommit[0];

        List<int> forkingOffLanes = reservedLanesForCommit.Skip(1).ToList();
        List<int> passingLanes = reservedLanes.Keys.Except(reservedLanesForCommit).ToList();
        List<int> mergingLanes = new();

        if (commit.Parents.Any())
        {
            reservedLanes[lane] = commit.Parents.First().Sha;

            foreach (Commit parent in commit.Parents.Skip(1))
            {
                int parentLane = ReserveParentLane(reservedLanes, parent.Sha, lane);
                mergingLanes.Add(parentLane);
            }
        }

        branchMap.TryGetValue(commit.Sha, out List<string>? branches);
        tagMap.TryGetValue(commit.Sha, out List<string>? tags);

        return new CommitSummary
        {
            Sha = commit.Sha,
            ShortSha = commit.Sha.Substring(0, Math.Min(7, commit.Sha.Length)),
            MessageShort = commit.MessageShort,
            Message = commit.Message.TrimEnd('\r', '\n'),
            AuthorName = commit.Author.Name,
            AuthorEmail = commit.Author.Email,
            AuthorDate = commit.Author.When,
            ParentShas = commit.Parents.Select(p => p.Sha).ToImmutableList(),
            BranchNames = (branches ?? new List<string>()).ToImmutableList(),
            TagNames = (tags ?? new List<string>()).ToImmutableList(),
            Lane = lane,
            PassingLanes = passingLanes.ToImmutableList(),
            MergingLanes = mergingLanes.ToImmutableList(),
            ForkingOffLanes = forkingOffLanes.ToImmutableList()
        };
    }

    private static List<int> GetReservedLanesForHash(Dictionary<int, string> reservedLanes, string hash)
    {
        List<int> lanes = reservedLanes
            .Where(pair => pair.Value == hash)
            .Select(pair => pair.Key)
            .OrderBy(l => l)
            .ToList();

        foreach (int lane in lanes)
        {
            reservedLanes.Remove(lane);
        }

        return lanes;
    }

    private static int ReserveParentLane(Dictionary<int, string> reservedLanes, string parentSha, int currentLane)
    {
        var existing = reservedLanes.FirstOrDefault(pair => pair.Value == parentSha);
        if (!existing.Equals(default(KeyValuePair<int, string>)))
        {
            return existing.Key;
        }

        int newLane = NextAvailableLane(reservedLanes, currentLane + 1);
        reservedLanes[newLane] = parentSha;
        return newLane;
    }

    private static int NextAvailableLane(Dictionary<int, string> reservedLanes, int minLane)
    {
        int lane = minLane;
        while (reservedLanes.ContainsKey(lane))
        {
            lane++;
        }

        return lane;
    }

    private static FileDiffSummary MapToFileDiffSummary(TreeEntryChanges change, Repository repo, Tree? oldTree, Tree newTree)
    {
        FileChangeStatus status = change.Status switch
        {
            ChangeKind.Added => FileChangeStatus.Added,
            ChangeKind.Deleted => FileChangeStatus.Deleted,
            ChangeKind.Modified => FileChangeStatus.Modified,
            ChangeKind.Renamed => FileChangeStatus.Renamed,
            ChangeKind.Copied => FileChangeStatus.Copied,
            ChangeKind.Ignored => FileChangeStatus.Ignored,
            _ => FileChangeStatus.Untracked
        };

        Patch patch = repo.Diff.Compare<Patch>(oldTree, newTree, new[] { change.Path });
        PatchEntryChanges? entry = patch[change.Path];

        return new FileDiffSummary
        {
            Path = change.Path,
            OldPath = change.OldPath != change.Path ? change.OldPath : null,
            Status = status,
            LinesAdded = entry?.LinesAdded ?? 0,
            LinesDeleted = entry?.LinesDeleted ?? 0
        };
    }

    private static IReadOnlyList<DiffLine> ParsePatchLines(string patchText)
    {
        if (string.IsNullOrEmpty(patchText))
        {
            return Array.Empty<DiffLine>();
        }

        string[] lines = patchText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        List<DiffLine> result = new();
        int oldLine = 1;
        int newLine = 1;

        foreach (string line in lines)
        {
            if (line.StartsWith("@@"))
            {
                result.Add(new DiffLine { Type = DiffLineType.HunkHeader, Content = line });
                ParseHunkHeaderLineNumbers(line, ref oldLine, ref newLine);
            }
            else if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                result.Add(new DiffLine { Type = DiffLineType.Addition, Content = line.Substring(1), NewLineNumber = newLine++ });
            }
            else if (line.StartsWith("-") && !line.StartsWith("---"))
            {
                result.Add(new DiffLine { Type = DiffLineType.Deletion, Content = line.Substring(1), OldLineNumber = oldLine++ });
            }
            else if (line.StartsWith(" "))
            {
                result.Add(new DiffLine { Type = DiffLineType.Context, Content = line.Substring(1), OldLineNumber = oldLine++, NewLineNumber = newLine++ });
            }
        }

        return result;
    }

    private static void ParseHunkHeaderLineNumbers(string line, ref int oldLine, ref int newLine)
    {
        Match match = Regex.Match(line, @"@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@");
        if (match.Success)
        {
            int.TryParse(match.Groups[1].Value, out oldLine);
            int.TryParse(match.Groups[2].Value, out newLine);
        }
    }

    private static void AppendWorkingTreeEntries(StatusEntry entry, List<WorkingTreeChange> changes)
    {
        if (entry.State.HasFlag(FileStatus.Conflicted))
        {
            changes.Add(new WorkingTreeChange
            {
                Path = entry.FilePath,
                Status = FileChangeStatus.Conflicted,
                IsStaged = false
            });
            return;
        }

        if (entry.State.HasFlag(FileStatus.ModifiedInIndex) || entry.State.HasFlag(FileStatus.NewInIndex) || entry.State.HasFlag(FileStatus.DeletedFromIndex) || entry.State.HasFlag(FileStatus.RenamedInIndex))
        {
            changes.Add(new WorkingTreeChange
            {
                Path = entry.FilePath,
                Status = ResolveIndexStatus(entry.State),
                IsStaged = true
            });
        }

        if (entry.State.HasFlag(FileStatus.ModifiedInWorkdir) || entry.State.HasFlag(FileStatus.NewInWorkdir) || entry.State.HasFlag(FileStatus.DeletedFromWorkdir) || entry.State.HasFlag(FileStatus.RenamedInWorkdir))
        {
            changes.Add(new WorkingTreeChange
            {
                Path = entry.FilePath,
                Status = ResolveWorkdirStatus(entry.State),
                IsStaged = false
            });
        }
    }

    private static FileChangeStatus ResolveIndexStatus(FileStatus state)
    {
        if (state.HasFlag(FileStatus.NewInIndex))
        {
            return FileChangeStatus.Added;
        }

        if (state.HasFlag(FileStatus.DeletedFromIndex))
        {
            return FileChangeStatus.Deleted;
        }

        if (state.HasFlag(FileStatus.RenamedInIndex))
        {
            return FileChangeStatus.Renamed;
        }

        return FileChangeStatus.Modified;
    }

    private static FileChangeStatus ResolveWorkdirStatus(FileStatus state)
    {
        if (state.HasFlag(FileStatus.NewInWorkdir))
        {
            return FileChangeStatus.Untracked;
        }

        if (state.HasFlag(FileStatus.DeletedFromWorkdir))
        {
            return FileChangeStatus.Deleted;
        }

        if (state.HasFlag(FileStatus.RenamedInWorkdir))
        {
            return FileChangeStatus.Renamed;
        }

        return FileChangeStatus.Modified;
    }

    private static void ApplyGraphWidth(List<CommitSummary> summaries)
    {
        int maxLane = 0;
        foreach (CommitSummary summary in summaries)
        {
            maxLane = Math.Max(maxLane, summary.Lane);
            maxLane = UpdateMaxLaneFromCollection(maxLane, summary.PassingLanes);
            maxLane = UpdateMaxLaneFromCollection(maxLane, summary.MergingLanes);
            maxLane = UpdateMaxLaneFromCollection(maxLane, summary.ForkingOffLanes);
        }

        double graphWidth = CalculateGraphWidth(maxLane);
        for (int i = 0; i < summaries.Count; i++)
        {
            summaries[i] = summaries[i] with { GraphWidth = graphWidth };
        }
    }

    private static int UpdateMaxLaneFromCollection(int currentMax, IReadOnlyList<int>? lanes)
    {
        if (lanes == null)
        {
            return currentMax;
        }

        int max = currentMax;
        foreach (int lane in lanes)
        {
            if (lane > max)
            {
                max = lane;
            }
        }

        return max;
    }

    private static double CalculateGraphWidth(int maxLane)
    {
        return Math.Max(80.0, 10.0 + ((maxLane + 1) * 16.0) + 12.0);
    }

    private static List<Commit> BuildReachableCommitList(Repository repository, int maxBranches)
    {
        HashSet<string> includedShas = new(StringComparer.OrdinalIgnoreCase);
        List<Commit> commits = new();

        AddHeadCommitIfPresent(repository, commits, includedShas);
        AddRecentBranchTips(repository, commits, includedShas, maxBranches);
        AddFallbackTagCommitsIfEmpty(repository, commits, includedShas, maxBranches);

        return commits;
    }

    private static void AddHeadCommitIfPresent(Repository repository, List<Commit> commits, HashSet<string> includedShas)
    {
        Commit? headTip = repository.Head?.Tip;
        if (headTip != null && includedShas.Add(headTip.Sha))
        {
            commits.Add(headTip);
        }
    }

    private static void AddRecentBranchTips(
        Repository repository,
        List<Commit> commits,
        HashSet<string> includedShas,
        int maxBranches)
    {
        var sortedBranches = repository.Branches
            .Where(b => b.Tip != null)
            .OrderByDescending(GetBranchActivityDate);

        foreach (Branch branch in sortedBranches)
        {
            if (commits.Count >= maxBranches)
            {
                break;
            }

            TryAddBranchTip(branch, commits, includedShas);
        }
    }

    private static void TryAddBranchTip(Branch branch, List<Commit> commits, HashSet<string> includedShas)
    {
        Commit? tip = branch.Tip;
        if (tip != null && includedShas.Add(tip.Sha))
        {
            commits.Add(tip);
        }
    }

    private static DateTimeOffset GetBranchActivityDate(Branch branch)
    {
        if (branch.Tip == null)
        {
            return DateTimeOffset.MinValue;
        }

        return branch.Tip.Committer?.When ?? branch.Tip.Author?.When ?? DateTimeOffset.MinValue;
    }

    private static void AddFallbackTagCommitsIfEmpty(
        Repository repository,
        List<Commit> commits,
        HashSet<string> includedShas,
        int maxBranches)
    {
        if (commits.Count > 0)
        {
            return;
        }

        foreach (Tag tag in repository.Tags)
        {
            if (commits.Count >= maxBranches)
            {
                break;
            }

            Commit? targetCommit = ResolveTagCommit(tag);
            if (targetCommit != null && includedShas.Add(targetCommit.Sha))
            {
                commits.Add(targetCommit);
            }
        }
    }

    private static Commit? ResolveTagCommit(Tag tag)
    {
        if (tag.Target is Commit directCommit)
        {
            return directCommit;
        }

        return tag.Annotation?.Target as Commit;
    }

    private static CommitSummary CreateWipCommitSummary(Repository repository, int uncommittedCount)
    {
        string headSha = repository.Head.Tip?.Sha ?? string.Empty;
        var parentShas = string.IsNullOrEmpty(headSha)
            ? ImmutableList<string>.Empty
            : ImmutableList.Create(headSha);

        return new CommitSummary
        {
            Sha = "WIP",
            ShortSha = string.Empty,
            MessageShort = "// WIP",
            Message = "// WIP",
            AuthorName = string.Empty,
            AuthorEmail = string.Empty,
            AuthorDate = DateTimeOffset.Now,
            ParentShas = parentShas,
            BranchNames = ImmutableList<string>.Empty,
            TagNames = ImmutableList<string>.Empty,
            Lane = 0,
            PassingLanes = ImmutableList<int>.Empty,
            MergingLanes = ImmutableList<int>.Empty,
            ForkingOffLanes = ImmutableList<int>.Empty,
            IsWip = true,
            UncommittedFilesCount = uncommittedCount
        };
    }

    private static IReadOnlyList<DiffLine> GetWorkingTreeFileDiffLines(Repository repository, string repositoryPath, string filePath)
    {
        Tree? headTree = repository.Head.Tip?.Tree;
        CompareOptions options = new() { ContextLines = 3 };
        Patch patch = repository.Diff.Compare<Patch>(headTree, DiffTargets.WorkingDirectory | DiffTargets.Index, new[] { filePath }, null, options);
        PatchEntryChanges? entry = patch[filePath];

        if (entry != null)
        {
            return ParsePatchLines(entry.Patch);
        }

        string fullPath = Path.Combine(repository.Info.WorkingDirectory ?? repositoryPath, filePath);
        if (File.Exists(fullPath))
        {
            return ReadUntrackedFileAsAdditions(fullPath);
        }

        return Array.Empty<DiffLine>();
    }

    private static IReadOnlyList<DiffLine> ReadUntrackedFileAsAdditions(string fullPath)
    {
        string[] lines = File.ReadAllLines(fullPath);
        List<DiffLine> result = new();
        for (int i = 0; i < lines.Length; i++)
        {
            result.Add(new DiffLine
            {
                Type = DiffLineType.Addition,
                Content = "+" + lines[i],
                NewLineNumber = i + 1
            });
        }
        return result;
    }

    /// <summary>
    /// Retrieves the conflicted file data, Git metadata (local/remote SHAs and branches), and parsed conflict hunks.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ConflictFileModel containing metadata and parsed chunks.</returns>
    public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            string fullPath = Path.Combine(repository.Info.WorkingDirectory ?? repositoryPath, filePath);
            string fileName = Path.GetFileName(filePath);

            string oursSha = repository.Head.Tip != null
                ? repository.Head.Tip.Sha.Substring(0, Math.Min(7, repository.Head.Tip.Sha.Length))
                : "HEAD";
            string oursBranch = repository.Head.FriendlyName ?? "HEAD";

            string theirsSha = ResolveTheirsSha(repository);
            string theirsBranch = ResolveTheirsBranch(repository);

            string[] fileLines = File.Exists(fullPath)
                ? File.ReadAllLines(fullPath)
                : Array.Empty<string>();

            var chunks = ParseConflictChunks(fileLines, repository, filePath, out int totalConflicts);

            return new ConflictFileModel
            {
                FilePath = filePath,
                FileName = fileName,
                TotalConflicts = totalConflicts,
                OursCommitSha = oursSha,
                OursBranch = oursBranch,
                TheirsCommitSha = theirsSha,
                TheirsBranch = theirsBranch,
                Chunks = chunks
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Writes resolved file content to disk and stages the file in the Git index, clearing its conflict state.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="resolvedContent">The full resolved text content to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            string fullPath = Path.Combine(repositoryPath, filePath);
            File.WriteAllText(fullPath, resolvedContent);

            using Repository repository = new(repositoryPath);
            Commands.Stage(repository, filePath);
        }, cancellationToken);
    }

    /// <summary>
    /// Marks all currently conflicted files as resolved by staging them in the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            var conflictPaths = repository.Index.Conflicts
                .Select(c => c.Ours?.Path ?? c.Theirs?.Path ?? c.Ancestor?.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            foreach (string? path in conflictPaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Commands.Stage(repository, path);
                }
            }
        }, cancellationToken);
    }

    private static string ResolveTheirsSha(Repository repository)
    {
        string mergeHeadPath = Path.Combine(repository.Info.Path, "MERGE_HEAD");
        if (File.Exists(mergeHeadPath))
        {
            string sha = File.ReadAllText(mergeHeadPath).Trim();
            return sha.Substring(0, Math.Min(7, sha.Length));
        }

        return "incoming";
    }

    private static string ResolveTheirsBranch(Repository repository)
    {
        string mergeMsgPath = Path.Combine(repository.Info.Path, "MERGE_MSG");
        if (File.Exists(mergeMsgPath))
        {
            string content = File.ReadAllText(mergeMsgPath);
            Match match = Regex.Match(content, @"(?:branch|remote-tracking branch)\s+'([^']+)'");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return "remote";
    }

    private static ImmutableList<ConflictFileChunk> ParseConflictChunks(string[] lines, Repository repository, string filePath, out int totalConflicts)
    {
        List<ConflictFileChunk> chunks = new();
        List<string> commonLines = new();
        List<string> oursLines = new();
        List<string> theirsLines = new();
        List<string> baseLines = new();
        int conflictCount = 0;
        int state = 0;

        foreach (string line in lines)
        {
            state = ProcessConflictChunkLine(line, state, commonLines, oursLines, theirsLines, baseLines, chunks, ref conflictCount);
        }

        if (commonLines.Count > 0)
        {
            chunks.Add(new ConflictFileChunk
            {
                IsConflict = false,
                CommonLines = commonLines.ToImmutableList()
            });
        }

        totalConflicts = conflictCount;
        if (totalConflicts == 0)
        {
            return FallbackChunksFromBlobs(repository, filePath, lines, out totalConflicts);
        }

        return chunks.ToImmutableList();
    }

    private static int ProcessConflictChunkLine(
        string line,
        int state,
        List<string> common,
        List<string> ours,
        List<string> theirs,
        List<string> baseLines,
        List<ConflictFileChunk> chunks,
        ref int conflictCount)
    {
        if (state == 0)
        {
            return HandleNormalLine(line, common, chunks);
        }

        if (state == 1)
        {
            return HandleOursLine(line, ours);
        }

        if (state == 2)
        {
            return HandleBaseLine(line, baseLines);
        }

        return HandleTheirsLine(line, ours, theirs, baseLines, chunks, ref conflictCount);
    }

    private static int HandleNormalLine(string line, List<string> common, List<ConflictFileChunk> chunks)
    {
        if (line.StartsWith("<<<<<<<"))
        {
            if (common.Count > 0)
            {
                chunks.Add(new ConflictFileChunk
                {
                    IsConflict = false,
                    CommonLines = common.ToImmutableList()
                });
                common.Clear();
            }

            return 1;
        }

        common.Add(line);
        return 0;
    }

    private static int HandleOursLine(string line, List<string> ours)
    {
        if (line.StartsWith("|||||||"))
        {
            return 2;
        }

        if (line.StartsWith("======="))
        {
            return 3;
        }

        ours.Add(line);
        return 1;
    }

    private static int HandleBaseLine(string line, List<string> baseLines)
    {
        if (line.StartsWith("======="))
        {
            return 3;
        }

        baseLines.Add(line);
        return 2;
    }

    private static int HandleTheirsLine(
        string line,
        List<string> ours,
        List<string> theirs,
        List<string> baseLines,
        List<ConflictFileChunk> chunks,
        ref int conflictCount)
    {
        if (line.StartsWith(">>>>>>>"))
        {
            conflictCount++;
            chunks.Add(new ConflictFileChunk
            {
                IsConflict = true,
                HunkIndex = conflictCount,
                OursLines = ours.ToImmutableList(),
                TheirsLines = theirs.ToImmutableList(),
                BaseLines = baseLines.ToImmutableList(),
                IsOursSelected = false,
                IsTheirsSelected = false
            });

            ours.Clear();
            theirs.Clear();
            baseLines.Clear();
            return 0;
        }

        theirs.Add(line);
        return 3;
    }

    private static ImmutableList<ConflictFileChunk> FallbackChunksFromBlobs(
        Repository repository,
        string filePath,
        string[] fileLines,
        out int totalConflicts)
    {
        Conflict? conflict = repository.Index.Conflicts[filePath];
        if (conflict == null)
        {
            totalConflicts = 0;
            return ImmutableList.Create(new ConflictFileChunk
            {
                IsConflict = false,
                CommonLines = fileLines.ToImmutableList()
            });
        }

        string oursText = ResolveBlobText(repository, conflict.Ours?.Id);
        string theirsText = ResolveBlobText(repository, conflict.Theirs?.Id);

        totalConflicts = 1;
        return ImmutableList.Create(new ConflictFileChunk
        {
            IsConflict = true,
            HunkIndex = 1,
            OursLines = oursText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToImmutableList(),
            TheirsLines = theirsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToImmutableList(),
            IsOursSelected = false,
            IsTheirsSelected = false
        });
    }

    private static string ResolveBlobText(Repository repository, ObjectId? blobId)
    {
        if (blobId == null)
        {
            return string.Empty;
        }

        Blob? blob = repository.Lookup<Blob>(blobId);
        if (blob == null)
        {
            return string.Empty;
        }

        return blob.GetContentText();
    }
}
