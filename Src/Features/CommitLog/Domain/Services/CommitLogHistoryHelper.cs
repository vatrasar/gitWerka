namespace GitWerka.Features.CommitLog.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Resources;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;

/// <summary>
/// Pure helper methods for filtering commits, formatting branch items, and creating WIP detail entries.
/// </summary>
public static class CommitLogHistoryHelper
{
    public static string? ResolveActiveBranchName(string? currentSelected, IReadOnlyList<BranchInfo> branches)
    {
        if (string.IsNullOrEmpty(currentSelected))
        {
            return null;
        }

        bool branchExists = branches.Any(b => string.Equals(b.Name, currentSelected, StringComparison.OrdinalIgnoreCase));
        return branchExists ? currentSelected : null;
    }

    public static ImmutableList<BranchListItem> BuildBranchListItems(IReadOnlyList<BranchInfo> branches, int maxVisibleBranches)
    {
        int effectiveMax = maxVisibleBranches > 0 ? maxVisibleBranches : 5;
        string allBranchesLabel = branches.Count > effectiveMax
            ? string.Format(CommitLogStrings.RecentlyModifiedBranchesFormat, effectiveMax)
            : CommitLogStrings.AllBranchesOption;

        BranchListItem allBranchesItem = new()
        {
            Name = string.Empty,
            DisplayName = allBranchesLabel,
            IsAllBranches = true
        };

        return ImmutableList.Create(allBranchesItem).AddRange(branches.Select(MapToBranchListItem));
    }

    public static BranchListItem MapToBranchListItem(BranchInfo branch) => new()
    {
        Name = branch.Name,
        DisplayName = branch.Name,
        IsRemote = branch.IsRemote,
        IsCurrentHead = branch.IsCurrentHead,
        TipSha = branch.TipSha,
        IsAllBranches = false
    };

    public static ImmutableList<CommitSummary> FilterCommits(ImmutableList<CommitSummary> commits, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return commits;
        }

        return commits.Where(c => c.IsWip ||
                    c.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.Sha.Contains(query, StringComparison.OrdinalIgnoreCase)).ToImmutableList();
    }

    public static string ExtractMergeSourceBranch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "remote";
        }

        var match = Regex.Match(message, @"(?:branch|remote-tracking branch)\s+'([^']+)'");
        return match.Success ? match.Groups[1].Value : "remote";
    }

    public static CommitDetail BuildWipCommitDetail(IReadOnlyList<WorkingTreeChange> changes, ImmutableList<string> parentShas)
    {
        var changedFiles = changes
            .GroupBy(c => c.Path)
            .Select(g => new FileDiffSummary
            {
                Path = g.Key,
                Status = g.First().Status,
                LinesAdded = 0,
                LinesDeleted = 0
            })
            .ToImmutableList();

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
            ChangedFiles = changedFiles
        };
    }
}
