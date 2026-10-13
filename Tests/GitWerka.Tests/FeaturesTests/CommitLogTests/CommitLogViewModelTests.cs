namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using ReactiveUI;
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

        public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default)
        {
            LastCommitMessage = message;
            LastCommitWasAmend = amendPreviousCommit;
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

        public Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RemoteInfo>>(Array.Empty<RemoteInfo>());

        public Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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
}
