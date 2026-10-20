namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using ReactiveUI;

internal sealed class MockScreen : IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}

internal sealed class MockSystemLauncherService : ISystemLauncherService
{
    public string? LastFileManagerPath { get; private set; }
    public string? LastTerminalPath { get; private set; }
    public string? LastEditorPath { get; private set; }
    public string? LastEditorCommand { get; private set; }

    public bool OpenInFileManager(string directoryPath)
    {
        LastFileManagerPath = directoryPath;
        return true;
    }

    public bool OpenInTerminal(string directoryPath)
    {
        LastTerminalPath = directoryPath;
        return true;
    }

    public bool OpenInEditor(string directoryPath, string? editorCommand = null)
    {
        LastEditorPath = directoryPath;
        LastEditorCommand = editorCommand;
        return true;
    }
}

internal sealed class MockGitService : IGitService
{
    public List<CommitSummary> Commits { get; set; } = new();
    public List<BranchInfo> Branches { get; set; } = new();
    public List<WorkingTreeChange> WorkingChanges { get; set; } = new();
    public Dictionary<string, CommitDetail> Details { get; set; } = new();
    public List<DiffLine> DiffLines { get; set; } = new();
    public string? LastCheckedOutBranch { get; private set; }
    public string? LastRequestedBranchName { get; private set; }
    public Dictionary<string, IReadOnlyList<CommitSummary>> BranchCommits { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Func<string, bool>? IsValidRepositoryFunc { get; set; }
    public bool IsValidRepository(string path) => IsValidRepositoryFunc?.Invoke(path) ?? true;
    public string? FindRepositoryRoot(string path) => path;

    public bool InitializeCalled { get; private set; }
    public string? LastInitializedPath { get; private set; }

    public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default)
    {
        InitializeCalled = true;
        LastInitializedPath = directoryPath;
        return Task.FromResult(directoryPath);
    }

    public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(targetDirectoryPath);

