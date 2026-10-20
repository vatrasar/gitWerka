namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using Xunit;

public class CommitLogWorkingTreeAndHistoryTests
{
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
}
