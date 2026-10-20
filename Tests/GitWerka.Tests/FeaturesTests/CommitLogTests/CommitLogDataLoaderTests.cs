namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Services;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;
using Xunit;

public class CommitLogDataLoaderTests
{
    [Fact]
    public async Task RefreshRepositoryDataAsync_WhenSnapshotUnchangedAndNotForced_DoesNotReloadData()
    {
        var (loader, gitService, state, getState) = CreateTestContext();
        var snapshot = new GitRepositoryStateSnapshot
        {
            HeadCommitSha = "sha1",
            CurrentBranch = "main"
        };
        gitService.SnapshotToReturn = snapshot;

        await loader.RefreshRepositoryDataAsync(force: true);
        int initialCommitCount = getState().AllCommits.Count;

        gitService.Commits.Add(new CommitSummary { Sha = "sha2", Message = "Second commit" });

        await loader.RefreshRepositoryDataAsync(force: false);

        Assert.Equal(initialCommitCount, getState().AllCommits.Count);
    }

    [Fact]
    public async Task RefreshRepositoryDataAsync_WhenSnapshotChangedAndNotForced_ReloadsData()
    {
        var (loader, gitService, state, getState) = CreateTestContext();
        var initialSnapshot = new GitRepositoryStateSnapshot
        {
            HeadCommitSha = "sha1",
            CurrentBranch = "main"
        };
        gitService.SnapshotToReturn = initialSnapshot;

        await loader.RefreshRepositoryDataAsync(force: true);

        gitService.Commits.Add(new CommitSummary { Sha = "sha2", Message = "Second commit" });
        var newSnapshot = initialSnapshot with
        {
            HeadCommitSha = "sha2",
            BranchSignatures = ImmutableList.Create("main:sha2:false:true")
        };
        gitService.SnapshotToReturn = newSnapshot;

        await loader.RefreshRepositoryDataAsync(force: false);

        Assert.Equal(2, getState().AllCommits.Count);
    }

    [Fact]
    public async Task RefreshRepositoryDataAsync_WhenForced_ReloadsEvenIfSnapshotUnchanged()
    {
        var (loader, gitService, state, getState) = CreateTestContext();
        var snapshot = new GitRepositoryStateSnapshot
        {
            HeadCommitSha = "sha1",
            CurrentBranch = "main"
        };
        gitService.SnapshotToReturn = snapshot;

        await loader.RefreshRepositoryDataAsync(force: true);

        gitService.Commits.Add(new CommitSummary { Sha = "sha2", Message = "Second commit" });

        await loader.RefreshRepositoryDataAsync(force: true);

        Assert.Equal(2, getState().AllCommits.Count);
    }

    private static (
        CommitLogDataLoader Loader,
        MockGitService GitService,
        CommitLogState State,
        Func<CommitLogState> GetState) CreateTestContext()
    {
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "sha1", Message = "First commit" });
        gitService.Branches.Add(new BranchInfo { Name = "main", IsCurrentHead = true, TipSha = "sha1" });

        var state = new CommitLogState
        {
            RepositoryPath = "/test/repo",
            RepositoryName = "repo"
        };

        void UpdateState(Func<CommitLogState, CommitLogState> updater)
        {
            state = updater(state);
        }

        CommitLogState GetState() => state;

        MockApplicationSettingsRepository settingsRepo = new();
        MockAiConfigurationRepository aiRepo = new();
        TestRecentRepositoriesRepository recentRepo = new();
        TestProfileRepository profileRepo = new();
        MockScreen screen = new();
        MockSystemLauncherService launcher = new();

        var settingsCoordinator = new CommitLogSettingsCoordinator(gitService, settingsRepo, aiRepo, UpdateState, GetState);
        var favoritesCoordinator = new CommitLogFavoritesCoordinator(recentRepo, gitService, UpdateState, GetState);
        var historyCoordinator = new CommitLogHistoryActionCoordinator(gitService, launcher, profileRepo, screen, GetState, UpdateState, () => Task.CompletedTask);

        var workingTreePanel = new WorkingTreePanelViewModel();
        var gitModals = new GitOperationModalsViewModel();
        var favoritesDock = new FavoritesDockViewModel(recentRepo, gitService, "/test/repo");

        var loader = new CommitLogDataLoader(
            gitService,
            settingsCoordinator,
            historyCoordinator,
            favoritesCoordinator,
            GetState,
            UpdateState,
            () => workingTreePanel,
            () => gitModals,
            () => favoritesDock);

        return (loader, gitService, state, GetState);
    }
}
