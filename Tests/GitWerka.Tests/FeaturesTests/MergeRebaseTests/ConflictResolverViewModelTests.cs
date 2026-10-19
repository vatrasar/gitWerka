namespace GitWerka.Tests.FeaturesTests.MergeRebaseTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;
using ReactiveUI;
using Xunit;

public class ConflictResolverViewModelTests
{
    private class MockScreen : IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }

    private class MockGitService : IGitService
    {
        public ConflictFileModel ConflictModelToReturn { get; set; } = new();
        public string? SavedRepositoryPath { get; private set; }
        public string? SavedFilePath { get; private set; }
        public string? SavedResolvedContent { get; private set; }
        public bool MarkAllConflictsResolvedCalled { get; private set; }

        public bool IsValidRepository(string path) => true;

        public string? FindRepositoryRoot(string path) => path;

        public Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(directoryPath);

        public Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(targetDirectoryPath);

        public Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitRepositoryInfo());

        public Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, int maxBranches = 5, string? branchName = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommitSummary>>(Array.Empty<CommitSummary>());

        public Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string sha, CancellationToken cancellationToken = default) =>
            Task.FromResult<CommitDetail?>(null);

        public Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiffLine>>(Array.Empty<DiffLine>());

        public Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkingTreeChange>>(Array.Empty<WorkingTreeChange>());

        public Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchInfo>>(Array.Empty<BranchInfo>());

        public Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default) =>
            Task.FromResult("sha");

        public Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TagInfo>>(Array.Empty<TagInfo>());

        public Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StashInfo>>(Array.Empty<StashInfo>());

        public Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

        public Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, bool force = false, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitConfigInfo());

        public Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RepositoryOperationStatus());

        public Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitMergeResult { Status = "Merged", HasConflicts = false });

        public Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RebaseStep>>(Array.Empty<RebaseStep>());

        public Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConflictModelToReturn);
        }

        public Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default)
        {
            SavedRepositoryPath = repositoryPath;
            SavedFilePath = filePath;
            SavedResolvedContent = resolvedContent;
            return Task.CompletedTask;
        }

        public Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default)
        {
            MarkAllConflictsResolvedCalled = true;
            return Task.CompletedTask;
        }
    }

    private static ConflictFileModel CreateSampleConflictModel()
    {
        return new ConflictFileModel
        {
            FilePath = "src/index.js",
            FileName = "index.js",
            TotalConflicts = 1,
            OursCommitSha = "aaaaaaa",
            OursBranch = "main",
            TheirsCommitSha = "bbbbbbb",
            TheirsBranch = "feature/login",
            Chunks = ImmutableList.Create(
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("line 1", "line 2")
                },
                new ConflictFileChunk
                {
                    IsConflict = true,
                    HunkIndex = 1,
                    OursLines = ImmutableList.Create("ours modification"),
                    TheirsLines = ImmutableList.Create("theirs modification"),
                    IsOursSelected = true,
                    IsTheirsSelected = false
                },
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("line 4")
                }
            )
        };
    }

    [Fact]
    public async Task LoadData_WithConflictFile_InitializesDisplayLinesAndChunks()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await vm.LoadDataCommand.Execute().FirstAsync();

        Assert.Equal("index.js", vm.State.FileName);
        Assert.Equal(1, vm.State.TotalConflicts);
        Assert.Equal(1, vm.State.ActiveConflictIndex);
        Assert.Equal("main", vm.State.OursBranch);
        Assert.Equal("feature/login", vm.State.TheirsBranch);
        Assert.True(vm.State.OutputDisplayLines.Count > 0);
        Assert.Contains("ours modification", vm.State.MergedOutputText);
        Assert.DoesNotContain("theirs modification", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task ToggleHunkTheirs_WhenSelected_IncludesTheirsInOutput()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleHunkTheirs(1);

        Assert.True(vm.State.Chunks[1].IsTheirsSelected);
        Assert.True(vm.State.Chunks[1].IsOursSelected);
        Assert.Contains("ours modification", vm.State.MergedOutputText);
        Assert.Contains("theirs modification", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task ToggleHunkOurs_WhenUnselected_RemovesOursFromOutput()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleHunkOurs(1);

        Assert.False(vm.State.Chunks[1].IsOursSelected);
        Assert.DoesNotContain("ours modification", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task ToggleAllTheirs_WhenInvoked_SelectsTheirsForAllHunks()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleAllTheirsCommand.Execute().Subscribe();

        Assert.True(vm.State.IsAllTheirsChecked);
        Assert.True(vm.State.Chunks[1].IsTheirsSelected);
        Assert.Contains("theirs modification", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task ToggleAllOurs_WhenAllSelected_DeselectsAllOurs()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleAllOursCommand.Execute().Subscribe();

        Assert.False(vm.State.IsAllOursChecked);
        Assert.False(vm.State.Chunks[1].IsOursSelected);
        Assert.DoesNotContain("ours modification", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task NextConflict_WithMultipleConflicts_CyclesForward()
    {
        MockScreen screen = new();
        ConflictFileModel multiModel = new()
        {
            FilePath = "test.txt",
            FileName = "test.txt",
            TotalConflicts = 3,
            Chunks = ImmutableList.Create(
                new ConflictFileChunk { IsConflict = true, HunkIndex = 1 },
                new ConflictFileChunk { IsConflict = true, HunkIndex = 2 },
                new ConflictFileChunk { IsConflict = true, HunkIndex = 3 }
            )
        };
        MockGitService gitService = new() { ConflictModelToReturn = multiModel };

        ConflictResolverViewModel vm = new(screen, gitService, "/path", "test.txt");
        await vm.LoadDataCommand.Execute().FirstAsync();

        Assert.Equal(1, vm.State.ActiveConflictIndex);

        vm.NextConflictCommand.Execute().Subscribe();
        Assert.Equal(2, vm.State.ActiveConflictIndex);

        vm.NextConflictCommand.Execute().Subscribe();
        Assert.Equal(3, vm.State.ActiveConflictIndex);

        vm.NextConflictCommand.Execute().Subscribe();
        Assert.Equal(1, vm.State.ActiveConflictIndex);
    }

    [Fact]
    public async Task PreviousConflict_WithMultipleConflicts_CyclesBackward()
    {
        MockScreen screen = new();
        ConflictFileModel multiModel = new()
        {
            FilePath = "test.txt",
            FileName = "test.txt",
            TotalConflicts = 3,
            Chunks = ImmutableList.Create(
                new ConflictFileChunk { IsConflict = true, HunkIndex = 1 },
                new ConflictFileChunk { IsConflict = true, HunkIndex = 2 },
                new ConflictFileChunk { IsConflict = true, HunkIndex = 3 }
            )
        };
        MockGitService gitService = new() { ConflictModelToReturn = multiModel };

        ConflictResolverViewModel vm = new(screen, gitService, "/path", "test.txt");
        await vm.LoadDataCommand.Execute().FirstAsync();

        Assert.Equal(1, vm.State.ActiveConflictIndex);

        vm.PreviousConflictCommand.Execute().Subscribe();
        Assert.Equal(3, vm.State.ActiveConflictIndex);

        vm.PreviousConflictCommand.Execute().Subscribe();
        Assert.Equal(2, vm.State.ActiveConflictIndex);
    }

    [Fact]
    public async Task SaveAndResolve_WhenInvoked_SavesContentToGitServiceAndNavigatesBack()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await screen.Router.Navigate.Execute(vm).FirstAsync();
        await vm.LoadDataCommand.Execute().FirstAsync();

        await vm.SaveAndResolveCommand.Execute().FirstAsync();

        Assert.Equal("/path/to/repo", gitService.SavedRepositoryPath);
        Assert.Equal("src/index.js", gitService.SavedFilePath);
        Assert.Contains("ours modification", gitService.SavedResolvedContent);
        Assert.Empty(screen.Router.NavigationStack);
    }

    [Fact]
    public async Task Cancel_WhenInvoked_NavigatesBackWithoutSaving()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateSampleConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/index.js");
        await screen.Router.Navigate.Execute(vm).FirstAsync();

        vm.CancelCommand.Execute().Subscribe();

        Assert.Null(gitService.SavedRepositoryPath);
        Assert.Empty(screen.Router.NavigationStack);
    }

    [Fact]
    public async Task ToggleOursLine_AddsAndRemovesIndividualLine()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleOursLine(1, 1);

        Assert.Contains("ours line 2", vm.State.MergedOutputText);
        Assert.DoesNotContain("ours line 1", vm.State.MergedOutputText);
        Assert.Contains(vm.State.Chunks[1].SelectedLines, s => s.IsOurs && s.LineIndex == 1);

        vm.ToggleOursLine(1, 1);

        Assert.DoesNotContain("ours line 2", vm.State.MergedOutputText);
        Assert.Empty(vm.State.Chunks[1].SelectedLines);
    }

    [Fact]
    public async Task ToggleTheirsLine_AddsAndRemovesIndividualLine()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleTheirsLine(1, 0);

        Assert.Contains("theirs line 1", vm.State.MergedOutputText);
        Assert.DoesNotContain("theirs line 2", vm.State.MergedOutputText);
        Assert.Contains(vm.State.Chunks[1].SelectedLines, s => !s.IsOurs && s.LineIndex == 0);

        vm.ToggleTheirsLine(1, 0);

        Assert.DoesNotContain("theirs line 1", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task InterleavingLines_PreservesSelectionOrderInOutput()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleOursLine(1, 1);
        vm.ToggleTheirsLine(1, 0);
        vm.ToggleOursLine(1, 0);

        var conflictLines = vm.State.OutputDisplayLines.Where(l => l.IsConflict).ToList();
        Assert.Equal(3, conflictLines.Count);
        Assert.Equal("ours line 2", conflictLines[0].Text);
        Assert.True(conflictLines[0].IsFromOurs);
        Assert.Equal("theirs line 1", conflictLines[1].Text);
        Assert.True(conflictLines[1].IsFromTheirs);
        Assert.Equal("ours line 1", conflictLines[2].Text);
        Assert.True(conflictLines[2].IsFromOurs);
    }

    [Fact]
    public async Task UpdateMergedOutputText_AllowsManualTextEditing()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        string customEdited = "header 1\ncustom hand-crafted resolution\nfooter 1";
        vm.UpdateMergedOutputText(customEdited);

        Assert.Equal(customEdited, vm.State.MergedOutputText);
    }

    [Fact]
    public async Task ResetOutputToSelection_RestoresSelectedLinesAfterManualEdit()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        vm.ToggleOursLine(1, 0);
        Assert.Contains("ours line 1", vm.State.MergedOutputText);

        vm.UpdateMergedOutputText("arbitrary override");
        Assert.Equal("arbitrary override", vm.State.MergedOutputText);

        vm.ResetOutputToSelection();
        Assert.Contains("ours line 1", vm.State.MergedOutputText);
        Assert.DoesNotContain("arbitrary override", vm.State.MergedOutputText);
    }

    [Fact]
    public async Task HunkCheckbox_ReflectsFullPartialAndEmptySelectionStates()
    {
        MockScreen screen = new();
        MockGitService gitService = new()
        {
            ConflictModelToReturn = CreateMultiLineConflictModel()
        };

        ConflictResolverViewModel vm = new(screen, gitService, "/path/to/repo", "src/app.cs");
        await vm.LoadDataCommand.Execute().FirstAsync();

        var firstLine = vm.State.OursDisplayLines.First(l => l.IsHunkFirstLine);
        Assert.False(firstLine.IsHunkChecked);

        vm.ToggleHunkOurs(1);
        firstLine = vm.State.OursDisplayLines.First(l => l.IsHunkFirstLine);
        Assert.True(firstLine.IsHunkChecked);
        Assert.True(vm.State.OursDisplayLines.Where(l => l.IsConflict).All(l => l.IsSelected));

        vm.ToggleOursLine(1, 0);
        firstLine = vm.State.OursDisplayLines.First(l => l.IsHunkFirstLine);
        Assert.Null(firstLine.IsHunkChecked);

        vm.ToggleHunkOurs(1);
        firstLine = vm.State.OursDisplayLines.First(l => l.IsHunkFirstLine);
        Assert.True(firstLine.IsHunkChecked);
        Assert.True(vm.State.OursDisplayLines.Where(l => l.IsConflict).All(l => l.IsSelected));

        vm.ToggleHunkOurs(1);
        firstLine = vm.State.OursDisplayLines.First(l => l.IsHunkFirstLine);
        Assert.False(firstLine.IsHunkChecked);
        Assert.True(vm.State.OursDisplayLines.Where(l => l.IsConflict).All(l => !l.IsSelected));
    }

    private static ConflictFileModel CreateMultiLineConflictModel()
    {
        return new ConflictFileModel
        {
            FilePath = "src/app.cs",
            FileName = "app.cs",
            TotalConflicts = 1,
            OursCommitSha = "aaaaaaa",
            OursBranch = "main",
            TheirsCommitSha = "bbbbbbb",
            TheirsBranch = "feature/test",
            Chunks = ImmutableList.Create(
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("header 1")
                },
                new ConflictFileChunk
                {
                    IsConflict = true,
                    HunkIndex = 1,
                    OursLines = ImmutableList.Create("ours line 1", "ours line 2"),
                    TheirsLines = ImmutableList.Create("theirs line 1", "theirs line 2"),
                    IsOursSelected = false,
                    IsTheirsSelected = false
                },
                new ConflictFileChunk
                {
                    IsConflict = false,
                    HunkIndex = 0,
                    CommonLines = ImmutableList.Create("footer 1")
                }
            )
        };
    }
}
