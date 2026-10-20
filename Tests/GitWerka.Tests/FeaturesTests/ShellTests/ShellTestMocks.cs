namespace GitWerka.Tests.FeaturesTests.ShellTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using ReactiveUI;

internal sealed class MockScreen : IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}

internal sealed class MockGitService : IGitService
{
    public bool IsValidRepository(string path) => true;

    public string? FindRepositoryRoot(string path) => path;

    public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(directoryPath);

    public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(targetDirectoryPath);

    public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitRepositoryInfo
        {
            Name = "TestRepo",
            Path = repositoryPath,
            CurrentBranch = "main"
        });

    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, string? branchName = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommitSummary>>(new List<CommitSummary>());

    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommitDetail?>(null);

    public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DiffLine>>(new List<DiffLine>());

    public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkingTreeChange>>(new List<WorkingTreeChange>());

    public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BranchInfo>>(new List<BranchInfo>());

    public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
        Task.FromResult("new-sha");

    public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TagInfo>>(Array.Empty<TagInfo>());

    public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StashInfo>>(Array.Empty<StashInfo>());

    public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RemoteInfo>>(Array.Empty<RemoteInfo>());

    public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitConfigInfo { UserName = "Tester", UserEmail = "test@example.com" });

    public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RepositoryOperationStatus { CurrentOperation = RepositoryOperationType.None });

    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode = GitMergeFastForwardMode.FastForward, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitMergeResult());

    public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RebaseStep>>(new List<RebaseStep>());

    public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConflictFileModel { FilePath = filePath, FileName = "file.txt" });

    public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class TestProfileRepository : IProfileRepository
{
    public List<GitProfile> Profiles { get; } = new();
    public Dictionary<string, int> RepositoryActiveProfiles { get; } = new();

    public Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitProfile>>(Profiles);

    public Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles.Find(p => p.Id == id));

    public Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
    {
        int index = Profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
        {
            Profiles[index] = profile;
        }
        else
        {
            Profiles.Add(profile);
        }
        return Task.FromResult(profile);
    }

    public Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        Profiles.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitAuthor>>(new List<GitAuthor>());

    public Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<GitAuthor?>(null);

    public Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default) =>
        Task.FromResult(author);

    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitProfileCredentials>>(new List<GitProfileCredentials>());

    public Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<GitProfileCredentials?>(null);

    public Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default) =>
        Task.FromResult(credentials);

    public Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (RepositoryActiveProfiles.TryGetValue(repositoryPath, out int profileId))
        {
            return Task.FromResult(Profiles.Find(p => p.Id == profileId));
        }
        return Task.FromResult<GitProfile?>(null);
    }

    public Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default)
    {
        RepositoryActiveProfiles[repositoryPath] = profileId;
        return Task.CompletedTask;
    }

    public Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        RepositoryActiveProfiles.Remove(repositoryPath);
        return Task.CompletedTask;
    }
}
