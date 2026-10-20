namespace GitWerka.Tests.FeaturesTests.MergeRebaseTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;
using ReactiveUI;
using Xunit;

using static GitWerka.Tests.FeaturesTests.MergeRebaseTests.ConflictResolverTestHelper;

public class ConflictResolverResolutionAndActionsTests
{
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
