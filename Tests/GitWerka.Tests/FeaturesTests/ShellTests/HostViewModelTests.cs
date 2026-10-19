namespace GitWerka.Tests.FeaturesTests.ShellTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.Shell.UI.Screens.Host;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Infrastructure.Services;
using ReactiveUI;
using Xunit;

public class HostViewModelTests
{
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockGitService : IGitService
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

        public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, CancellationToken cancellationToken = default) =>
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

    [Fact]
    public void InitialState_HasReadyStatus_AndNoLoadedRepository()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        Assert.Equal("Ready", vm.State.StatusMessage);
        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Empty(vm.State.CurrentRepositoryName);
    }

    [Fact]
    public void SetActiveRepository_UpdatesStateCorrectly()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        vm.SetActiveRepository("RepoTest", "/path/to/repo");

        Assert.True(vm.State.IsRepositoryLoaded);
        Assert.Equal("RepoTest", vm.State.CurrentRepositoryName);
        Assert.Equal("/path/to/repo", vm.State.CurrentRepositoryPath);
    }

    [Fact]
    public void OpenSettingsCommand_WhenCommitLogScreenActive_OpensSettingsModal()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.False(commitLogVm.State.IsSettingsModalVisible);

        hostVm.OpenSettingsCommand.Execute().Subscribe();

        Assert.True(commitLogVm.State.IsSettingsModalVisible);
    }

    [Fact]
    public void RouterCurrentViewModel_WhenCommitLogNavigated_UpdatesActiveRepositoryInHost()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.True(hostVm.State.IsRepositoryLoaded);
        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);
        Assert.Equal("/tmp/sample-repo", hostVm.State.CurrentRepositoryPath);
    }

    private class TestProfileRepository : IProfileRepository
    {
        public List<GitProfile> Profiles { get; } = new();
        public Dictionary<string, int> Mappings { get; } = new();

        public Task<IReadOnlyList<GitProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitProfile>>(Profiles.ToList());

        public Task<GitProfile?> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profiles.FirstOrDefault(p => p.Id == id));

        public Task<GitProfile> SaveProfileAsync(GitProfile profile, CancellationToken cancellationToken = default)
        {
            Profiles.RemoveAll(p => p.Id == profile.Id);
            Profiles.Add(profile);
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

        public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<GitProfileCredentials>> GetAllCredentialsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitProfileCredentials>>(new List<GitProfileCredentials>());

        public Task<GitProfileCredentials?> GetCredentialsByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<GitProfileCredentials?>(null);

        public Task<GitProfileCredentials> SaveCredentialsAsync(GitProfileCredentials credentials, CancellationToken cancellationToken = default) =>
            Task.FromResult(credentials);

        public Task DeleteCredentialsAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<GitProfile?> GetActiveProfileForRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            if (Mappings.TryGetValue(repositoryPath, out int profileId))
            {
                return Task.FromResult(Profiles.FirstOrDefault(p => p.Id == profileId));
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
    public async Task RouterCurrentViewModel_WhenCommitLogNavigated_SynchronizesProfilesInHost()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@test.com" };
        GitProfile profile = new() { Id = 10, Name = "DevProfile", Author = author };
        profileRepo.Profiles.Add(profile);
        profileRepo.Mappings["/tmp/sample-repo"] = 10;

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        await commitLogVm.SelectProfileAsync(profile);

        Assert.NotNull(hostVm.State.ActiveProfile);
        Assert.Equal("DevProfile", hostVm.State.ActiveProfile.Name);
        Assert.Single(hostVm.State.AvailableProfiles);
    }

    [Fact]
    public async Task SelectProfileCommand_InHost_UpdatesActiveProfileAndPropagatesToCommitLog()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        GitAuthor author = new() { Id = 2, Name = "Bob", Email = "bob@test.com" };
        GitProfile profile = new() { Id = 20, Name = "BobProfile", Author = author };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        await hostVm.SelectProfileCommand.Execute(profile).FirstAsync();

        Assert.NotNull(hostVm.State.ActiveProfile);
        Assert.Equal("BobProfile", hostVm.State.ActiveProfile.Name);
        Assert.NotNull(commitLogVm.State.ActiveProfile);
        Assert.Equal("BobProfile", commitLogVm.State.ActiveProfile.Name);
        Assert.Equal(20, profileRepo.Mappings["/tmp/sample-repo"]);
    }

    [Fact]
    public void NavigateToProfilesCommand_InHost_NavigatesToProfileManagementScreen()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        hostVm.NavigateToProfilesCommand.Execute().Subscribe();

        Assert.IsType<ProfileManagementViewModel>(screen.Router.NavigationStack.Last());
    }

    [Fact]
    public async Task RefreshProfilesCommand_WhenNewProfileAdded_UpdatesAvailableProfilesInHostAndCommitLog()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.Empty(hostVm.State.AvailableProfiles);

        GitAuthor author = new() { Id = 5, Name = "Charlie", Email = "charlie@test.com" };
        GitProfile newProfile = new() { Id = 50, Name = "CharlieProfile", Author = author };
        profileRepo.Profiles.Add(newProfile);

        await hostVm.RefreshProfilesCommand.Execute().FirstAsync();

        Assert.Single(hostVm.State.AvailableProfiles);
        Assert.Equal("CharlieProfile", hostVm.State.AvailableProfiles[0].Name);
        Assert.Single(commitLogVm.State.AvailableProfiles);
        Assert.Equal("CharlieProfile", commitLogVm.State.AvailableProfiles[0].Name);
    }

    [Fact]
    public async Task RouterCurrentViewModel_WhenNavigatedBackFromProfileManagement_ReloadsProfilesAutomatically()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.Empty(hostVm.State.AvailableProfiles);

        ProfileManagementViewModel profileMgmtVm = new(screen, profileRepo, "/tmp/sample-repo");
        screen.Router.Navigate.Execute(profileMgmtVm).Subscribe();

        GitAuthor author = new() { Id = 6, Name = "Dave", Email = "dave@test.com" };
        GitProfile newProfile = new() { Id = 60, Name = "DaveProfile", Author = author };
        profileRepo.Profiles.Add(newProfile);

        screen.Router.NavigateBack.Execute().Subscribe();

        await commitLogVm.ReloadProfilesAsync();

        Assert.Single(hostVm.State.AvailableProfiles);
        Assert.Equal("DaveProfile", hostVm.State.AvailableProfiles[0].Name);
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
    public async Task CommitLogRepositoryName_WhenFavoritedOrUnfavorited_UpdatesHostCurrentRepositoryNameReactively()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();
        recentRepo.Items.Add(new RecentRepository
        {
            Path = "/tmp/sample-repo",
            Name = "sample-repo",
            IsFavorite = false,
            LastOpenedAtUtc = DateTime.UtcNow
        });

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService, profileRepository: profileRepo, recentRepositoriesRepository: recentRepo);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();
        await commitLogVm.LoadRepositoryDataCommand.Execute().FirstAsync();

        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);

        commitLogVm.ShowAddFavoriteModalCommand.Execute().Subscribe();
        commitLogVm.UpdateNewFavoriteNameInput("Custom Work Repo");
        await commitLogVm.ConfirmAddFavoriteCommand.Execute().FirstAsync();

        Assert.Equal("Custom Work Repo", commitLogVm.State.RepositoryName);
        Assert.Equal("Custom Work Repo", hostVm.State.CurrentRepositoryName);

        await commitLogVm.RemoveFromFavoritesCommand.Execute("/tmp/sample-repo").FirstAsync();

        Assert.Equal("sample-repo", commitLogVm.State.RepositoryName);
        Assert.Equal("sample-repo", hostVm.State.CurrentRepositoryName);
    }

    [Fact]
    public void HostViewModel_WhenOnWelcomeScreen_SettingsButtonIsVisibleAndTriggersSettingsModal()
    {
        MockScreen screen = new();
        TestProfileRepository profileRepo = new();
        HostViewModel hostVm = new(screen, profileRepo);
        MockGitService gitService = new();
        TestRecentRepositoriesRepository recentRepo = new();

        RepositoryManagerViewModel repoManagerVm = new(screen, recentRepo, gitService);
        screen.Router.Navigate.Execute(repoManagerVm).Subscribe();

        Assert.True(hostVm.State.IsSettingsButtonVisible);
        Assert.False(repoManagerVm.State.IsSettingsModalVisible);

        hostVm.OpenSettingsCommand.Execute().Subscribe();
        Assert.True(repoManagerVm.State.IsSettingsModalVisible);
    }

    [Fact]
    public void InitialState_FooterInfoText_DisplaysAppVersion()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Equal(vm.State.AppVersion, vm.State.FooterInfoText);
        Assert.StartsWith("v", vm.State.FooterInfoText);
    }

    [Fact]
    public void SetActiveRepository_UpdatesFooterInfoText_ToRepositoryPath()
    {
        MockScreen screen = new();
        HostViewModel vm = new(screen);

        vm.SetActiveRepository("RepoTest", "/path/to/my-repo");

        Assert.True(vm.State.IsRepositoryLoaded);
        Assert.Equal("/path/to/my-repo", vm.State.FooterInfoText);

        vm.SetActiveRepository(string.Empty, string.Empty);

        Assert.False(vm.State.IsRepositoryLoaded);
        Assert.Equal(vm.State.AppVersion, vm.State.FooterInfoText);
    }

    [Fact]
    public void RouterCurrentViewModel_WhenCommitLogNavigated_UpdatesFooterInfoTextToRepositoryPath_AndRestoresVersionOnBack()
    {
        MockScreen screen = new();
        HostViewModel hostVm = new(screen);
        MockGitService gitService = new();

        RepositoryManagerViewModel repoManagerVm = new(screen, new TestRecentRepositoriesRepository(), gitService);
        screen.Router.Navigate.Execute(repoManagerVm).Subscribe();

        Assert.Equal(hostVm.State.AppVersion, hostVm.State.FooterInfoText);

        CommitLogViewModel commitLogVm = new(screen, "/tmp/sample-repo", gitService);
        screen.Router.Navigate.Execute(commitLogVm).Subscribe();

        Assert.True(hostVm.State.IsRepositoryLoaded);
        Assert.Equal("/tmp/sample-repo", hostVm.State.FooterInfoText);

        screen.Router.NavigateBack.Execute().Subscribe();

        Assert.False(hostVm.State.IsRepositoryLoaded);
        Assert.Equal(hostVm.State.AppVersion, hostVm.State.FooterInfoText);
    }

    [Fact]
    public void HostViewModel_WhenStatusServicePublishesStatus_UpdatesHostStateAndFlags()
    {
        MockScreen screen = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        HostViewModel hostVm = new(screen, appStatusService: statusService);

        Assert.Equal("Ready", hostVm.State.StatusMessage);
        Assert.True(hostVm.State.IsStatusReady);
        Assert.False(hostVm.State.IsStatusInProgress);

        statusService.SetInProgress("Pushing changes...");

        Assert.Equal("Pushing changes...", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.InProgress, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusInProgress);
        Assert.False(hostVm.State.IsStatusReady);

        statusService.SetSuccess("Push completed successfully.");

        Assert.Equal("Push completed successfully.", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Success, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusSuccess);
        Assert.False(hostVm.State.IsStatusInProgress);

        statusService.SetError("Push failed: rejected");

        Assert.Equal("Push failed: rejected", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Error, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusError);

        statusService.SetReady();

        Assert.Equal("Ready", hostVm.State.StatusMessage);
        Assert.Equal(AppStatusSeverity.Ready, hostVm.State.StatusSeverity);
        Assert.True(hostVm.State.IsStatusReady);
    }

    [Fact]
    public void SetStatusMessage_WithAppStatusService_ForwardsToService()
    {
        MockScreen screen = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        HostViewModel hostVm = new(screen, appStatusService: statusService);

        hostVm.SetStatusMessage("Working...", AppStatusSeverity.InProgress);

        Assert.Equal("Working...", statusService.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.InProgress, statusService.CurrentStatus.Severity);
        Assert.True(hostVm.State.IsStatusInProgress);
    }
}
