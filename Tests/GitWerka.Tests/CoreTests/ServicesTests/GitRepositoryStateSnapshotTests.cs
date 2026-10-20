namespace GitWerka.Tests.CoreTests.ServicesTests;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using Xunit;

public class GitRepositoryStateSnapshotTests
{
    [Fact]
    public void Equals_WithIdenticalValues_ReturnsTrueAndMatchingHashCode()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = CreateSampleSnapshot();

        Assert.True(snapshot1.Equals(snapshot2));
        Assert.True(snapshot1 == snapshot2);
        Assert.Equal(snapshot1.GetHashCode(), snapshot2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentHeadCommitSha_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with { HeadCommitSha = "different_sha" };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentCurrentBranch_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with { CurrentBranch = "feature/test" };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentHeadDetached_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with { IsHeadDetached = true };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentBranchSignatures_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with
        {
            BranchSignatures = snapshot1.BranchSignatures.Add("feature/new:sha3:false:false")
        };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentRemoteSignatures_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with
        {
            RemoteSignatures = snapshot1.RemoteSignatures.Add("upstream:https://upstream.org:https://upstream.org")
        };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentWorkingTreeSignatures_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with
        {
            WorkingTreeSignatures = snapshot1.WorkingTreeSignatures.Add("newfile.txt:128:12345:100")
        };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentTagSignatures_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with
        {
            TagSignatures = snapshot1.TagSignatures.Add("v2.0:sha9")
        };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentStashSignatures_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with
        {
            StashSignatures = snapshot1.StashSignatures.Add("1:stash_sha2")
        };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentCurrentOperation_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with { CurrentOperation = RepositoryOperationType.Merge };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    [Fact]
    public void Equals_WithDifferentHasConflicts_ReturnsFalse()
    {
        var snapshot1 = CreateSampleSnapshot();
        var snapshot2 = snapshot1 with { HasConflicts = true };

        Assert.False(snapshot1.Equals(snapshot2));
    }

    private static GitRepositoryStateSnapshot CreateSampleSnapshot()
    {
        return new GitRepositoryStateSnapshot
        {
            HeadCommitSha = "aaa111",
            CurrentBranch = "main",
            IsHeadDetached = false,
            CurrentOperation = RepositoryOperationType.None,
            HasConflicts = false,
            BranchSignatures = ImmutableList.Create("main:aaa111:false:true", "origin/main:aaa111:true:false"),
            RemoteSignatures = ImmutableList.Create("origin:https://example.com:https://example.com"),
            WorkingTreeSignatures = ImmutableList.Create("file.cs:256:1000:50"),
            TagSignatures = ImmutableList.Create("v1.0:aaa111"),
            StashSignatures = ImmutableList.Create("0:stash_sha")
        };
    }
}
