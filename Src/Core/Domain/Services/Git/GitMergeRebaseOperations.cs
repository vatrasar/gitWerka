namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git branch merging and interactive rebasing workflows.
/// </summary>
public class GitMergeRebaseOperations
{
    private record WerkaRebaseState
    {
        public string OriginalBranchName { get; init; } = string.Empty;
        public string OriginalHeadSha { get; init; } = string.Empty;
        public string UpstreamSha { get; init; } = string.Empty;
        public List<RebaseStep> RemainingSteps { get; init; } = new();
    }

    /// <summary>
    /// Checks the current repository operation status (Merge, Rebase, CherryPick, Revert, or None) and any conflict states.
    /// </summary>
    public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            CurrentOperation op = repository.Info.CurrentOperation;
            bool hasRebaseState = File.Exists(GetRebaseStateFilePath(repository));
            bool hasMergeHead = File.Exists(Path.Combine(repository.Info.Path, "MERGE_HEAD"));
            bool hasConflicts = repository.Index.Conflicts.Any();

            RepositoryOperationType operationType = ResolveOperationType(op, hasRebaseState, hasMergeHead);
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
    /// Merges the specified branch into the current HEAD branch with the chosen fast-forward strategy.
    /// </summary>
    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Branch? sourceBranch = repository.Branches[sourceBranchName]
                ?? repository.Branches.FirstOrDefault(b => b.FriendlyName == sourceBranchName);

            if (sourceBranch == null)
            {
                throw new InvalidOperationException($"Source branch '{sourceBranchName}' not found.");
            }

            MergeOptions mergeOptions = new()
            {
                FastForwardStrategy = fastForwardMode == GitMergeFastForwardMode.NoFastForward
                    ? FastForwardStrategy.NoFastForward
                    : FastForwardStrategy.Default
            };

            Signature merger = GitServiceHelper.BuildSignatureOrDefault(repository);
            MergeResult mergeResult = repository.Merge(sourceBranch, merger, mergeOptions);

            bool hasConflicts = mergeResult.Status == MergeStatus.Conflicts || repository.Index.Conflicts.Any();
            string? commitSha = mergeResult.Commit?.Sha;

            return new GitMergeResult
            {
                Status = mergeResult.Status.ToString(),
                HasConflicts = hasConflicts,
                CommitSha = commitSha,
                Message = $"Merge outcome: {mergeResult.Status}"
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Aborts an ongoing merge operation and resets the working tree and index to HEAD.
    /// </summary>
    public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            repository.Reset(ResetMode.Hard);
            DeleteMergeMetadataFiles(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Completes an in-progress merge after conflicts have been resolved by creating the merge commit.
    /// </summary>
    public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            if (repository.Index.Conflicts.Any())
            {
                throw new InvalidOperationException("Cannot continue merge while conflicts remain unresolved.");
            }

            Signature merger = GitServiceHelper.BuildSignatureOrDefault(repository);
            string finalMessage = ReadMergeMessageOrDefault(repository, commitMessage);

            repository.Commit(finalMessage, merger, merger);
            DeleteMergeMetadataFiles(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves the ordered list of commits from upstream to HEAD for interactive rebasing.
    /// </summary>
    public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            Commit? upstream = repository.Lookup<Commit>(upstreamCommitSha);
            Commit? head = repository.Head.Tip;

            if (upstream == null || head == null)
            {
                return (IReadOnlyList<RebaseStep>)Array.Empty<RebaseStep>();
            }

            ICommitLog commitRange = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = head,
                ExcludeReachableFrom = upstream,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            });

            List<RebaseStep> steps = new();
            foreach (Commit commit in commitRange)
            {
                steps.Add(new RebaseStep
                {
                    CommitSha = commit.Sha,
                    ShortSha = commit.Sha.Substring(0, Math.Min(7, commit.Sha.Length)),
                    OriginalMessage = commit.Message.TrimEnd('\r', '\n'),
                    NewMessage = commit.Message.TrimEnd('\r', '\n'),
                    AuthorName = commit.Author.Name,
                    Action = RebaseStepAction.Pick
                });
            }

            return (IReadOnlyList<RebaseStep>)steps;
        }, cancellationToken);
    }

    /// <summary>
    /// Executes an interactive rebase with the specified ordered steps.
    /// </summary>
    public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            string originalBranchName = repository.Head.FriendlyName;
            string originalHeadSha = repository.Head.Tip?.Sha ?? string.Empty;

            Commit upstream = repository.Lookup<Commit>(upstreamCommitSha)
                ?? throw new InvalidOperationException($"Upstream commit '{upstreamCommitSha}' not found.");

            repository.Reset(ResetMode.Hard, upstream);

            WerkaRebaseState state = new()
            {
                OriginalBranchName = originalBranchName,
                OriginalHeadSha = originalHeadSha,
                UpstreamSha = upstreamCommitSha,
                RemainingSteps = steps.ToList()
            };

            return RunRebaseStepsSequence(repository, state);
        }, cancellationToken);
    }

    /// <summary>
    /// Aborts an ongoing interactive rebase and restores the original branch and HEAD pointer.
    /// </summary>
    public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            WerkaRebaseState? state = LoadRebaseState(repository);

            if (state != null && !string.IsNullOrEmpty(state.OriginalHeadSha))
            {
                Commit originalHead = repository.Lookup<Commit>(state.OriginalHeadSha);
                if (originalHead != null)
                {
                    repository.Reset(ResetMode.Hard, originalHead);
                }
            }
            else
            {
                repository.Reset(ResetMode.Hard);
            }

            DeleteRebaseState(repository);
        }, cancellationToken);
    }

    /// <summary>
    /// Continues an interrupted interactive rebase after conflict resolution or commit editing.
    /// </summary>
    public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            WerkaRebaseState? state = LoadRebaseState(repository);
            if (state == null || state.RemainingSteps.Count == 0)
            {
                DeleteRebaseState(repository);
                return true;
            }

            if (repository.Index.Conflicts.Any())
            {
                throw new InvalidOperationException("Cannot continue rebase while conflicts remain unresolved.");
            }

            state.RemainingSteps.RemoveAt(0);
            return RunRebaseStepsSequence(repository, state);
        }, cancellationToken);
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

        Signature committer = GitServiceHelper.BuildSignatureOrDefault(repo);
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
}