    public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitRepositoryInfo
        {
            Name = "TestRepo",
            Path = repositoryPath,
            CurrentBranch = "main"
        });

    public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, string? branchName = null, CancellationToken cancellationToken = default)
    {
        LastRequestedBranchName = branchName;
        if (!string.IsNullOrEmpty(branchName) && BranchCommits.TryGetValue(branchName, out var branchSpecific))
        {
            return Task.FromResult(branchSpecific);
        }

        return Task.FromResult<IReadOnlyList<CommitSummary>>(Commits);
    }

    public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        Details.TryGetValue(commitSha, out CommitDetail? detail);
        return Task.FromResult(detail);
    }

    public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DiffLine>>(DiffLines);

    public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WorkingTreeChange>>(WorkingChanges);

    public string? LastCreatedBranch { get; private set; }
    public string? LastCreatedTag { get; private set; }
    public string? LastCreatedStash { get; private set; }
    public bool FetchCalled { get; private set; }
    public bool PullCalled { get; private set; }
    public bool PushCalled { get; private set; }
    public List<string> StagedFiles { get; } = new();
    public List<string> UnstagedFiles { get; } = new();

    public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        StagedFiles.Add(filePath);
        return Task.CompletedTask;
    }

    public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
    {
        UnstagedFiles.Add(filePath);
        return Task.CompletedTask;
    }

    public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BranchInfo>>(Branches);

    public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default)
    {
        LastCheckedOutBranch = branchName;
        return Task.CompletedTask;
    }

    public string? LastCommitMessage { get; private set; }
    public bool LastCommitWasAmend { get; private set; }
    public string? LastCommitAuthorName { get; private set; }
    public string? LastCommitAuthorEmail { get; private set; }
    public Exception? CreateCommitException { get; set; }

    public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default)
    {
        if (CreateCommitException != null)
        {
            throw CreateCommitException;
        }

        LastCommitMessage = message;
        LastCommitWasAmend = amendPreviousCommit;
        LastCommitAuthorName = authorName;
        LastCommitAuthorEmail = authorEmail;
        return Task.FromResult("new-sha");
    }

    public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default)
    {
        LastCreatedBranch = branchName;
        return Task.CompletedTask;
    }

    public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TagInfo>>(Array.Empty<TagInfo>());

    public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default)
    {
        LastCreatedTag = tagName;
        return Task.CompletedTask;
    }

    public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StashInfo>>(Array.Empty<StashInfo>());

    public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default)
    {
        LastCreatedStash = message;
        return Task.CompletedTask;
    }

    public Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public int? LastPoppedStashIndex { get; private set; }
    public int? LastDroppedStashIndex { get; private set; }

    public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        LastPoppedStashIndex = stashIndex;
        return Task.CompletedTask;
    }

    public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default)
    {
        LastDroppedStashIndex = stashIndex;
        return Task.CompletedTask;
    }

    public List<RemoteInfo> Remotes { get; set; } = new();

    public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RemoteInfo>>(Remotes);

    public string? LastAddedRemoteName { get; private set; }
    public string? LastAddedRemoteUrl { get; private set; }
    public string? LastRemovedRemoteName { get; private set; }
    public string? LastFetchedRemoteName { get; private set; }

    public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default)
    {
        LastAddedRemoteName = remoteName;
        LastAddedRemoteUrl = url;
        Remotes.Add(new RemoteInfo { Name = remoteName, Url = url, PushUrl = url });
        return Task.CompletedTask;
    }

    public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default)
    {
        LastRemovedRemoteName = remoteName;
        Remotes.RemoveAll(r => r.Name == remoteName);
        return Task.CompletedTask;
    }

    public GitCredentials? LastCredentialsUsed { get; private set; }
    public bool ShouldThrowAuthOnRemote { get; set; }
    public bool AlwaysThrowAuthOnRemote { get; set; }
    public Exception? PushException { get; set; }

    public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        if (AlwaysThrowAuthOnRemote || (ShouldThrowAuthOnRemote && credentials == null))
        {
            throw new Exception("Authentication required: 401 Unauthorized");
        }

        FetchCalled = true;
        LastFetchedRemoteName = remoteName;
        LastCredentialsUsed = credentials;
        return Task.CompletedTask;
    }

    public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrowAuthOnRemote && credentials == null)
        {
            throw new Exception("Authentication failed: invalid credentials");
        }

        PullCalled = true;
        LastCredentialsUsed = credentials;
        return Task.CompletedTask;
    }

    public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        if (PushException != null)
        {
            throw PushException;
        }

        if (ShouldThrowAuthOnRemote && credentials == null)
        {
            throw new Exception("Permission denied (publickey)");
        }

        PushCalled = true;
        LastCredentialsUsed = credentials;
        return Task.CompletedTask;
    }

    public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GitConfigInfo());

    public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public string? LastCherryPickedSha { get; private set; }
    public string? LastRevertedSha { get; private set; }
    public string? LastResetSha { get; private set; }
    public GitResetType? LastResetType { get; private set; }

    public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        LastCherryPickedSha = commitSha;
        return Task.CompletedTask;
    }

    public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default)
    {
        LastRevertedSha = commitSha;
        return Task.CompletedTask;
    }

    public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default)
    {
        LastResetSha = commitSha;
        LastResetType = resetType;
        return Task.CompletedTask;
    }

    public RepositoryOperationStatus OperationStatusToReturn { get; set; } = new();
    public string? LastMergedBranch { get; private set; }
    public GitMergeFastForwardMode? LastMergeMode { get; private set; }
    public bool AbortMergeCalled { get; private set; }
    public bool ContinueMergeCalled { get; private set; }
    public List<RebaseStep> StepsToReturn { get; set; } = new();
    public List<RebaseStep>? LastExecutedRebaseSteps { get; private set; }
    public string? LastRebaseUpstreamSha { get; private set; }
    public bool AbortRebaseCalled { get; private set; }
    public bool ContinueRebaseCalled { get; private set; }

    public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationStatusToReturn);

    public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode = GitMergeFastForwardMode.FastForward, CancellationToken cancellationToken = default)
    {
        LastMergedBranch = sourceBranchName;
        LastMergeMode = fastForwardMode;
        return Task.FromResult(new GitMergeResult());
    }

    public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        AbortMergeCalled = true;
        return Task.CompletedTask;
    }

    public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default)
    {
        ContinueMergeCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RebaseStep>>(StepsToReturn);

    public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default)
    {
        LastRebaseUpstreamSha = upstreamCommitSha;
        LastExecutedRebaseSteps = steps.ToList();
        return Task.FromResult(true);
    }

    public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        AbortRebaseCalled = true;
        return Task.CompletedTask;
    }

    public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        ContinueRebaseCalled = true;
        return Task.FromResult(true);
    }

    public bool MarkAllConflictsResolvedCalled { get; private set; }
    public string? LastSavedConflictFilePath { get; private set; }
    public string? LastSavedConflictContent { get; private set; }

    public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConflictFileModel { FilePath = filePath, FileName = "file.txt", TotalConflicts = 1 });

    public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default)
    {
        LastSavedConflictFilePath = filePath;
        LastSavedConflictContent = resolvedContent;
        return Task.CompletedTask;
    }

    public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        MarkAllConflictsResolvedCalled = true;
        return Task.CompletedTask;
    }
}

internal sealed class TestRecentRepositoriesRepository : IRecentRepositoriesRepository
{
    public List<RecentRepository> Items { get; } = new();

    public Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecentRepository>>(Items);

