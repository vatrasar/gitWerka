namespace GitWerka.Core.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Helper for calculating diff summaries and line changes for uncommitted working tree modifications.
/// Usages: GitService.
/// </summary>
internal static class WorkingTreeDiffCalculator
{
    public static CommitDetail CreateWipCommitDetail(Repository repository, string repositoryPath)
    {
        string headSha = repository.Head.Tip?.Sha ?? string.Empty;
        var parentShas = string.IsNullOrEmpty(headSha)
            ? ImmutableList<string>.Empty
            : ImmutableList.Create(headSha);

        IReadOnlyList<FileDiffSummary> changedFiles = CalculateWorkingTreeDiffSummaries(repository, repositoryPath);

        return new CommitDetail
        {
            Sha = "WIP",
            ShortSha = string.Empty,
            Message = "Uncommitted changes in working directory",
            AuthorName = "Working Directory",
            AuthorEmail = string.Empty,
            AuthorDate = DateTimeOffset.Now,
            CommitterName = "Working Directory",
            CommitterEmail = string.Empty,
            CommitterDate = DateTimeOffset.Now,
            ParentShas = parentShas,
            ChangedFiles = changedFiles.ToImmutableList()
        };
    }

    public static IReadOnlyList<FileDiffSummary> CalculateWorkingTreeDiffSummaries(Repository repository, string repositoryPath)
    {
        Tree? headTree = repository.Head.Tip?.Tree;
        CompareOptions options = new() { ContextLines = 3 };
        Patch patch = repository.Diff.Compare<Patch>(headTree, DiffTargets.WorkingDirectory | DiffTargets.Index, null, null, options);
        Dictionary<string, PatchEntryChanges> patchMap = BuildPatchMap(patch);

        StatusOptions statusOptions = new() { IncludeIgnored = false };
        RepositoryStatus status = repository.RetrieveStatus(statusOptions);

        List<FileDiffSummary> summaries = new();
        HashSet<string> processedPaths = new(StringComparer.Ordinal);

        foreach (StatusEntry entry in status)
        {
            ProcessStatusEntry(entry, patchMap, repository, repositoryPath, processedPaths, summaries);
        }

        foreach (Conflict conflict in repository.Index.Conflicts)
        {
            ProcessConflictEntry(conflict, patchMap, processedPaths, summaries);
        }

        foreach (PatchEntryChanges patchEntry in patch)
        {
            ProcessPatchEntry(patchEntry, processedPaths, summaries);
        }

        return summaries;
    }

    private static Dictionary<string, PatchEntryChanges> BuildPatchMap(Patch patch)
    {
        Dictionary<string, PatchEntryChanges> patchMap = new(StringComparer.Ordinal);
        foreach (PatchEntryChanges p in patch)
        {
            patchMap[p.Path] = p;
        }

        return patchMap;
    }

    private static void ProcessStatusEntry(
        StatusEntry entry,
        Dictionary<string, PatchEntryChanges> patchMap,
        Repository repository,
        string repositoryPath,
        HashSet<string> processedPaths,
        List<FileDiffSummary> summaries)
    {
        if (processedPaths.Contains(entry.FilePath))
        {
            return;
        }

        processedPaths.Add(entry.FilePath);
        summaries.Add(BuildSummaryFromStatusEntry(entry, patchMap, repository, repositoryPath));
    }

    private static void ProcessConflictEntry(
        Conflict conflict,
        Dictionary<string, PatchEntryChanges> patchMap,
        HashSet<string> processedPaths,
        List<FileDiffSummary> summaries)
    {
        string? conflictPath = conflict.Ours?.Path ?? conflict.Theirs?.Path ?? conflict.Ancestor?.Path;
        if (string.IsNullOrEmpty(conflictPath) || processedPaths.Contains(conflictPath))
        {
            return;
        }

        processedPaths.Add(conflictPath);
        patchMap.TryGetValue(conflictPath, out PatchEntryChanges? patchEntry);
        summaries.Add(new FileDiffSummary
        {
            Path = conflictPath,
            OldPath = null,
            Status = FileChangeStatus.Conflicted,
            LinesAdded = patchEntry?.LinesAdded ?? 0,
            LinesDeleted = patchEntry?.LinesDeleted ?? 0
        });
    }

