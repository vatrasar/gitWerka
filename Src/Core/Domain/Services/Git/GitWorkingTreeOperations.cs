namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git working tree changes, staging, and diffs.
/// </summary>
public class GitWorkingTreeOperations
{
    /// <summary>
    /// Reads working tree changes (both staged and unstaged) including file statuses, line additions, and line deletions.
    /// Invoked by: <see cref="GitService.GetWorkingTreeChangesAsync"/>.
    /// </summary>
    public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            RepositoryStatus status = repository.RetrieveStatus(new StatusOptions());
            string workingDirectory = repository.Info.WorkingDirectory ?? repositoryPath;
            Dictionary<string, (int Added, int Deleted)> stagedStats = BuildStagedStats(repository);
            Dictionary<string, (int Added, int Deleted)> unstagedStats = BuildUnstagedStats(repository);
            List<WorkingTreeChange> changes = new();

            foreach (StatusEntry entry in status)
            {
                if (entry.State == FileStatus.Ignored || entry.State == FileStatus.Unaltered)
                {
                    continue;
                }

                AppendWorkingTreeEntries(entry, changes, workingDirectory, stagedStats, unstagedStats);
            }

            return (IReadOnlyList<WorkingTreeChange>)changes;
        }, cancellationToken);
    }

    /// <summary>
    /// Stages a specific file in the repository index.
    /// </summary>
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
    /// Unstages a specific file from the repository index.
    /// </summary>
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
    /// Discards uncommitted modifications to a file in the working directory.
    /// </summary>
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
    /// Computes the diff lines for a specific file in a commit or working tree.
    /// </summary>
    public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(
        string repositoryPath,
        string commitSha,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);

            if (string.IsNullOrEmpty(commitSha) || commitSha == "WIP")
            {
                return GetWorkingTreeFileDiffLines(repository, repositoryPath, filePath);
            }

            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return (IReadOnlyList<DiffLine>)Array.Empty<DiffLine>();
            }

            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            Tree commitTree = commit.Tree;

            CompareOptions options = new() { ContextLines = 3 };
            Patch patch = repository.Diff.Compare<Patch>(parentTree, commitTree, new[] { filePath }, null, options);
            PatchEntryChanges? entry = patch[filePath];

            if (entry == null)
            {
                return (IReadOnlyList<DiffLine>)Array.Empty<DiffLine>();
            }

            return ParsePatchLines(entry.Patch);
        }, cancellationToken);
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

    private static Dictionary<string, (int Added, int Deleted)> BuildStagedStats(Repository repository)
    {
        Dictionary<string, (int Added, int Deleted)> stats = new(StringComparer.Ordinal);
        try
        {
            PopulateStagedStats(repository, stats);
        }
        catch (LibGit2SharpException exception)
        {
            System.Diagnostics.Debug.WriteLine(exception.Message);
        }

        return stats;
    }

    private static void PopulateStagedStats(Repository repository, Dictionary<string, (int Added, int Deleted)> stats)
    {
        Tree? headTree = repository.Head.Tip?.Tree;
        Patch patch = repository.Diff.Compare<Patch>(headTree, DiffTargets.Index);
        foreach (PatchEntryChanges entry in patch)
        {
            stats[entry.Path] = (entry.LinesAdded, entry.LinesDeleted);
        }
    }

    private static Dictionary<string, (int Added, int Deleted)> BuildUnstagedStats(Repository repository)
    {
        Dictionary<string, (int Added, int Deleted)> stats = new(StringComparer.Ordinal);
        try
        {
            PopulateUnstagedStats(repository, stats);
        }
        catch (LibGit2SharpException exception)
        {
            System.Diagnostics.Debug.WriteLine(exception.Message);
        }

        return stats;
    }

    private static void PopulateUnstagedStats(Repository repository, Dictionary<string, (int Added, int Deleted)> stats)
    {
        Patch patch = repository.Diff.Compare<Patch>();
        foreach (PatchEntryChanges entry in patch)
        {
            stats[entry.Path] = (entry.LinesAdded, entry.LinesDeleted);
        }
    }

    private static void AppendWorkingTreeEntries(
        StatusEntry entry,
        List<WorkingTreeChange> changes,
        string workingDirectory,
        Dictionary<string, (int Added, int Deleted)> stagedStats,
        Dictionary<string, (int Added, int Deleted)> unstagedStats)
    {
        string fullPath = Path.GetFullPath(Path.Combine(workingDirectory, entry.FilePath));

        if (entry.State.HasFlag(FileStatus.Conflicted))
        {
            changes.Add(new WorkingTreeChange
            {
                Path = entry.FilePath,
                FullPath = fullPath,
                Status = FileChangeStatus.Conflicted,
                IsStaged = false
            });
            return;
        }

        if (IsStagedStatus(entry.State))
        {
            AppendStagedEntry(entry, changes, fullPath, stagedStats);
        }

        if (IsWorkdirStatus(entry.State))
        {
            AppendWorkdirEntry(entry, changes, fullPath, unstagedStats);
        }
    }

    private static void AppendStagedEntry(
        StatusEntry entry,
        List<WorkingTreeChange> changes,
        string fullPath,
        Dictionary<string, (int Added, int Deleted)> stagedStats)
    {
        stagedStats.TryGetValue(entry.FilePath, out (int Added, int Deleted) lineChange);
        changes.Add(new WorkingTreeChange
        {
            Path = entry.FilePath,
            FullPath = fullPath,
            Status = ResolveIndexStatus(entry.State),
            IsStaged = true,
            LinesAdded = lineChange.Added,
            LinesDeleted = lineChange.Deleted
        });
    }

    private static void AppendWorkdirEntry(
        StatusEntry entry,
        List<WorkingTreeChange> changes,
        string fullPath,
        Dictionary<string, (int Added, int Deleted)> unstagedStats)
    {
        (int Added, int Deleted) lineChange = ResolveWorkdirLineStats(entry, fullPath, unstagedStats);
        changes.Add(new WorkingTreeChange
        {
            Path = entry.FilePath,
            FullPath = fullPath,
            Status = ResolveWorkdirStatus(entry.State),
            IsStaged = false,
            LinesAdded = lineChange.Added,
            LinesDeleted = lineChange.Deleted
        });
    }

    private static (int Added, int Deleted) ResolveWorkdirLineStats(
        StatusEntry entry,
        string fullPath,
        Dictionary<string, (int Added, int Deleted)> unstagedStats)
    {
        if (entry.State.HasFlag(FileStatus.NewInWorkdir))
        {
            return (CountFileLines(fullPath), 0);
        }

        unstagedStats.TryGetValue(entry.FilePath, out (int Added, int Deleted) stats);
        return stats;
    }

    private static int CountFileLines(string fullPath)
    {
        try
        {
            return CountLinesFromStream(fullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine(exception.Message);
            return 0;
        }
    }

    private static int CountLinesFromStream(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return 0;
        }

        using FileStream stream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(stream);
        return ReadLineCount(reader);
    }

    private static int ReadLineCount(StreamReader reader)
    {
        int lineCount = 0;
        while (reader.ReadLine() != null)
        {
            lineCount++;
        }

        return lineCount;
    }

    private static bool IsStagedStatus(FileStatus state) =>
        state.HasFlag(FileStatus.ModifiedInIndex) ||
        state.HasFlag(FileStatus.NewInIndex) ||
        state.HasFlag(FileStatus.DeletedFromIndex) ||
        state.HasFlag(FileStatus.RenamedInIndex);

    private static bool IsWorkdirStatus(FileStatus state) =>
        state.HasFlag(FileStatus.ModifiedInWorkdir) ||
        state.HasFlag(FileStatus.NewInWorkdir) ||
        state.HasFlag(FileStatus.DeletedFromWorkdir) ||
        state.HasFlag(FileStatus.RenamedInWorkdir);

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

    internal static FileDiffSummary MapToFileDiffSummary(TreeEntryChanges change, Repository repo, Tree? oldTree, Tree newTree)
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

    internal static IReadOnlyList<DiffLine> ParsePatchLines(string patchText)
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
}
