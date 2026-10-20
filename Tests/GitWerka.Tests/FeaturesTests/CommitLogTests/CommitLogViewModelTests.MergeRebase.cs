namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using Xunit;

public class CommitLogMergeRebaseTests
{
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
}