    private static void ProcessPatchEntry(
        PatchEntryChanges patchEntry,
        HashSet<string> processedPaths,
        List<FileDiffSummary> summaries)
    {
        if (processedPaths.Contains(patchEntry.Path))
        {
            return;
        }

        processedPaths.Add(patchEntry.Path);
        summaries.Add(new FileDiffSummary
        {
            Path = patchEntry.Path,
            OldPath = patchEntry.OldPath != patchEntry.Path ? patchEntry.OldPath : null,
            Status = MapChangeKind(patchEntry.Status),
            LinesAdded = patchEntry.LinesAdded,
            LinesDeleted = patchEntry.LinesDeleted
        });
    }

    private static FileDiffSummary BuildSummaryFromStatusEntry(
        StatusEntry entry,
        Dictionary<string, PatchEntryChanges> patchMap,
        Repository repository,
        string repositoryPath)
    {
        if (patchMap.TryGetValue(entry.FilePath, out PatchEntryChanges? patchEntry))
        {
            return new FileDiffSummary
            {
                Path = entry.FilePath,
                OldPath = patchEntry.OldPath != patchEntry.Path ? patchEntry.OldPath : null,
                Status = ResolveStatus(entry.State, patchEntry.Status),
                LinesAdded = patchEntry.LinesAdded,
                LinesDeleted = patchEntry.LinesDeleted
            };
        }

        if (entry.State.HasFlag(FileStatus.NewInWorkdir))
        {
            string fullPath = Path.Combine(repository.Info.WorkingDirectory ?? repositoryPath, entry.FilePath);
            int linesCount = CountFileLines(fullPath);
            return new FileDiffSummary
            {
                Path = entry.FilePath,
                OldPath = null,
                Status = FileChangeStatus.Untracked,
                LinesAdded = linesCount,
                LinesDeleted = 0
            };
        }

        return new FileDiffSummary
        {
            Path = entry.FilePath,
            OldPath = null,
            Status = ResolveFallbackStatus(entry.State),
            LinesAdded = 0,
            LinesDeleted = 0
        };
    }

    private static FileChangeStatus ResolveStatus(FileStatus state, ChangeKind kind)
    {
        if (state.HasFlag(FileStatus.Conflicted))
        {
            return FileChangeStatus.Conflicted;
        }

        if (state.HasFlag(FileStatus.NewInWorkdir))
        {
            return FileChangeStatus.Untracked;
        }

        return MapChangeKind(kind);
    }

    private static FileChangeStatus MapChangeKind(ChangeKind kind)
    {
        return kind switch
        {
            ChangeKind.Added => FileChangeStatus.Added,
            ChangeKind.Deleted => FileChangeStatus.Deleted,
            ChangeKind.Modified => FileChangeStatus.Modified,
            ChangeKind.Renamed => FileChangeStatus.Renamed,
            ChangeKind.Copied => FileChangeStatus.Copied,
            ChangeKind.Ignored => FileChangeStatus.Ignored,
            _ => FileChangeStatus.Modified
        };
    }

    private static FileChangeStatus ResolveFallbackStatus(FileStatus state)
    {
        if (state.HasFlag(FileStatus.DeletedFromWorkdir) || state.HasFlag(FileStatus.DeletedFromIndex))
        {
            return FileChangeStatus.Deleted;
        }

        if (state.HasFlag(FileStatus.NewInIndex))
        {
            return FileChangeStatus.Added;
        }

        if (state.HasFlag(FileStatus.RenamedInIndex) || state.HasFlag(FileStatus.RenamedInWorkdir))
        {
            return FileChangeStatus.Renamed;
        }

        return FileChangeStatus.Modified;
    }

    private static int CountFileLines(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return 0;
        }

        try
        {
            return File.ReadAllLines(fullPath).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return 0;
        }
    }
}
