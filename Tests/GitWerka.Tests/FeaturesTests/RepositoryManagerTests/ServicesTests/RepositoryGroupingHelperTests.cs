namespace GitWerka.Tests.FeaturesTests.RepositoryManagerTests.ServicesTests;

using System;
using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.RepositoryManager.Domain.Enums;
using GitWerka.Features.RepositoryManager.Domain.Services;
using Xunit;

public class RepositoryGroupingHelperTests
{
    [Fact]
    public void BuildFavoriteGroupsAndUngrouped_WhenExpandedGroupsIsEmpty_AllGroupsAreCollapsedByDefault()
    {
        var favorites = ImmutableList.Create(
            new RecentRepository { Name = "repo1", Path = "/p1", IsFavorite = true, GroupName = "Group A" },
            new RecentRepository { Name = "repo2", Path = "/p2", IsFavorite = true, GroupName = "Group B" },
            new RecentRepository { Name = "repo3", Path = "/p3", IsFavorite = true, GroupName = null }
        );

        var (groups, ungrouped) = RepositoryGroupingHelper.BuildFavoriteGroupsAndUngrouped(
            favorites,
            ImmutableHashSet<string>.Empty,
            string.Empty);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.False(g.IsExpanded));
        Assert.Single(ungrouped);
    }

    [Fact]
    public void BuildFavoriteGroupsAndUngrouped_WhenSpecificGroupIsExpanded_OnlyThatGroupIsExpanded()
    {
        var favorites = ImmutableList.Create(
            new RecentRepository { Name = "repo1", Path = "/p1", IsFavorite = true, GroupName = "Group A" },
            new RecentRepository { Name = "repo2", Path = "/p2", IsFavorite = true, GroupName = "Group B" }
        );

        var expanded = ImmutableHashSet.Create("Group B");

        var (groups, _) = RepositoryGroupingHelper.BuildFavoriteGroupsAndUngrouped(
            favorites,
            expanded,
            string.Empty);

        var groupA = Assert.Single(groups, g => g.Name == "Group A");
        var groupB = Assert.Single(groups, g => g.Name == "Group B");

        Assert.False(groupA.IsExpanded);
        Assert.True(groupB.IsExpanded);
    }

    [Fact]
    public void BuildFavoriteGroupsAndUngrouped_WhenSearchQueryMatches_GroupIsAutoExpanded()
    {
        var favorites = ImmutableList.Create(
            new RecentRepository { Name = "web-client", Path = "/client", IsFavorite = true, GroupName = "Frontend" }
        );

        var (groups, _) = RepositoryGroupingHelper.BuildFavoriteGroupsAndUngrouped(
            favorites,
            ImmutableHashSet<string>.Empty,
            "client");

        var group = Assert.Single(groups);
        Assert.True(group.IsExpanded);
    }

    [Fact]
    public void BuildCollections_WhenCalledWithEmptyExpandedGroups_ProducesCollapsedGroups()
    {
        var favorites = ImmutableList.Create(
            new RecentRepository { Name = "api-service", Path = "/api", IsFavorite = true, GroupName = "Services" }
        );

        var result = RepositoryGroupingHelper.BuildCollections(
            ImmutableList<RecentRepository>.Empty,
            favorites,
            string.Empty,
            ImmutableHashSet<string>.Empty,
            RepositoryManagerTab.Favorites);

        var group = Assert.Single(result.Groups);
        Assert.False(group.IsExpanded);
    }
}
