namespace GitWerka.Core.Domain.Services.Git;

using System;
using LibGit2Sharp;

/// <summary>
/// Shared helper utilities for Git operations.
/// </summary>
internal static class GitServiceHelper
{
    public static Signature BuildSignatureOrDefault(Repository repo)
    {
        return repo.Config.BuildSignature(DateTimeOffset.Now) ?? new Signature("GitWerka", "gitwerka@local", DateTimeOffset.Now);
    }
}
