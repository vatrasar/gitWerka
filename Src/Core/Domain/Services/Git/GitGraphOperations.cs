namespace GitWerka.Core.Domain.Services.Git;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using LibGit2Sharp;

/// <summary>
/// Specialized operations for Git commit graph topology and commit metadata.
/// </summary>
public class GitGraphOperations
{
    /// <summary>
    /// Reads commits and computes visual graph lane indices.
    /// </summary>
    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(
        string repositoryPath,
        int maxCount = 300,
        int maxBranches = 5,
        string? branchName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);

            var branchMap = BuildReferenceMap(repository.Branches);
            var tagMap = BuildReferenceMap(repository.Tags);

            List<Commit> startingCommits = ResolveReachableCommits(repository, maxBranches, branchName);
            if (startingCommits.Count == 0)
            {
                return (IReadOnlyList<CommitSummary>)Array.Empty<CommitSummary>();
            }

            CommitFilter filter = new()
            {
                IncludeReachableFrom = startingCommits,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            Dictionary<int, string> reservedLanes = new();
            List<CommitSummary> summaries = new();

            int uncommittedCount = repository.RetrieveStatus(new StatusOptions()).Count(e => e.State != FileStatus.Ignored && e.State != FileStatus.Unaltered);
            if (uncommittedCount > 0)
            {
                summaries.Add(CreateWipCommitSummary(repository, uncommittedCount));
            }

            foreach (Commit commit in repository.Commits.QueryBy(filter))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (summaries.Count >= maxCount)
                {
                    break;
                }

                CommitSummary summary = ProcessCommitNode(commit, reservedLanes, branchMap, tagMap);
                summaries.Add(summary);
            }

            ApplyGraphWidth(summaries);

            return (IReadOnlyList<CommitSummary>)summaries;
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves full details for a commit, including author, parents, and file diff summaries.
    /// </summary>
    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            using Repository repository = new(repositoryPath);
            if (string.Equals(commitSha, "WIP", StringComparison.OrdinalIgnoreCase))
            {
                return WorkingTreeDiffCalculator.CreateWipCommitDetail(repository, repositoryPath);
            }

            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
            {
                return null;
            }

            Tree? parentTree = commit.Parents.FirstOrDefault()?.Tree;
            Tree commitTree = commit.Tree;

            TreeChanges changes = repository.Diff.Compare<TreeChanges>(parentTree, commitTree);
            List<FileDiffSummary> fileDiffs = new();

            foreach (TreeEntryChanges change in changes)
            {
                fileDiffs.Add(GitWorkingTreeOperations.MapToFileDiffSummary(change, repository, parentTree, commitTree));
            }

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
                ChangedFiles = fileDiffs.ToImmutableList()
            };
        }, cancellationToken);
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

    private static List<Commit> ResolveReachableCommits(Repository repository, int maxBranches, string? branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            int effectiveMaxBranches = maxBranches > 0 ? maxBranches : 5;
            return BuildReachableCommitList(repository, effectiveMaxBranches);
        }

        return BuildSingleBranchCommitList(repository, branchName);
    }

    private static List<Commit> BuildSingleBranchCommitList(Repository repository, string branchName)
    {
        List<Commit> commits = new();
        Branch? branch = repository.Branches[branchName] ??
                         repository.Branches.FirstOrDefault(b => string.Equals(b.FriendlyName, branchName, StringComparison.OrdinalIgnoreCase));

        if (branch?.Tip != null)
        {
            commits.Add(branch.Tip);
        }

        return commits;
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
}
