namespace GitWerka.Features.RepositoryManager.Domain.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.RepositoryManager.Domain.Enums;

public record RepositoryCollectionsResult(
    ImmutableList<RecentRepository> All,
    ImmutableList<RecentRepository> FilteredAll,
    ImmutableList<RecentRepository> Favorites,
    ImmutableList<RecentRepository> FilteredFavorites,
    ImmutableList<FavoriteRepositoryGroup> Groups,
    ImmutableList<RecentRepository> Ungrouped,
    ImmutableList<string> GroupNames,
    ImmutableList<RecentRepository> Displayed);

/// <summary>
/// Helper utilities for repository filtering, search matching, and favorite group hierarchies.
/// </summary>
public static class RepositoryGroupingHelper
{
    public static RepositoryCollectionsResult BuildCollections(
        IEnumerable<RecentRepository> repos,
        IEnumerable<RecentRepository> favorites,
        string searchQuery,
        ImmutableHashSet<string> collapsedGroups,
        RepositoryManagerTab selectedTab)
    {
        var allList = repos.ToImmutableList();
        var favList = favorites.ToImmutableList();

        var filteredAll = FilterRepositories(allList, searchQuery);
        var filteredFav = FilterRepositories(favList, searchQuery);
        var (groups, ungrouped) = BuildFavoriteGroupsAndUngrouped(favList, collapsedGroups, searchQuery);
        var groupNames = ExtractDistinctGroupNames(favList);

        var displayed = selectedTab == RepositoryManagerTab.Favorites
            ? filteredFav
            : filteredAll;

        return new RepositoryCollectionsResult(allList, filteredAll, favList, filteredFav, groups, ungrouped, groupNames, displayed);
    }

    public static ImmutableList<string> ExtractDistinctGroupNames(ImmutableList<RecentRepository> favorites)
    {
        return favorites
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .Select(r => r.GroupName!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
    }

    public static (ImmutableList<FavoriteRepositoryGroup> Groups, ImmutableList<RecentRepository> Ungrouped) BuildFavoriteGroupsAndUngrouped(
        ImmutableList<RecentRepository> favorites,
        ImmutableHashSet<string> collapsedGroups,
        string searchQuery)
    {
        bool hasSearch = !string.IsNullOrWhiteSpace(searchQuery);

        var groups = favorites
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
            .GroupBy(r => r.GroupName!.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => !hasSearch || g.Key.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || g.Any(r => MatchesSearch(r, searchQuery)))
            .Select(g =>
            {
                var repos = hasSearch && !g.Key.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    ? g.Where(r => MatchesSearch(r, searchQuery)).ToImmutableList()
                    : g.ToImmutableList();

                return new FavoriteRepositoryGroup
                {
                    Name = g.Key,
                    Repositories = repos,
                    IsExpanded = hasSearch || !collapsedGroups.Contains(g.Key)
                };
            })
            .Where(g => g.Repositories.Count > 0)
            .ToImmutableList();

        var ungrouped = favorites
            .Where(r => string.IsNullOrWhiteSpace(r.GroupName))
            .Where(r => !hasSearch || MatchesSearch(r, searchQuery))
            .ToImmutableList();

        return (groups, ungrouped);
    }

    public static bool MatchesSearch(RecentRepository repo, string query)
    {
        return repo.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               repo.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               repo.Path.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public static ImmutableList<RecentRepository> FilterRepositories(ImmutableList<RecentRepository> repos, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return repos;
        }

        return repos
            .Where(r => MatchesSearch(r, query))
            .ToImmutableList();
    }
}
