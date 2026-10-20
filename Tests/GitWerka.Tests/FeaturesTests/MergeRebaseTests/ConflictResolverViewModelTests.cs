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

using static GitWerka.Tests.FeaturesTests.MergeRebaseTests.ConflictResolverTestHelper;

public class ConflictResolverViewModelTests
{
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
    }
}
