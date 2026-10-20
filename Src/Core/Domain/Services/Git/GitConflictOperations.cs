namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git merge conflicts resolution and file chunks parsing.
/// </summary>
public class GitConflictOperations
{
    /// <summary>
    /// Retrieves the conflicted file data, Git metadata (local/remote SHAs and branches), and parsed conflict hunks.
    /// </summary>
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
    /// </summary>
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
    /// </summary>
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
