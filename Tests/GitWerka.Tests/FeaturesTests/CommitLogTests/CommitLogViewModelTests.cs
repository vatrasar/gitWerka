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
using GitWerka.Features.CommitLog.Resources;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Infrastructure.Services;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Linq;
using Xunit;

public class CommitLogViewModelTests
{

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
}
