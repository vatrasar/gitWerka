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

public class CommitLogBranchesAndRemotesTests
{
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

    [Fact]
    public async Task LoadRepositoryData_PopulatesBranchListItemsWithAllBranchesOptionAsDefault()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "main", IsCurrentHead = true });
        gitService.Branches.Add(new BranchInfo { Name = "feature", IsCurrentHead = false });

        CommitLogViewModel vm = new(screen, "/repo/test", gitService);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Null(vm.State.SelectedBranchName);
        Assert.Equal(3, vm.State.BranchListItems.Count);
        Assert.True(vm.State.BranchListItems[0].IsAllBranches);
        Assert.Equal("All branches", vm.State.BranchListItems[0].DisplayName);
        Assert.Equal("main", vm.State.BranchListItems[1].Name);
        Assert.True(vm.State.BranchListItems[1].IsCurrentHead);
        Assert.Equal("feature", vm.State.BranchListItems[2].Name);
    }

    [Fact]
    public async Task LoadRepositoryData_WhenBranchesExceedLimit_SetsRecentlyModifiedBranchesLabel()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository appSettingsRepo = new()
        {
            Settings = new ApplicationSettings { MaxVisibleBranches = 3 }
        };

        for (int i = 1; i <= 5; i++)
        {
            gitService.Branches.Add(new BranchInfo { Name = $"branch-{i}" });
        }

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, applicationSettingsRepository: appSettingsRepo);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.True(vm.State.BranchListItems[0].IsAllBranches);
        Assert.Equal("Recently modified (3)", vm.State.BranchListItems[0].DisplayName);
    }

    [Fact]
    public async Task SelectBranchAsync_FiltersCommitsToSelectedBranch()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "main" });
        gitService.Branches.Add(new BranchInfo { Name = "feature" });

        var allCommits = new List<CommitSummary>
        {
            new CommitSummary { Sha = "c1", MessageShort = "Commit 1" },
            new CommitSummary { Sha = "c2", MessageShort = "Commit 2" }
        };
        var featureCommits = new List<CommitSummary>
        {
            new CommitSummary { Sha = "c1", MessageShort = "Commit 1" }
        };

        gitService.Commits.AddRange(allCommits);
        gitService.BranchCommits["feature"] = featureCommits;

        CommitLogViewModel vm = new(screen, "/repo/test", gitService);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Equal(2, vm.State.AllCommits.Count);

        var featureItem = vm.State.BranchListItems.First(b => b.Name == "feature");
        await vm.SelectBranchAsync(featureItem);

        Assert.Equal("feature", vm.State.SelectedBranchName);
        Assert.Equal("feature", gitService.LastRequestedBranchName);
        Assert.Single(vm.State.AllCommits);
        Assert.Equal("c1", vm.State.AllCommits[0].Sha);
    }

    [Fact]
    public async Task SelectBranchAsync_WithAllBranchesItem_RestoresAllCommits()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Branches.Add(new BranchInfo { Name = "main" });
        gitService.Branches.Add(new BranchInfo { Name = "feature" });

        var allCommits = new List<CommitSummary>
        {
            new CommitSummary { Sha = "c1", MessageShort = "Commit 1" },
            new CommitSummary { Sha = "c2", MessageShort = "Commit 2" }
        };
        var featureCommits = new List<CommitSummary>
        {
            new CommitSummary { Sha = "c1", MessageShort = "Commit 1" }
        };

        gitService.Commits.AddRange(allCommits);
        gitService.BranchCommits["feature"] = featureCommits;

        CommitLogViewModel vm = new(screen, "/repo/test", gitService);
        await vm.LoadRepositoryDataCommand.Execute().FirstAsync();

        var featureItem = vm.State.BranchListItems.First(b => b.Name == "feature");
        await vm.SelectBranchAsync(featureItem);
        Assert.Equal("feature", vm.State.SelectedBranchName);
        Assert.Single(vm.State.AllCommits);

        var allBranchesItem = vm.State.BranchListItems.First(b => b.IsAllBranches);
        await vm.SelectBranchAsync(allBranchesItem);

        Assert.Null(vm.State.SelectedBranchName);
        Assert.Null(gitService.LastRequestedBranchName);
        Assert.Equal(2, vm.State.AllCommits.Count);
    }
}
