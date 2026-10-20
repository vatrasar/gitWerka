namespace GitWerka.Core.Domain.Models;

using System;
using System.Collections.Immutable;
using System.Linq;
using GitWerka.Core.Domain.Enums;

/// <summary>
/// Lightweight domain model representing a snapshot of Git repository state
/// used for detecting changes in commits, working tree, branches, and remotes.
/// </summary>
public record GitRepositoryStateSnapshot : IEquatable<GitRepositoryStateSnapshot>
{
    public string HeadCommitSha { get; init; } = string.Empty;

    public string CurrentBranch { get; init; } = string.Empty;

    public bool IsHeadDetached { get; init; }

    public RepositoryOperationType CurrentOperation { get; init; }

    public bool HasConflicts { get; init; }

    public ImmutableList<string> BranchSignatures { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> RemoteSignatures { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> WorkingTreeSignatures { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> TagSignatures { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> StashSignatures { get; init; } = ImmutableList<string>.Empty;

    public virtual bool Equals(GitRepositoryStateSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return HeadCommitSha == other.HeadCommitSha
            && CurrentBranch == other.CurrentBranch
            && IsHeadDetached == other.IsHeadDetached
            && CurrentOperation == other.CurrentOperation
            && HasConflicts == other.HasConflicts
            && BranchSignatures.SequenceEqual(other.BranchSignatures)
            && RemoteSignatures.SequenceEqual(other.RemoteSignatures)
            && WorkingTreeSignatures.SequenceEqual(other.WorkingTreeSignatures)
            && TagSignatures.SequenceEqual(other.TagSignatures)
            && StashSignatures.SequenceEqual(other.StashSignatures);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(HeadCommitSha);
        hash.Add(CurrentBranch);
        hash.Add(IsHeadDetached);
        hash.Add(CurrentOperation);
        hash.Add(HasConflicts);
        hash.Add(BranchSignatures.Count);
        hash.Add(RemoteSignatures.Count);
        hash.Add(WorkingTreeSignatures.Count);
        hash.Add(TagSignatures.Count);
        hash.Add(StashSignatures.Count);
        return hash.ToHashCode();
    }
}