    public Task AddOrUpdateRecentRepositoryAsync(RecentRepository repository, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(x => x.Path == repository.Path);
        Items.Add(repository);
        return Task.CompletedTask;
    }

    public Task RemoveRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(x => x.Path == repositoryPath);
        return Task.CompletedTask;
    }

    public Task TogglePinRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<RecentRepository?> GetRepositoryByPathAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(x => x.Path == repositoryPath));

    public Task SetFavoriteAsync(string repositoryPath, bool isFavorite, string? customName = null, string? groupName = null, CancellationToken cancellationToken = default)
    {
        var existing = Items.FirstOrDefault(x => x.Path == repositoryPath);
        if (existing != null)
        {
            Items.Remove(existing);
            Items.Add(existing with { IsFavorite = isFavorite, CustomName = isFavorite ? customName : null, GroupName = isFavorite ? groupName : null });
        }
        else
        {
            Items.Add(new RecentRepository
            {
                Path = repositoryPath,
                Name = new System.IO.DirectoryInfo(repositoryPath).Name,
                IsFavorite = isFavorite,
                CustomName = isFavorite ? customName : null,
                GroupName = isFavorite ? groupName : null,
                LastOpenedAtUtc = DateTime.UtcNow
            });
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecentRepository>>(Items.Where(x => x.IsFavorite).ToList());

    public Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Where(x => !x.IsFavorite).OrderByDescending(x => x.LastOpenedAtUtc).FirstOrDefault());
}

internal sealed class MockAiCommitMessageService : IAiCommitMessageService
{
    public string ProposalToReturn { get; set; } = "feat(core): add feature";
    public Exception? ExceptionToThrow { get; set; }
    public string? LastRepositoryPath { get; private set; }
    public Action<IProgress<AiRetryAttempt>?>? OnGenerate { get; set; }

    public Task<string> GenerateCommitMessageAsync(
        string repositoryPath,
        IProgress<AiRetryAttempt>? retryProgress,
        CancellationToken cancellationToken = default)
    {
        LastRepositoryPath = repositoryPath;
        OnGenerate?.Invoke(retryProgress);
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }
        return Task.FromResult(ProposalToReturn);
    }
}

internal sealed class MockAiConfigurationRepository : IAiConfigurationRepository
{
    public AiConfiguration ConfigurationToReturn { get; set; } = new();
    public AiConfiguration? LastSavedConfiguration { get; private set; }
    public Dictionary<string, string?> Descriptions { get; } = new();
    public (string Path, string? Desc)? LastSavedDescription { get; private set; }

    public Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ConfigurationToReturn);

    public Task SaveConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default)
    {
        LastSavedConfiguration = configuration;
        return Task.CompletedTask;
    }

    public Task<string?> GetProjectDescriptionAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Descriptions.GetValueOrDefault(repositoryPath));

    public Task SaveProjectDescriptionAsync(string repositoryPath, string? description, CancellationToken cancellationToken = default)
    {
        LastSavedDescription = (repositoryPath, description);
        Descriptions[repositoryPath] = description;
        return Task.CompletedTask;
    }
}

internal sealed class MockApplicationSettingsRepository : IApplicationSettingsRepository
{
    public ApplicationSettings Settings { get; set; } = new();

    public Task<ApplicationSettings> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Settings);

    public Task SaveSettingsAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}

internal sealed class TestProfileRepository : IProfileRepository
{
    public List<GitProfile> Profiles { get; } = new();
    public Dictionary<string, int> Mappings { get; } = new();

    public Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitProfile>>(Profiles);

    public Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles.FirstOrDefault(p => p.Id == id));

    public Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
    {
        Profiles.Add(profile);
        return Task.FromResult(profile);
    }

    public Task DeleteProfileAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<GitAuthor>> GetAllAuthorsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitAuthor>>(new List<GitAuthor>());
    public Task<GitAuthor?> GetAuthorByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<GitAuthor?>(null);
    public Task<GitAuthor> SaveAuthorAsync(GitAuthor author, CancellationToken cancellationToken = default) => Task.FromResult(author);
    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitProfileCredentials>>(new List<GitProfileCredentials>());
    public Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<GitProfileCredentials?>(null);
    public Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default) => Task.FromResult(credentials);
    public Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (Mappings.TryGetValue(repositoryPath, out int id))
        {
            return GetProfileByIdAsync(id, cancellationToken);
        }
        return Task.FromResult<GitProfile?>(null);
    }

    public Task SetActiveProfileForRepositoryAsync(string repositoryPath, int profileId, CancellationToken cancellationToken = default)
    {
        Mappings[repositoryPath] = profileId;
        return Task.CompletedTask;
    }

    public Task ClearActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        Mappings.Remove(repositoryPath);
        return Task.CompletedTask;
    }
}
