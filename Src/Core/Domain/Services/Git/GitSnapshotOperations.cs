namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for creating repository state snapshots to detect changes in commits, working tree, branches, and remotes.
/// </summary>
public class GitSnapshotOperations
{
    /// <summary>
    /// Computes a lightweight snapshot of repository refs, working tree status, and remotes.
    /// Usages: GitService.
    /// </summary>
    public Task<GitRepositoryStateSnapshot> GetRepositoryStateSnapshotAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(repositoryPath) || !Directory.Exists(repositoryPath) || !Repository.IsValid(repositoryPath))
            {
                return new GitRepositoryStateSnapshot();
            }

            using Repository repository = new(repositoryPath);

            string headSha = repository.Head?.Tip?.Sha ?? string.Empty;
            bool isHeadDetached = repository.Info.IsHeadDetached;
            string currentBranch = isHeadDetached ? string.Empty : (repository.Head?.FriendlyName ?? string.Empty);

            var branchSignatures = BuildBranchSignatures(repository);
            var remoteSignatures = BuildRemoteSignatures(repository);
            var workingTreeSignatures = BuildWorkingTreeSignatures(repository);
            var tagSignatures = BuildTagSignatures(repository);
            var stashSignatures = BuildStashSignatures(repository);
            var (operationType, hasConflicts) = ResolveOperationStatus(repository);

            return new GitRepositoryStateSnapshot
            {
                HeadCommitSha = headSha,
                CurrentBranch = currentBranch,
                IsHeadDetached = isHeadDetached,
                CurrentOperation = operationType,
                HasConflicts = hasConflicts,
                BranchSignatures = branchSignatures,
                RemoteSignatures = remoteSignatures,
                WorkingTreeSignatures = workingTreeSignatures,
                TagSignatures = tagSignatures,
                StashSignatures = stashSignatures
            };
        }, cancellationToken);
    }

    private static ImmutableList<string> BuildBranchSignatures(Repository repository)
    {
        return repository.Branches
            .Select(b => $"{b.FriendlyName}:{b.Tip?.Sha}:{b.IsRemote}:{b.IsCurrentRepositoryHead}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToImmutableList();
    }

    private static ImmutableList<string> BuildRemoteSignatures(Repository repository)
    {
        return repository.Network.Remotes
            .Select(r => $"{r.Name}:{r.Url}:{r.PushUrl}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToImmutableList();
    }

    private static ImmutableList<string> BuildTagSignatures(Repository repository)
    {
        return repository.Tags
            .Select(t => $"{t.FriendlyName}:{t.Target?.Sha}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToImmutableList();
    }

    private static ImmutableList<string> BuildStashSignatures(Repository repository)
    {
        return repository.Stashes
            .Select((s, index) => $"{index}:{s.WorkTree?.Sha}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToImmutableList();
    }

    private static ImmutableList<string> BuildWorkingTreeSignatures(Repository repository)
    {
        string workingDirectory = repository.Info.WorkingDirectory ?? string.Empty;
        RepositoryStatus status = repository.RetrieveStatus(new StatusOptions());
        List<string> signatures = new();

        foreach (StatusEntry entry in status)
        {
            if (entry.State == FileStatus.Ignored || entry.State == FileStatus.Unaltered)
            {
                continue;
            }

            signatures.Add(BuildStatusEntrySignature(workingDirectory, entry));
        }

        signatures.Sort(StringComparer.Ordinal);
        return signatures.ToImmutableList();
    }

    private static string BuildStatusEntrySignature(string workingDirectory, StatusEntry entry)
    {
        string fullPath = Path.Combine(workingDirectory, entry.FilePath);
        long lastWrite = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath).Ticks : 0;
        long length = File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
        return $"{entry.FilePath}:{(int)entry.State}:{lastWrite}:{length}";
    }

    private static (RepositoryOperationType OperationType, bool HasConflicts) ResolveOperationStatus(Repository repository)
    {
        CurrentOperation op = repository.Info.CurrentOperation;
        bool hasRebaseState = File.Exists(Path.Combine(repository.Info.Path, "rebase-merge", "git-rebase-todo"))
            || File.Exists(Path.Combine(repository.Info.Path, "rebase-apply", "0001"));
        bool hasMergeHead = File.Exists(Path.Combine(repository.Info.Path, "MERGE_HEAD"));
        bool hasConflicts = repository.Index.Conflicts.Any();

        RepositoryOperationType operationType = op switch
        {
            CurrentOperation.Merge => RepositoryOperationType.Merge,
            CurrentOperation.Rebase or CurrentOperation.RebaseInteractive or CurrentOperation.RebaseMerge => RepositoryOperationType.Rebase,
            CurrentOperation.CherryPick or CurrentOperation.CherryPickSequence => RepositoryOperationType.CherryPick,
            CurrentOperation.Revert or CurrentOperation.RevertSequence => RepositoryOperationType.Revert,
            _ when hasMergeHead => RepositoryOperationType.Merge,
            _ when hasRebaseState => RepositoryOperationType.Rebase,
            _ => RepositoryOperationType.None
        };

        return (operationType, hasConflicts);
    }
}
