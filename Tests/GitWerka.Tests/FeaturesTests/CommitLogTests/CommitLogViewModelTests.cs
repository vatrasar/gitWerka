namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using ReactiveUI;
using System.Reactive.Linq;
using System.Linq;
using Xunit;

public class CommitLogViewModelTests
{
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockSystemLauncherService : ISystemLauncherService
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

    private class MockGitService : IGitService
    {
        public List<CommitSummary> Commits { get; set; } = new();
        public List<BranchInfo> Branches { get; set; } = new();
        public List<WorkingTreeChange> WorkingChanges { get; set; } = new();
        public Dictionary<string, CommitDetail> Details { get; set; } = new();
        public List<DiffLine> DiffLines { get; set; } = new();
        public string? LastCheckedOutBranch { get; private set; }

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

        public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommitSummary>>(Commits);

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

        public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default)
        {
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

    [Fact]
    public void UpdateSearchQuery_FilterByCommitMessage_ReturnsMatchingCommitsOnly()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "1111111", Message = "Add feature A", AuthorName = "Alice" });
        gitService.Commits.Add(new CommitSummary { Sha = "2222222", Message = "Fix bug B", AuthorName = "Bob" });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.UpdateSearchQuery("feature");

        Assert.Single(vm.State.FilteredCommits);
        Assert.Equal("1111111", vm.State.FilteredCommits[0].Sha);
    }

    [Fact]
    public async Task SelectCommitAsync_WithValidCommit_UpdatesSelectedCommitAndDetail()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "aaa111", Message = "Initial", AuthorName = "Charlie" };
        gitService.Commits.Add(commit);

        CommitDetail detail = new()
        {
            Sha = "aaa111",
            Message = "Initial commit",
            AuthorName = "Charlie",
            AuthorEmail = "charlie@example.com",
            ChangedFiles = ImmutableList.Create(new FileDiffSummary
            {
                Path = "test.txt",
                Status = FileChangeStatus.Added,
                LinesAdded = 10,
                LinesDeleted = 0
            })
        };
        gitService.Details[commit.Sha] = detail;

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        await vm.SelectCommitAsync(commit);

        Assert.NotNull(vm.State.SelectedCommit);
        Assert.Equal("aaa111", vm.State.SelectedCommit.Sha);
        Assert.NotNull(vm.State.SelectedCommitDetail);
        Assert.Equal("Initial commit", vm.State.SelectedCommitDetail.Message);
    }

    [Fact]
    public async Task SelectFileDiffAsync_WithValidFile_LoadsDiffLines()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "bbb222", Message = "Update file", AuthorName = "Dan" };
        gitService.Commits.Add(commit);
        gitService.DiffLines.Add(new DiffLine { Type = DiffLineType.Addition, Content = "+line 1", NewLineNumber = 1 });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        await vm.SelectCommitAsync(commit);

        FileDiffSummary fileDiff = new() { Path = "readme.md", Status = FileChangeStatus.Modified };
        await vm.SelectFileDiffAsync(fileDiff);

        Assert.NotNull(vm.State.SelectedFileDiff);
        Assert.Equal("readme.md", vm.State.SelectedFileDiff.Path);
        Assert.Single(vm.State.DiffLines);
        Assert.Equal("+line 1", vm.State.DiffLines[0].Content);
    }

    [Fact]
    public void CheckoutBranch_WithValidBranchName_InvokesCheckoutAndRefreshesData()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "develop" });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.CheckoutBranchCommand.Execute("develop").Subscribe();

        Assert.Equal("develop", gitService.LastCheckedOutBranch);
    }

    [Fact]
    public void UpdateCommitMessageInput_ValidText_UpdatesState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        vm.UpdateCommitMessageInput("feat: add new feature");

        Assert.Equal("feat: add new feature", vm.State.CommitMessageInput);
    }

    [Fact]
    public void SetActiveSidebarTab_TabPassed_UpdatesActiveSidebarTab()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.SetActiveSidebarTab(CommitLogSidebarTab.WorkingTree);

        Assert.Equal(CommitLogSidebarTab.WorkingTree, vm.State.ActiveSidebarTab);
    }

    [Fact]
    public void SetActiveSidebarTab_RemotesPassed_UpdatesActiveSidebarTab()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.SetActiveSidebarTab(CommitLogSidebarTab.Remotes);

        Assert.Equal(CommitLogSidebarTab.Remotes, vm.State.ActiveSidebarTab);
    }

    [Fact]
    public void ShowAndHideCreateRemoteModal_InvokesCommands_TogglesModalVisibility()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        Assert.False(vm.State.IsCreateRemoteModalVisible);

        vm.ShowCreateRemoteModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCreateRemoteModalVisible);

        vm.HideCreateRemoteModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsCreateRemoteModalVisible);
    }

    [Fact]
    public void ExecuteCreateRemote_ValidInputs_AddsRemoteAndRefreshes()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowCreateRemoteModalCommand.Execute().Subscribe();
        vm.UpdateNewRemoteNameInput("upstream");
        vm.UpdateNewRemoteUrlInput("https://github.com/upstream/repo.git");
        vm.ExecuteCreateRemoteCommand.Execute().Subscribe();

        Assert.Equal("upstream", gitService.LastAddedRemoteName);
        Assert.Equal("https://github.com/upstream/repo.git", gitService.LastAddedRemoteUrl);
        Assert.False(vm.State.IsCreateRemoteModalVisible);
    }

    [Fact]
    public void DeleteRemote_ValidName_RemovesRemoteAndRefreshes()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.DeleteRemoteCommand.Execute("origin").Subscribe();

        Assert.Equal("origin", gitService.LastRemovedRemoteName);
    }

    [Fact]
    public void FetchRemote_ValidName_FetchesSpecifiedRemote()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.FetchRemoteCommand.Execute("origin").Subscribe();

        Assert.True(gitService.FetchCalled);
        Assert.Equal("origin", gitService.LastFetchedRemoteName);
    }

    [Fact]
    public void ShowAndHideCreateBranchModal_InvokesCommands_TogglesModalVisibility()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        Assert.False(vm.State.IsCreateBranchModalVisible);

        vm.ShowCreateBranchModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCreateBranchModalVisible);

        vm.HideCreateBranchModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsCreateBranchModalVisible);
    }

    [Fact]
    public void ExecuteCreateBranch_ValidBranchName_CreatesBranchAndRefreshes()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowCreateBranchModalCommand.Execute().Subscribe();
        vm.UpdateNewBranchNameInput("feature-x");
        vm.ExecuteCreateBranchCommand.Execute().Subscribe();

        Assert.Equal("feature-x", gitService.LastCreatedBranch);
        Assert.False(vm.State.IsCreateBranchModalVisible);
    }

    [Fact]
    public void ShowAndHideCreateTagModal_InvokesCommands_TogglesModalVisibility()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        Assert.False(vm.State.IsCreateTagModalVisible);

        vm.ShowCreateTagModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCreateTagModalVisible);

        vm.HideCreateTagModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsCreateTagModalVisible);
    }

    [Fact]
    public void ExecuteCreateTag_ValidTagName_CreatesTagAndRefreshes()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowCreateTagModalCommand.Execute().Subscribe();
        vm.UpdateNewTagNameInput("v1.0.0");
        vm.ExecuteCreateTagCommand.Execute().Subscribe();

        Assert.Equal("v1.0.0", gitService.LastCreatedTag);
        Assert.False(vm.State.IsCreateTagModalVisible);
    }

    [Fact]
    public void ShowAndHideStashModal_InvokesCommands_TogglesModalVisibility()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        Assert.False(vm.State.IsStashModalVisible);

        vm.ShowStashModalCommand.Execute().Subscribe();
        Assert.True(vm.State.IsStashModalVisible);

        vm.HideStashModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsStashModalVisible);
    }

    [Fact]
    public void ExecuteCreateStash_ValidInput_CreatesStashAndRefreshes()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowStashModalCommand.Execute().Subscribe();
        vm.UpdateNewStashMessageInput("WIP changes");
        vm.ExecuteCreateStashCommand.Execute().Subscribe();

        Assert.Equal("WIP changes", gitService.LastCreatedStash);
        Assert.False(vm.State.IsStashModalVisible);
    }

    [Fact]
    public void SelectStash_SetsSelectedStashInState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        StashInfo stash = new() { Index = 1, Message = "Test stash", Sha = "1234567" };
        vm.SelectStash(stash);

        Assert.Equal(stash, vm.State.SelectedStash);
        Assert.Equal("stash@{1}", stash.Name);
    }

    [Fact]
    public void PopStash_ExecutesPopCommand()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.PopStashCommand.Execute(2).Subscribe();

        Assert.Equal(2, gitService.LastPoppedStashIndex);
    }

    [Fact]
    public void DropStash_ExecutesDropCommand()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.DropStashCommand.Execute(3).Subscribe();

        Assert.Equal(3, gitService.LastDroppedStashIndex);
    }

    [Fact]
    public void Fetch_Remote_ExecutesFetch()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.FetchCommand.Execute().Subscribe();

        Assert.True(gitService.FetchCalled);
    }

    [Fact]
    public void Pull_Remote_ExecutesPull()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.PullCommand.Execute().Subscribe();

        Assert.True(gitService.PullCalled);
    }

    [Fact]
    public void Push_Remote_ExecutesPush()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.PushCommand.Execute().Subscribe();

        Assert.True(gitService.PushCalled);
    }

    [Fact]
    public void StageAll_UnstagedFiles_StagesAllFiles()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "file1.cs", IsStaged = false });
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "file2.cs", IsStaged = false });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.StageAllCommand.Execute().Subscribe();

        Assert.Contains("file1.cs", gitService.StagedFiles);
        Assert.Contains("file2.cs", gitService.StagedFiles);
    }

    [Fact]
    public void UnstageAll_StagedFiles_UnstagesAllFiles()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "staged1.cs", IsStaged = true });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.UnstageAllCommand.Execute().Subscribe();

        Assert.Contains("staged1.cs", gitService.UnstagedFiles);
    }

    [Fact]
    public void CherryPickCommit_WithCommit_ExecutesCherryPickOnGitService()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "cherry123", Message = "Cherry test", AuthorName = "Alice" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.CherryPickCommitCommand.Execute(commit).Subscribe();

        Assert.Equal("cherry123", gitService.LastCherryPickedSha);
    }

    [Fact]
    public void RevertCommit_WithCommit_ExecutesRevertOnGitService()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "revert456", Message = "Revert test", AuthorName = "Bob" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.RevertCommitCommand.Execute(commit).Subscribe();

        Assert.Equal("revert456", gitService.LastRevertedSha);
    }

    [Fact]
    public async Task RequestResetBranch_HardReset_ShowsConfirmModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "reset789", Message = "Reset test", AuthorName = "Charlie" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        await vm.SelectCommitAsync(commit);
        vm.RequestResetBranchCommand.Execute(GitResetType.Hard).Subscribe();

        Assert.True(vm.State.IsResetConfirmModalVisible);
        Assert.Equal(GitResetType.Hard, vm.State.PendingResetType);
        Assert.Equal(commit, vm.State.PendingResetCommit);
    }

    [Fact]
    public async Task ExecuteResetBranch_ConfirmedHardReset_ExecutesResetAndHidesModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "reset789", Message = "Reset test", AuthorName = "Charlie" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        await vm.SelectCommitAsync(commit);
        vm.RequestResetBranchCommand.Execute(GitResetType.Hard).Subscribe();
        vm.ExecuteResetBranchCommand.Execute().Subscribe();

        Assert.False(vm.State.IsResetConfirmModalVisible);
        Assert.Equal("reset789", gitService.LastResetSha);
        Assert.Equal(GitResetType.Hard, gitService.LastResetType);
    }

    [Fact]
    public async Task CancelResetBranch_WhenModalVisible_HidesModalAndClearsPending()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "reset789", Message = "Reset test", AuthorName = "Charlie" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        await vm.SelectCommitAsync(commit);
        vm.RequestResetBranchCommand.Execute(GitResetType.Hard).Subscribe();
        vm.CancelResetBranchCommand.Execute().Subscribe();

        Assert.False(vm.State.IsResetConfirmModalVisible);
        Assert.Null(vm.State.PendingResetCommit);
    }

    [Fact]
    public async Task RequestResetBranch_SoftReset_ExecutesDirectlyWithoutModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "resetsoft1", Message = "Soft test", AuthorName = "David" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        await vm.SelectCommitAsync(commit);
        vm.RequestResetBranchCommand.Execute(GitResetType.Soft).Subscribe();

        Assert.False(vm.State.IsResetConfirmModalVisible);
        Assert.Equal("resetsoft1", gitService.LastResetSha);
        Assert.Equal(GitResetType.Soft, gitService.LastResetType);
    }

    [Fact]
    public void CreateBranchFromCommit_WithCommit_ShowsModalAndPreparesInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "branchtarget", Message = "Target commit", AuthorName = "Eve" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.CreateBranchFromCommitCommand.Execute(commit).Subscribe();

        Assert.True(vm.State.IsCreateBranchModalVisible);
        Assert.Equal(commit, vm.State.SelectedCommit);
        Assert.Equal(string.Empty, vm.State.NewBranchNameInput);
    }

    [Fact]
    public void CreateTagFromCommit_WithCommit_ShowsModalAndPreparesInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary commit = new() { Sha = "tagtarget", Message = "Tag commit", AuthorName = "Frank" };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.CreateTagFromCommitCommand.Execute(commit).Subscribe();

        Assert.True(vm.State.IsCreateTagModalVisible);
        Assert.Equal(commit, vm.State.SelectedCommit);
        Assert.Equal(string.Empty, vm.State.NewTagNameInput);
    }

    [Fact]
    public void ShowMergeModal_SetsDefaultSelectedBranchAndFastForwardMode()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "main", IsCurrentHead = true });
        gitService.Branches.Add(new BranchInfo { Name = "feature-x", IsCurrentHead = false });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowMergeModalCommand.Execute(null!).Subscribe();

        Assert.True(vm.State.IsMergeModalVisible);
        Assert.Equal("feature-x", vm.State.SelectedMergeBranch);
        Assert.Equal(GitMergeFastForwardMode.FastForward, vm.State.MergeFastForwardMode);
    }

    [Fact]
    public void ExecuteMerge_CallsGitServiceWithSelectedBranchAndMode()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "main", IsCurrentHead = true });
        gitService.Branches.Add(new BranchInfo { Name = "feature-y", IsCurrentHead = false });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowMergeModalCommand.Execute("feature-y").Subscribe();
        vm.SetMergeFastForwardMode(GitMergeFastForwardMode.NoFastForward);
        vm.ExecuteMergeCommand.Execute().Subscribe();

        Assert.False(vm.State.IsMergeModalVisible);
        Assert.Equal("feature-y", gitService.LastMergedBranch);
        Assert.Equal(GitMergeFastForwardMode.NoFastForward, gitService.LastMergeMode);
    }

    [Fact]
    public void StartInteractiveRebase_LoadsStepsAndOpensModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary target = new() { Sha = "upstream123", ShortSha = "upst123", Message = "Base commit" };
        gitService.StepsToReturn.Add(new RebaseStep { CommitSha = "sha1", ShortSha = "s1", OriginalMessage = "Commit 1", NewMessage = "Commit 1" });
        gitService.StepsToReturn.Add(new RebaseStep { CommitSha = "sha2", ShortSha = "s2", OriginalMessage = "Commit 2", NewMessage = "Commit 2" });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.StartInteractiveRebaseCommand.Execute(target).Subscribe();

        Assert.True(vm.State.IsInteractiveRebaseModalVisible);
        Assert.Equal("upstream123", vm.State.RebaseUpstreamSha);
        Assert.Equal("upst123", vm.State.RebaseUpstreamShortSha);
        Assert.Equal(2, vm.State.RebaseSteps.Count);
        Assert.Equal("sha1", vm.State.SelectedRebaseStep?.CommitSha);
    }

    [Fact]
    public void MoveRebaseStepUpAndDown_ReordersStepsCorrectly()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary target = new() { Sha = "baseSha" };
        var step1 = new RebaseStep { CommitSha = "sha1", NewMessage = "C1" };
        var step2 = new RebaseStep { CommitSha = "sha2", NewMessage = "C2" };
        var step3 = new RebaseStep { CommitSha = "sha3", NewMessage = "C3" };
        gitService.StepsToReturn.AddRange(new[] { step1, step2, step3 });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.StartInteractiveRebaseCommand.Execute(target).Subscribe();
        vm.SelectRebaseStep(step2);

        vm.MoveRebaseStepUpCommand.Execute(null!).Subscribe();
        Assert.Equal("sha2", vm.State.RebaseSteps[0].CommitSha);
        Assert.Equal("sha1", vm.State.RebaseSteps[1].CommitSha);
        Assert.Equal("sha3", vm.State.RebaseSteps[2].CommitSha);

        vm.MoveRebaseStepDownCommand.Execute(null!).Subscribe();
        Assert.Equal("sha1", vm.State.RebaseSteps[0].CommitSha);
        Assert.Equal("sha2", vm.State.RebaseSteps[1].CommitSha);
        Assert.Equal("sha3", vm.State.RebaseSteps[2].CommitSha);
    }

    [Fact]
    public void ChangeRebaseStepActionAndMessage_ModifiesStepInState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary target = new() { Sha = "baseSha" };
        var step = new RebaseStep { CommitSha = "stepSha", Action = RebaseStepAction.Pick, OriginalMessage = "Orig", NewMessage = "Orig" };
        gitService.StepsToReturn.Add(step);
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.StartInteractiveRebaseCommand.Execute(target).Subscribe();
        vm.ChangeRebaseStepAction(vm.State.RebaseSteps[0], RebaseStepAction.Squash);
        vm.UpdateRebaseStepMessage(vm.State.RebaseSteps[0], "Squashed commit message");

        Assert.Equal(RebaseStepAction.Squash, vm.State.RebaseSteps[0].Action);
        Assert.Equal("Squashed commit message", vm.State.RebaseSteps[0].NewMessage);
    }

    [Fact]
    public void ExecuteInteractiveRebase_CallsGitServiceWithUpdatedSteps()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitSummary target = new() { Sha = "upstreamBase" };
        var step = new RebaseStep { CommitSha = "stepSha", Action = RebaseStepAction.Reword, NewMessage = "Reworded" };
        gitService.StepsToReturn.Add(step);
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.StartInteractiveRebaseCommand.Execute(target).Subscribe();
        vm.ExecuteInteractiveRebaseCommand.Execute().Subscribe();

        Assert.False(vm.State.IsInteractiveRebaseModalVisible);
        Assert.Equal("upstreamBase", gitService.LastRebaseUpstreamSha);
        Assert.NotNull(gitService.LastExecutedRebaseSteps);
        Assert.Single(gitService.LastExecutedRebaseSteps!);
        Assert.Equal(RebaseStepAction.Reword, gitService.LastExecutedRebaseSteps![0].Action);
    }

    [Fact]
    public void AbortAndContinueOperation_DispatchesToCorrectGitMethods()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.OperationStatusToReturn = new RepositoryOperationStatus
        {
            CurrentOperation = RepositoryOperationType.Merge,
            HasConflicts = true
        };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        vm.LoadRepositoryDataCommand.Execute().Subscribe();

        Assert.Equal(RepositoryOperationType.Merge, vm.State.OperationStatus.CurrentOperation);
        Assert.True(vm.State.OperationStatus.HasConflicts);

        vm.ContinueOperationCommand.Execute().Subscribe();
        Assert.True(gitService.ContinueMergeCalled);

        vm.AbortOperationCommand.Execute().Subscribe();
        Assert.True(gitService.AbortMergeCalled);

        gitService.OperationStatusToReturn = new RepositoryOperationStatus
        {
            CurrentOperation = RepositoryOperationType.Rebase,
            HasConflicts = false
        };
        vm.LoadRepositoryDataCommand.Execute().Subscribe();

        vm.ContinueOperationCommand.Execute().Subscribe();
        Assert.True(gitService.ContinueRebaseCalled);

        vm.AbortOperationCommand.Execute().Subscribe();
        Assert.True(gitService.AbortRebaseCalled);
    }

    [Fact]
    public void ToggleAmendCommit_WhenEnabled_LoadsPreviousCommitMessageAndSetsAmendFlag()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "c1", Message = "First commit message" });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ToggleAmendCommit(true);

        Assert.True(vm.State.IsAmendCommit);
        Assert.Equal("First commit message", vm.State.CommitMessageInput);
    }

    [Fact]
    public void ToggleAmendCommit_WhenDisabled_ClearsAmendFlag()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ToggleAmendCommit(true);
        Assert.True(vm.State.IsAmendCommit);

        vm.ToggleAmendCommit(false);
        Assert.False(vm.State.IsAmendCommit);
    }

    [Fact]
    public void ExecuteCommit_WhenAmendEnabled_PassesAmendFlagAndResetsState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "c1", Message = "Original commit" });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ToggleAmendCommit(true);
        vm.UpdateCommitMessageInput("Amended commit message");
        vm.ExecuteCommitCommand.Execute().Subscribe();

        Assert.True(gitService.LastCommitWasAmend);
        Assert.Equal("Amended commit message", gitService.LastCommitMessage);
        Assert.False(vm.State.IsAmendCommit);
        Assert.Equal(string.Empty, vm.State.CommitMessageInput);
    }

    [Fact]
    public void OpenFileManagerCommand_InvokesSystemLauncherWithRepositoryPath()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockSystemLauncherService launcher = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, launcher);

        vm.OpenFileManagerCommand.Execute().Subscribe();

        Assert.Equal("/repo/path", launcher.LastFileManagerPath);
    }

    [Fact]
    public void OpenTerminalCommand_InvokesSystemLauncherWithRepositoryPath()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockSystemLauncherService launcher = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, launcher);

        vm.OpenTerminalCommand.Execute().Subscribe();

        Assert.Equal("/repo/path", launcher.LastTerminalPath);
    }

    [Fact]
    public void OpenEditorCommand_InvokesSystemLauncherWithRepositoryPath()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockSystemLauncherService launcher = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, launcher);

        vm.OpenEditorCommand.Execute().Subscribe();

        Assert.Equal("/repo/path", launcher.LastEditorPath);
    }

    [Fact]
    public void FetchCommand_WhenAuthFails_OpensCredentialsModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.ShouldThrowAuthOnRemote = true;
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.FetchCommand.Execute().Subscribe();

        Assert.True(vm.State.IsCredentialsModalVisible);
        Assert.Equal(RemoteOperationPending.Fetch, vm.State.PendingRemoteOperation);
        Assert.Equal(GitCredentialType.Https, vm.State.CredentialsAuthType);
    }

    [Fact]
    public void SubmitCredentialsCommand_WithHttpsAuth_PassesCredentialsAndClosesModal()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.ShouldThrowAuthOnRemote = true;
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.FetchCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCredentialsModalVisible);

        vm.SetCredentialsAuthType(GitCredentialType.Https);
        vm.UpdateCredentialsUsernameInput("alice");
        vm.UpdateCredentialsPasswordInput("pat_token_xyz");

        gitService.ShouldThrowAuthOnRemote = false;
        vm.SubmitCredentialsCommand.Execute().Subscribe();

        Assert.False(vm.State.IsCredentialsModalVisible);
        Assert.Equal(RemoteOperationPending.None, vm.State.PendingRemoteOperation);
        Assert.NotNull(gitService.LastCredentialsUsed);
        Assert.Equal(GitCredentialType.Https, gitService.LastCredentialsUsed!.Type);
        Assert.Equal("alice", gitService.LastCredentialsUsed.Username);
        Assert.Equal("pat_token_xyz", gitService.LastCredentialsUsed.PasswordOrToken);
    }

    [Fact]
    public void SubmitCredentialsCommand_WithSshAuth_PassesPassphrase()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.ShouldThrowAuthOnRemote = true;
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.PushCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCredentialsModalVisible);
        Assert.Equal(RemoteOperationPending.Push, vm.State.PendingRemoteOperation);

        vm.SetCredentialsAuthType(GitCredentialType.SshKeyPassphrase);
        vm.UpdateCredentialsPasswordInput("ssh_secret_passphrase");

        gitService.ShouldThrowAuthOnRemote = false;
        vm.SubmitCredentialsCommand.Execute().Subscribe();

        Assert.False(vm.State.IsCredentialsModalVisible);
        Assert.NotNull(gitService.LastCredentialsUsed);
        Assert.Equal(GitCredentialType.SshKeyPassphrase, gitService.LastCredentialsUsed!.Type);
        Assert.Equal("ssh_secret_passphrase", gitService.LastCredentialsUsed.Passphrase);
    }

    [Fact]
    public void CancelCredentialsCommand_ClosesModalAndClearsPendingOperation()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.ShouldThrowAuthOnRemote = true;
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.PullCommand.Execute().Subscribe();
        Assert.True(vm.State.IsCredentialsModalVisible);

        vm.CancelCredentialsCommand.Execute().Subscribe();

        Assert.False(vm.State.IsCredentialsModalVisible);
        Assert.Equal(RemoteOperationPending.None, vm.State.PendingRemoteOperation);
    }

    [Fact]
    public void SubmitCredentialsCommand_WhenFailsAgain_RetainsModalAndSetsErrorMessage()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.AlwaysThrowAuthOnRemote = true;
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.FetchCommand.Execute().Subscribe();
        vm.SubmitCredentialsCommand.Execute().Subscribe();

        Assert.True(vm.State.IsCredentialsModalVisible);
        Assert.False(string.IsNullOrWhiteSpace(vm.State.CredentialsErrorMessage));
    }

    private class TestProfileRepository : IProfileRepository
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

    [Fact]
    public async Task SelectProfileCommand_SetsActiveProfileAndSavesMapping()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@example.com" };
        GitProfileCredentials creds = new() { Id = 1, Username = "alicegh", AccessToken = "token" };
        GitProfile profile = new() { Id = 5, Name = "AliceProfile", AuthorId = 1, CredentialsId = 1, Author = author, Credentials = creds };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        Assert.NotNull(vm.State.ActiveProfile);
        Assert.Equal("AliceProfile", vm.State.ActiveProfile.Name);
        Assert.True(profileRepo.Mappings.ContainsKey("/my/repo"));
        Assert.Equal(5, profileRepo.Mappings["/my/repo"]);
    }

    [Fact]
    public async Task PushCommand_WhenActiveProfileHasCredentials_AutomaticallyPassesCredentials()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@example.com" };
        GitProfileCredentials creds = new() { Id = 1, Username = "alicegh", AccessToken = "ghp_secure_access_token_999" };
        GitProfile profile = new() { Id = 7, Name = "AliceProfile", AuthorId = 1, CredentialsId = 1, Author = author, Credentials = creds };
        profileRepo.Profiles.Add(profile);
        profileRepo.Mappings["/my/repo"] = 7;

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        await vm.PushCommand.Execute().FirstAsync();

        Assert.True(gitService.PushCalled);
        Assert.NotNull(gitService.LastCredentialsUsed);
        Assert.Equal("alicegh", gitService.LastCredentialsUsed.Username);
        Assert.Equal("ghp_secure_access_token_999", gitService.LastCredentialsUsed.PasswordOrToken);
        Assert.Equal(GitCredentialType.Https, gitService.LastCredentialsUsed.Type);
    }

    [Fact]
    public async Task CreateCommitCommand_WhenActiveProfileHasAuthor_UsesAuthorForCommitSignature()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 3, Name = "Bob Dev", Email = "bob@company.com" };
        GitProfile profile = new() { Id = 9, Name = "WorkProfile", AuthorId = 3, Author = author };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        vm.UpdateCommitMessageInput("feat: add feature");
        await vm.ExecuteCommitCommand.Execute().FirstAsync();

        Assert.Equal("feat: add feature", gitService.LastCommitMessage);
        Assert.Equal("Bob Dev", gitService.LastCommitAuthorName);
        Assert.Equal("bob@company.com", gitService.LastCommitAuthorEmail);
    }

    private class TestRecentRepositoriesRepository : IRecentRepositoriesRepository
    {
        public List<RecentRepository> Items { get; } = new();

        public Task<IReadOnlyList<RecentRepository>> GetRecentRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentRepository>>(Items);
        }

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

        public Task TogglePinRecentRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<RecentRepository?> GetRepositoryByPathAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.Path == repositoryPath));
        }

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

        public Task<IReadOnlyList<RecentRepository>> GetFavoriteRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RecentRepository> favorites = Items
                .Where(x => x.IsFavorite)
                .OrderByDescending(x => x.LastOpenedAtUtc)
                .ToList();
            return Task.FromResult(favorites);
        }

        public Task<RecentRepository?> GetMostRecentNonFavoriteRepositoryAsync(CancellationToken cancellationToken = default)
        {
            RecentRepository? mostRecent = Items
                .Where(x => !x.IsFavorite)
                .OrderByDescending(x => x.LastOpenedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(mostRecent);
        }
    }

    [Fact]
    public async Task ToggleFavoritesDockCommand_WhenExecuted_TogglesIsFavoritesDockOpenAndRefreshesData()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/fav",
            Name = "fav",
            IsFavorite = true,
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/recent",
            Name = "recent",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        CommitLogViewModel vm = new(screen, "/repo/fav", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsFavoritesDockOpen);

        await vm.ToggleFavoritesDockCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsFavoritesDockOpen);
        Assert.Single(vm.State.FavoriteRepositories);
        Assert.Equal("/repo/fav", vm.State.FavoriteRepositories[0].Path);
        Assert.NotNull(vm.State.RecentNonFavoriteRepository);
        Assert.Equal("/repo/recent", vm.State.RecentNonFavoriteRepository.Path);

        vm.CloseFavoritesDockCommand.Execute().Subscribe();

        Assert.False(vm.State.IsFavoritesDockOpen);
    }

    [Fact]
    public void ShowAddFavoriteModalCommand_And_HideAddFavoriteModalCommand_TogglesModalAndInitializesInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();

        Assert.True(vm.State.IsAddFavoriteModalVisible);
        Assert.Equal("/repo/my-project", vm.State.TargetFavoritePath);
        Assert.Equal("my-project", vm.State.NewFavoriteNameInput);

        vm.UpdateNewFavoriteNameInput("Custom Project Name");
        Assert.Equal("Custom Project Name", vm.State.NewFavoriteNameInput);

        vm.HideAddFavoriteModalCommand.Execute().Subscribe();
        Assert.False(vm.State.IsAddFavoriteModalVisible);
    }

    [Fact]
    public async Task ConfirmAddFavoriteCommand_WhenCurrentRepositoryFavorited_UpdatesRepositoryNameAndFavoriteList()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/my-project",
            Name = "my-project",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        vm.UpdateNewFavoriteNameInput("My Super Project");

        await vm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsAddFavoriteModalVisible);
        Assert.Equal("My Super Project", vm.State.RepositoryName);
        Assert.Single(vm.State.FavoriteRepositories);
        Assert.Equal("My Super Project", vm.State.FavoriteRepositories[0].DisplayName);
        Assert.True(vm.State.FavoriteRepositories[0].IsFavorite);
    }

    [Fact]
    public async Task RefreshFavoritesDockData_WhenFavoritesContainGroups_BuildsFavoriteGroupsAndUngroupedLists()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent1",
            Name = "agent1",
            CustomName = "Agent 1",
            IsFavorite = true,
            GroupName = "Project Fleet",
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent2",
            Name = "agent2",
            CustomName = "Agent 2",
            IsFavorite = true,
            GroupName = "Project Fleet",
            LastOpenedAtUtc = DateTime.UtcNow
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/solo",
            Name = "solo",
            CustomName = "Solo Project",
            IsFavorite = true,
            GroupName = null,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("Project Fleet", vm.State.FavoriteGroups[0].Name);
        Assert.Equal(2, vm.State.FavoriteGroups[0].Repositories.Count);
        Assert.Single(vm.State.UngroupedFavoriteRepositories);
        Assert.Equal("Solo Project", vm.State.UngroupedFavoriteRepositories[0].DisplayName);
    }

    [Fact]
    public async Task ExistingGroupNames_WhenFavoritesContainDistinctGroups_ReturnsSortedUniqueGroupNames()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r1",
            Name = "r1",
            IsFavorite = true,
            GroupName = "Zeta Project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r2",
            Name = "r2",
            IsFavorite = true,
            GroupName = "alpha project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r3",
            Name = "r3",
            IsFavorite = true,
            GroupName = "Alpha Project"
        });
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/r4",
            Name = "r4",
            IsFavorite = true,
            GroupName = "   "
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.Equal(2, vm.State.ExistingGroupNames.Count);
        Assert.Equal("alpha project", vm.State.ExistingGroupNames[0]);
        Assert.Equal("Zeta Project", vm.State.ExistingGroupNames[1]);
    }

    [Fact]
    public async Task ToggleFavoriteGroupCommand_WhenGroupIsExpanded_TogglesIsExpandedState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/sub1",
            Name = "sub1",
            IsFavorite = true,
            GroupName = "Backend Team",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        Assert.True(vm.State.FavoriteGroups[0].IsExpanded);

        vm.ToggleFavoriteGroupCommand.Execute("Backend Team").Subscribe();

        Assert.False(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.Contains("Backend Team", vm.State.CollapsedFavoriteGroupNames);

        vm.ToggleFavoriteGroupCommand.Execute("Backend Team").Subscribe();

        Assert.True(vm.State.FavoriteGroups[0].IsExpanded);
        Assert.DoesNotContain("Backend Team", vm.State.CollapsedFavoriteGroupNames);
    }

    [Fact]
    public async Task ShowEditFavoriteModalCommand_WhenExistingFavoriteSelected_PopulatesModalInputsWithGroup()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/existing",
            Name = "existing",
            CustomName = "Custom Existing",
            IsFavorite = true,
            GroupName = "Core Group",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/current", gitService, recentRepositoriesRepository: repo);
        await vm.RefreshFavoritesDockDataAsync();

        vm.ShowEditFavoriteModalCommand.Execute("/repo/existing").Subscribe();

        Assert.True(vm.State.IsAddFavoriteModalVisible);
        Assert.True(vm.State.IsEditingExistingFavorite);
        Assert.Equal("/repo/existing", vm.State.TargetFavoritePath);
        Assert.Equal("Custom Existing", vm.State.NewFavoriteNameInput);
        Assert.Equal("Core Group", vm.State.NewFavoriteGroupInput);
    }

    [Fact]
    public async Task ConfirmAddFavoriteCommand_WithGroupNameProvided_PersistsGroupAndCustomName()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/agent-repo",
            Name = "agent-repo",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/agent-repo", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        vm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        vm.UpdateNewFavoriteNameInput("Agent Worker A");
        vm.UpdateNewFavoriteGroupInput("Cluster X");

        await vm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.False(vm.State.IsAddFavoriteModalVisible);
        RecentRepository? updated = repo.Items.FirstOrDefault(x => x.Path == "/repo/agent-repo");
        Assert.NotNull(updated);
        Assert.True(updated.IsFavorite);
        Assert.Equal("Agent Worker A", updated.CustomName);
        Assert.Equal("Cluster X", updated.GroupName);
        Assert.Single(vm.State.FavoriteGroups);
        Assert.Equal("Cluster X", vm.State.FavoriteGroups[0].Name);
    }

    [Fact]
    public async Task RemoveFromFavoritesCommand_WhenCurrentRepositoryUnfavorited_RevertsRepositoryNameToDirectoryName()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        repo.Items.Add(new RecentRepository
        {
            Path = "/repo/my-project",
            Name = "my-project",
            IsFavorite = true,
            CustomName = "Custom Name",
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel vm = new(screen, "/repo/my-project", gitService, recentRepositoriesRepository: repo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Equal("Custom Name", vm.State.RepositoryName);

        await vm.RemoveFromFavoritesCommand.Execute("/repo/my-project").FirstAsync();

        Assert.Equal("my-project", vm.State.RepositoryName);
        Assert.Empty(vm.State.FavoriteRepositories);
        Assert.NotNull(vm.State.RecentNonFavoriteRepository);
        Assert.Equal("/repo/my-project", vm.State.RecentNonFavoriteRepository.Path);
    }

    [Fact]
    public void OpenRepositoryFromDockCommand_WithDifferentValidRepository_NavigatesToNewCommitLog()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestRecentRepositoriesRepository repo = new();
        CommitLogViewModel vm = new(screen, "/repo/first", gitService, recentRepositoriesRepository: repo);

        vm.ToggleFavoritesDockCommand.Execute().Subscribe();
        Assert.True(vm.State.IsFavoritesDockOpen);

        vm.OpenRepositoryFromDockCommand.Execute("/repo/second").Subscribe();

        Assert.False(vm.State.IsFavoritesDockOpen);
        Assert.IsType<CommitLogViewModel>(screen.Router.NavigationStack.Last());
        CommitLogViewModel nextVm = (CommitLogViewModel)screen.Router.NavigationStack.Last();
        Assert.Equal("/repo/second", nextVm.State.RepositoryPath);
    }

    private class MockAiCommitMessageService : IAiCommitMessageService
    {
        public string ProposalToReturn { get; set; } = "feat(core): add feature";
        public Exception? ExceptionToThrow { get; set; }
        public string? LastRepositoryPath { get; private set; }

        public Task<string> GenerateCommitMessageAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            LastRepositoryPath = repositoryPath;
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(ProposalToReturn);
        }
    }

    private class MockAiConfigurationRepository : IAiConfigurationRepository
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

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenSuccessful_SetsCommitMessageInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ProposalToReturn = "feat(auth): add login flow" };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Equal("feat(auth): add login flow", vm.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Null(vm.State.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenProposalSubjectExceedsLimit_TruncatesSubjectToHardLimit()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        string longSubject = new('A', 85);
        MockAiCommitMessageService aiService = new() { ProposalToReturn = longSubject };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Equal(72, vm.State.CommitMessageInput.Length);
        Assert.Equal(new string('A', 72), vm.State.CommitMessageInput);
        Assert.Equal(0, vm.State.CommitSubjectRemainingChars);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenProposalHasBody_ExtractsOnlySubjectUnderLimit()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        string longSubject = new('X', 80);
        string body = "\n\nThis is the detailed commit description.";
        MockAiCommitMessageService aiService = new() { ProposalToReturn = longSubject + body };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        string expected = new string('X', 72);
        Assert.Equal(expected, vm.State.CommitMessageInput);
        Assert.Equal(0, vm.State.CommitSubjectRemainingChars);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenFailsWithApiKeyError_SetsAiErrorMessageAndSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ExceptionToThrow = new InvalidOperationException("OpenRouter API key is not configured.") };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Empty(vm.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Equal("OpenRouter API key is not configured.", vm.State.AiErrorMessage);
        Assert.True(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenFailsWithNetworkError_SetsAiErrorMessageWithoutSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ExceptionToThrow = new InvalidOperationException("Failed to connect to OpenRouter API.") };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Empty(vm.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Equal("Failed to connect to OpenRouter API.", vm.State.AiErrorMessage);
        Assert.False(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public void DismissAiErrorCommand_ClearsAiErrorMessageAndSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/test", gitService);

        vm.UpdateAiApiKeyInput("sk-test");
        vm.DismissAiErrorCommand.Execute().Subscribe();

        Assert.Null(vm.State.AiErrorMessage);
        Assert.False(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public async Task ShowSettingsModal_LoadsAiConfigurationAndProjectDescription()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiConfigurationRepository aiRepo = new()
        {
            ConfigurationToReturn = new AiConfiguration
            {
                ApiKey = "sk-secret-123",
                Model = "anthropic/claude-3.5-sonnet",
                CommitStyle = "gitmoji",
                TokenLimit = 6000
            }
        };
        aiRepo.Descriptions["/repo/test"] = "Test repository project";

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiConfigurationRepository: aiRepo);

        await vm.ShowSettingsModalCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsSettingsModalVisible);
        Assert.Equal("sk-secret-123", vm.State.AiApiKeyInput);
        Assert.Equal("anthropic/claude-3.5-sonnet", vm.State.AiModelInput);
        Assert.Equal("gitmoji", vm.State.AiCommitStyleInput);
        Assert.Equal("6000", vm.State.AiTokenLimitInput);
        Assert.Equal("Test repository project", vm.State.AiProjectDescriptionInput);
    }

    [Fact]
    public async Task ExecuteSaveSettings_SavesAiConfigurationAndProjectDescription()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiConfigurationRepository aiRepo = new();

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiConfigurationRepository: aiRepo);

        vm.UpdateAiApiKeyInput("sk-new-key");
        vm.UpdateAiModelInput("google/gemini-2.5-flash");
        vm.UpdateAiCommitStyleInput("Conventional Commits");
        vm.UpdateAiTokenLimitInput("5000");
        vm.UpdateAiProjectDescriptionInput("Updated description");

        await vm.ExecuteSaveSettingsCommand.Execute().FirstAsync();

        Assert.NotNull(aiRepo.LastSavedConfiguration);
        Assert.Equal("sk-new-key", aiRepo.LastSavedConfiguration.ApiKey);
        Assert.Equal("google/gemini-2.5-flash", aiRepo.LastSavedConfiguration.Model);
        Assert.Equal("Conventional Commits", aiRepo.LastSavedConfiguration.CommitStyle);
        Assert.Equal(5000, aiRepo.LastSavedConfiguration.TokenLimit);

        Assert.NotNull(aiRepo.LastSavedDescription);
        Assert.Equal("/repo/test", aiRepo.LastSavedDescription.Value.Path);
        Assert.Equal("Updated description", aiRepo.LastSavedDescription.Value.Desc);
    }

    [Theory]
    [InlineData("", 72)]
    [InlineData("feat: short title", 55)]
    [InlineData("feat: exactly seventy-two characters long title for conventional commit!", 0)]
    [InlineData("feat: a commit title that is seventy-five characters long for test purpose!", -3)]
    [InlineData("feat: title line\n\nLong body line with lots of details and explanations.", 56)]
    public void CommitSubjectRemainingChars_CalculatesFirstLineRemainingAccurately(string input, int expectedRemaining)
    {
        CommitLogState state = new() { CommitMessageInput = input };
        Assert.Equal(expectedRemaining, state.CommitSubjectRemainingChars);
    }

    [Fact]
    public async Task SelectCommitAsync_WithWipCommit_SetsWipDetailAndSwitchesToWorkingTreeTab()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "modified.cs", Status = FileChangeStatus.Modified });

        CommitSummary wipCommit = new()
        {
            Sha = "WIP",
            ShortSha = string.Empty,
            Message = "// WIP",
            IsWip = true,
            UncommittedFilesCount = 1
        };
        gitService.Commits.Add(wipCommit);

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        await vm.SelectCommitAsync(wipCommit);

        Assert.NotNull(vm.State.SelectedCommit);
        Assert.True(vm.State.SelectedCommit.IsWip);
        Assert.NotNull(vm.State.SelectedCommitDetail);
        Assert.Equal("WIP", vm.State.SelectedCommitDetail.Sha);
        Assert.Single(vm.State.SelectedCommitDetail.ChangedFiles);
        Assert.Equal("modified.cs", vm.State.SelectedCommitDetail.ChangedFiles[0].Path);
        Assert.Equal(CommitLogSidebarTab.WorkingTree, vm.State.ActiveSidebarTab);
    }

    [Fact]
    public async Task AutoFetch_WhenRemotesExist_InvokesFetchSilently()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowCreateRemoteModalCommand.Execute().Subscribe();
        vm.UpdateNewRemoteNameInput("origin");
        vm.UpdateNewRemoteUrlInput("https://example.com/repo.git");
        vm.ExecuteCreateRemoteCommand.Execute().Subscribe();

        await vm.AutoFetchCommand.Execute().FirstAsync();

        Assert.True(gitService.FetchCalled);
        Assert.Null(vm.State.ErrorMessage);
    }

    [Fact]
    public async Task AutoFetch_WhenFetchThrows_SuppressesError()
    {
        MockScreen screen = new();
        MockGitService gitService = new() { AlwaysThrowAuthOnRemote = true };
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ShowCreateRemoteModalCommand.Execute().Subscribe();
        vm.UpdateNewRemoteNameInput("origin");
        vm.UpdateNewRemoteUrlInput("https://example.com/repo.git");
        vm.ExecuteCreateRemoteCommand.Execute().Subscribe();

        await vm.AutoFetchCommand.Execute().FirstAsync();

        Assert.Null(vm.State.ErrorMessage);
    }

    [Fact]
    public void LoadRepositoryData_WhenConflictedFilesPresent_SwitchesToConflictMode()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "conflicted.txt", Status = FileChangeStatus.Conflicted, IsStaged = false });
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "resolved.txt", Status = FileChangeStatus.Modified, IsStaged = true });
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "normal_unstaged.txt", Status = FileChangeStatus.Modified, IsStaged = false });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        Assert.True(vm.State.IsInConflictMode);
        Assert.Single(vm.State.ConflictedFiles);
        Assert.Equal("conflicted.txt", vm.State.ConflictedFiles[0].Path);
        Assert.Single(vm.State.ResolvedFiles);
        Assert.Equal("resolved.txt", vm.State.ResolvedFiles[0].Path);
        Assert.Single(vm.State.UnstagedChanges);
        Assert.Equal("normal_unstaged.txt", vm.State.UnstagedChanges[0].Path);
    }

    [Fact]
    public async Task MarkAllResolved_WhenInvoked_CallsGitServiceAndRefreshesChanges()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.WorkingChanges.Add(new WorkingTreeChange { Path = "conflicted.txt", Status = FileChangeStatus.Conflicted, IsStaged = false });

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        Assert.True(vm.State.IsInConflictMode);

        await vm.MarkAllResolvedCommand.Execute().FirstAsync();

        Assert.True(gitService.MarkAllConflictsResolvedCalled);
    }

    [Fact]
    public void OpenConflictResolver_WhenFileProvided_NavigatesToConflictResolverScreen()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        WorkingTreeChange change = new() { Path = "file_with_conflict.txt", Status = FileChangeStatus.Conflicted };

        vm.OpenConflictResolver(change);

        Assert.Single(screen.Router.NavigationStack);
        Assert.IsType<GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver.ConflictResolverViewModel>(screen.Router.NavigationStack[0]);
    }
}

