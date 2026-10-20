namespace GitWerka.Tests.CoreTests.ServicesTests;

using System;
using System.IO;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using LibGit2Sharp;
using Xunit;

public class GitServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly GitService _gitService;

    public GitServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "GitWerkaTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _gitService = new GitService();
    }

    [Fact]
    public async Task GetCommitsAsync_RepositoryWithIgnoredFiles_DoesNotCreateWipNode()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, ".gitignore"), "*.log\nbin/\n");
        await _gitService.StageFileAsync(repoPath, ".gitignore");
        await _gitService.CreateCommitAsync(repoPath, "Initial commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "test.log"), "Ignored log contents");
        Directory.CreateDirectory(Path.Combine(repoPath, "bin"));
        await File.WriteAllTextAsync(Path.Combine(repoPath, "bin", "app.dll"), "binary");

        var commits = await _gitService.GetCommitLogAsync(repoPath);
        var changes = await _gitService.GetWorkingTreeChangesAsync(repoPath);

        Assert.DoesNotContain(commits, c => c.IsWip);
        Assert.Empty(changes);
    }

    [Fact]
    public void IsValidRepository_NonExistentOrEmptyDirectory_ReturnsFalse()
    {
        bool isValid = _gitService.IsValidRepository(_testDirectory);

        Assert.False(isValid);
    }

    [Fact]
    public void FindRepositoryRoot_NullOrEmptyOrInvalid_ReturnsNull()
    {
        Assert.Null(_gitService.FindRepositoryRoot(string.Empty));
        Assert.Null(_gitService.FindRepositoryRoot(_testDirectory));
    }

    [Fact]
    public async Task FindRepositoryRoot_Subdirectory_ReturnsRootPath()
    {
        string initPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        string subDir = Path.Combine(initPath, "nested", "deep", "folder");
        Directory.CreateDirectory(subDir);

        string? foundRoot = _gitService.FindRepositoryRoot(subDir);

        Assert.NotNull(foundRoot);
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(initPath)), foundRoot);
    }

    [Fact]
    public async Task InitializeRepositoryAsync_CreatesValidRepository()
    {
        string initPath = await _gitService.InitializeRepositoryAsync(_testDirectory);

        Assert.True(Directory.Exists(initPath));
        Assert.True(_gitService.IsValidRepository(initPath));
    }

    [Fact]
    public async Task GetRepositoryInfoAsync_InitializedRepository_ReturnsExpectedInfo()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);

        GitRepositoryInfo info = await _gitService.GetRepositoryInfoAsync(repoPath);

        Assert.NotNull(info);
        Assert.Equal(new DirectoryInfo(_testDirectory).Name, info.Name);
    }

    [Fact]
    public async Task CreateCommitAsync_StagedFile_CreatesCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        string testFilePath = Path.Combine(repoPath, "hello.txt");
        await File.WriteAllTextAsync(testFilePath, "Hello GitWerka");

        await _gitService.StageFileAsync(repoPath, "hello.txt");
        string sha = await _gitService.CreateCommitAsync(repoPath, "Initial commit", "Tester", "tester@example.com");

        Assert.False(string.IsNullOrWhiteSpace(sha));
        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Single(log);
        Assert.Equal("Initial commit", log[0].Message);
    }

    [Fact]
    public async Task CreateCommitAsync_WithAmend_AmendsPreviousCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        string testFilePath = Path.Combine(repoPath, "hello.txt");
        await File.WriteAllTextAsync(testFilePath, "Hello GitWerka");
        await _gitService.StageFileAsync(repoPath, "hello.txt");
        await _gitService.CreateCommitAsync(repoPath, "Initial commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(testFilePath, "Hello GitWerka Updated");
        await _gitService.StageFileAsync(repoPath, "hello.txt");
        string amendedSha = await _gitService.CreateCommitAsync(
            repoPath,
            "Amended commit message",
            "Tester",
            "tester@example.com",
            amendPreviousCommit: true);

        Assert.False(string.IsNullOrWhiteSpace(amendedSha));
        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Single(log);
        Assert.Equal("Amended commit message", log[0].Message);
    }

    [Fact]
    public async Task CreateBranchAsync_ValidBranchName_CreatesBranch()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        string testFilePath = Path.Combine(repoPath, "file.txt");
        await File.WriteAllTextAsync(testFilePath, "Content");
        await _gitService.StageFileAsync(repoPath, "file.txt");
        await _gitService.CreateCommitAsync(repoPath, "Commit 1", "Tester", "tester@example.com");

        await _gitService.CreateBranchAsync(repoPath, "feature-branch");

        var branches = await _gitService.GetBranchesAsync(repoPath);
        Assert.Contains(branches, b => b.Name == "feature-branch");
    }

    [Fact]
    public async Task CreateTagAsync_ValidTagName_CreatesTag()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        string testFilePath = Path.Combine(repoPath, "file.txt");
        await File.WriteAllTextAsync(testFilePath, "Content");
        await _gitService.StageFileAsync(repoPath, "file.txt");
        await _gitService.CreateCommitAsync(repoPath, "Commit 1", "Tester", "tester@example.com");

        await _gitService.CreateTagAsync(repoPath, "v1.0.0");

        var tags = await _gitService.GetTagsAsync(repoPath);
        Assert.Contains(tags, t => t.Name == "v1.0.0");
    }

    [Fact]
    public async Task SetGitConfigAsync_ValidUserIdentity_PersistsConfig()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);

        await _gitService.SetGitConfigAsync(repoPath, "Jane Doe", "jane@example.com");
        var config = await _gitService.GetGitConfigAsync(repoPath);

        Assert.Equal("Jane Doe", config.UserName);
        Assert.Equal("jane@example.com", config.UserEmail);
    }

    [Fact]
    public async Task ResetBranchToCommitAsync_HardReset_ResetsHeadToTargetCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "f1.txt"), "Version 1");
        await _gitService.StageFileAsync(repoPath, "f1.txt");
        string sha1 = await _gitService.CreateCommitAsync(repoPath, "Commit 1", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "f2.txt"), "Version 2");
        await _gitService.StageFileAsync(repoPath, "f2.txt");
        await _gitService.CreateCommitAsync(repoPath, "Commit 2", "Tester", "tester@example.com");

        var logBefore = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(2, logBefore.Count);

        await _gitService.ResetBranchToCommitAsync(repoPath, sha1, GitResetType.Hard);

        var logAfter = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Single(logAfter);
        Assert.Equal(sha1, logAfter[0].Sha);
    }

    [Fact]
    public async Task RevertCommitAsync_ValidCommit_CreatesRevertCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "init.txt"), "Init");
        await _gitService.StageFileAsync(repoPath, "init.txt");
        await _gitService.CreateCommitAsync(repoPath, "Initial commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "change.txt"), "Change");
        await _gitService.StageFileAsync(repoPath, "change.txt");
        string targetSha = await _gitService.CreateCommitAsync(repoPath, "Feature to revert", "Tester", "tester@example.com");

        await _gitService.RevertCommitAsync(repoPath, targetSha);

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(3, log.Count);
        Assert.Contains("Revert", log[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CherryPickCommitAsync_CommitFromOtherBranch_AppliesCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string baseBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "feature");
        await _gitService.CheckoutBranchAsync(repoPath, "feature");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "cherry.txt"), "Cherry Pick Content");
        await _gitService.StageFileAsync(repoPath, "cherry.txt");
        string cherrySha = await _gitService.CreateCommitAsync(repoPath, "Special Feature", "Tester", "tester@example.com");

        await _gitService.CheckoutBranchAsync(repoPath, baseBranch);
        await _gitService.CherryPickCommitAsync(repoPath, cherrySha);

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(3, log.Count);
        Assert.Contains("Special Feature", log[0].Message);
    }

    [Fact]
    public async Task GetCommitLogAsync_WithUncommittedChanges_IncludesWipNodeAtTop()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "newfile.txt"), "Uncommitted content");

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(2, log.Count);
        Assert.True(log[0].IsWip);
        Assert.Equal("// WIP", log[0].Message);
        Assert.Equal(1, log[0].UncommittedFilesCount);
        Assert.Equal("Base commit", log[1].Message);

        var diffLines = await _gitService.GetFileDiffLinesAsync(repoPath, "WIP", "newfile.txt");
        Assert.NotEmpty(diffLines);
    }

    [Fact]
    public async Task MergeBranchAsync_FastForward_MovesHeadForward()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string baseBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "feature");
        await _gitService.CheckoutBranchAsync(repoPath, "feature");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "feature.txt"), "Feature");
        await _gitService.StageFileAsync(repoPath, "feature.txt");
        string featureSha = await _gitService.CreateCommitAsync(repoPath, "Feature commit", "Tester", "tester@example.com");

        await _gitService.CheckoutBranchAsync(repoPath, baseBranch);
        GitMergeResult result = await _gitService.MergeBranchAsync(repoPath, "feature", GitMergeFastForwardMode.FastForward);

        Assert.Equal(MergeStatus.FastForward.ToString(), result.Status);
        Assert.False(result.HasConflicts);

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(2, log.Count);
        Assert.Equal(featureSha, log[0].Sha);
    }

    [Fact]
    public async Task GetCommitLogAsync_WithCommitsOnDifferentBranches_ReturnsAllCommits()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string baseBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "feature");
        await _gitService.CheckoutBranchAsync(repoPath, "feature");
        await File.WriteAllTextAsync(Path.Combine(repoPath, "feature.txt"), "Feature");
        await _gitService.StageFileAsync(repoPath, "feature.txt");
        await _gitService.CreateCommitAsync(repoPath, "Feature commit", "Tester", "tester@example.com");

        await _gitService.CheckoutBranchAsync(repoPath, baseBranch);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "main.txt"), "Main");
        await _gitService.StageFileAsync(repoPath, "main.txt");
        await _gitService.CreateCommitAsync(repoPath, "Main branch commit", "Tester", "tester@example.com");

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Contains(log, c => c.Message == "Feature commit");
        Assert.Contains(log, c => c.Message == "Main branch commit");
        Assert.Contains(log, c => c.Message == "Base commit");
        Assert.Equal(3, log.Count);
        Assert.All(log, c => Assert.True(c.GraphWidth >= 80.0));
    }

    [Fact]
    public async Task GetCommitLogAsync_WithSpecificBranchName_ReturnsOnlyCommitsOfThatBranch()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string baseBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "feature");
        await _gitService.CheckoutBranchAsync(repoPath, "feature");
        await File.WriteAllTextAsync(Path.Combine(repoPath, "feature.txt"), "Feature");
        await _gitService.StageFileAsync(repoPath, "feature.txt");
        await _gitService.CreateCommitAsync(repoPath, "Feature commit", "Tester", "tester@example.com");

        await _gitService.CheckoutBranchAsync(repoPath, baseBranch);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "main.txt"), "Main");
        await _gitService.StageFileAsync(repoPath, "main.txt");
        await _gitService.CreateCommitAsync(repoPath, "Main branch commit", "Tester", "tester@example.com");

        var featureLog = await _gitService.GetCommitLogAsync(repoPath, branchName: "feature");
        Assert.Equal(2, featureLog.Count);
        Assert.Contains(featureLog, c => c.Message == "Feature commit");
        Assert.Contains(featureLog, c => c.Message == "Base commit");
        Assert.DoesNotContain(featureLog, c => c.Message == "Main branch commit");

        var mainLog = await _gitService.GetCommitLogAsync(repoPath, branchName: baseBranch);
        Assert.Equal(2, mainLog.Count);
        Assert.Contains(mainLog, c => c.Message == "Main branch commit");
        Assert.Contains(mainLog, c => c.Message == "Base commit");
        Assert.DoesNotContain(mainLog, c => c.Message == "Feature commit");
    }

    [Fact]
    public async Task GetCommitLogAsync_WithMaxBranches_LimitsReachableCommitsToMostRecentlyUpdatedBranches()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string mainBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "older-branch");
        await _gitService.CheckoutBranchAsync(repoPath, "older-branch");
        await File.WriteAllTextAsync(Path.Combine(repoPath, "older.txt"), "Older");
        await _gitService.StageFileAsync(repoPath, "older.txt");
        await _gitService.CreateCommitAsync(repoPath, "Older commit", "Tester", "tester@example.com");

        await Task.Delay(1100);

        await _gitService.CheckoutBranchAsync(repoPath, mainBranch);
        await _gitService.CreateBranchAsync(repoPath, "newer-branch");
        await _gitService.CheckoutBranchAsync(repoPath, "newer-branch");
        await File.WriteAllTextAsync(Path.Combine(repoPath, "newer.txt"), "Newer");
        await _gitService.StageFileAsync(repoPath, "newer.txt");
        await _gitService.CreateCommitAsync(repoPath, "Newer commit", "Tester", "tester@example.com");

        await Task.Delay(1100);

        await _gitService.CheckoutBranchAsync(repoPath, mainBranch);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "main.txt"), "Main");
        await _gitService.StageFileAsync(repoPath, "main.txt");
        await _gitService.CreateCommitAsync(repoPath, "Main head commit", "Tester", "tester@example.com");

        var logWithMaxTwo = await _gitService.GetCommitLogAsync(repoPath, maxBranches: 2);

        Assert.Contains(logWithMaxTwo, c => c.Message == "Main head commit");
        Assert.Contains(logWithMaxTwo, c => c.Message == "Newer commit");
        Assert.DoesNotContain(logWithMaxTwo, c => c.Message == "Older commit");
    }

    [Fact]
    public async Task MergeBranchAsync_NoFastForward_CreatesMergeCommit()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        var repoInfo = await _gitService.GetRepositoryInfoAsync(repoPath);
        string baseBranch = repoInfo.CurrentBranch;

        await _gitService.CreateBranchAsync(repoPath, "feature-noff");
        await _gitService.CheckoutBranchAsync(repoPath, "feature-noff");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "feature-noff.txt"), "Feature NoFF");
        await _gitService.StageFileAsync(repoPath, "feature-noff.txt");
        await _gitService.CreateCommitAsync(repoPath, "Feature NoFF commit", "Tester", "tester@example.com");

        await _gitService.CheckoutBranchAsync(repoPath, baseBranch);
        GitMergeResult result = await _gitService.MergeBranchAsync(repoPath, "feature-noff", GitMergeFastForwardMode.NoFastForward);

        Assert.Equal(MergeStatus.NonFastForward.ToString(), result.Status);
        Assert.False(result.HasConflicts);

        var log = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(3, log.Count);
        Assert.Contains("Merge branch 'feature-noff'", log[0].Message);
    }

    [Fact]
    public async Task GetInteractiveRebaseStepsAsync_ReturnsChronologicalSteps()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        string baseSha = await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "step1.txt"), "1");
        await _gitService.StageFileAsync(repoPath, "step1.txt");
        await _gitService.CreateCommitAsync(repoPath, "Step 1", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "step2.txt"), "2");
        await _gitService.StageFileAsync(repoPath, "step2.txt");
        await _gitService.CreateCommitAsync(repoPath, "Step 2", "Tester", "tester@example.com");

        var steps = await _gitService.GetInteractiveRebaseStepsAsync(repoPath, baseSha);

        Assert.Equal(2, steps.Count);
        Assert.Equal("Step 1", steps[0].OriginalMessage);
        Assert.Equal("Step 2", steps[1].OriginalMessage);
    }

    [Fact]
    public async Task ExecuteInteractiveRebaseAsync_DropAndReword_AppliesExpectedModifications()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        string baseSha = await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "keep.txt"), "Keep");
        await _gitService.StageFileAsync(repoPath, "keep.txt");
        await _gitService.CreateCommitAsync(repoPath, "Keep Me", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "drop.txt"), "Drop");
        await _gitService.StageFileAsync(repoPath, "drop.txt");
        await _gitService.CreateCommitAsync(repoPath, "Drop Me", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "reword.txt"), "Reword");
        await _gitService.StageFileAsync(repoPath, "reword.txt");
        await _gitService.CreateCommitAsync(repoPath, "Reword Me", "Tester", "tester@example.com");

        var initialSteps = await _gitService.GetInteractiveRebaseStepsAsync(repoPath, baseSha);
        Assert.Equal(3, initialSteps.Count);

        var configuredSteps = new[]
        {
            initialSteps[0] with { Action = RebaseStepAction.Pick },
            initialSteps[1] with { Action = RebaseStepAction.Drop },
            initialSteps[2] with { Action = RebaseStepAction.Reword, NewMessage = "Reworded Commit Title" }
        };

        bool success = await _gitService.ExecuteInteractiveRebaseAsync(repoPath, baseSha, configuredSteps);
        Assert.True(success);

        var finalLog = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(3, finalLog.Count);
        Assert.Equal("Reworded Commit Title", finalLog[0].Message);
        Assert.Equal("Keep Me", finalLog[1].Message);
        Assert.Equal("Base commit", finalLog[2].Message);
        Assert.DoesNotContain(finalLog, c => c.Message == "Drop Me");
    }

    [Fact]
    public async Task ExecuteInteractiveRebaseAsync_ReorderSteps_InvertsCommitOrder()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);
        await File.WriteAllTextAsync(Path.Combine(repoPath, "base.txt"), "Base");
        await _gitService.StageFileAsync(repoPath, "base.txt");
        string baseSha = await _gitService.CreateCommitAsync(repoPath, "Base commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "alpha.txt"), "Alpha");
        await _gitService.StageFileAsync(repoPath, "alpha.txt");
        await _gitService.CreateCommitAsync(repoPath, "Alpha Commit", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(Path.Combine(repoPath, "beta.txt"), "Beta");
        await _gitService.StageFileAsync(repoPath, "beta.txt");
        await _gitService.CreateCommitAsync(repoPath, "Beta Commit", "Tester", "tester@example.com");

        var initialSteps = await _gitService.GetInteractiveRebaseStepsAsync(repoPath, baseSha);
        var invertedSteps = new[] { initialSteps[1], initialSteps[0] };

        bool success = await _gitService.ExecuteInteractiveRebaseAsync(repoPath, baseSha, invertedSteps);
        Assert.True(success);

        var finalLog = await _gitService.GetCommitLogAsync(repoPath);
        Assert.Equal(3, finalLog.Count);
        Assert.Equal("Alpha Commit", finalLog[0].Message);
        Assert.Equal("Beta Commit", finalLog[1].Message);
        Assert.Equal("Base commit", finalLog[2].Message);
    }

    [Fact]
    public async Task GetWorkingTreeChangesAsync_WithStagedUnstagedAndUntracked_CalculatesLinesAndFullPath()
    {
        string repoPath = await _gitService.InitializeRepositoryAsync(_testDirectory);

        string stagedFile = Path.Combine(repoPath, "staged.txt");
        await File.WriteAllTextAsync(stagedFile, "Line 1\nLine 2\n");
        await _gitService.StageFileAsync(repoPath, "staged.txt");
        await _gitService.CreateCommitAsync(repoPath, "Base", "Tester", "tester@example.com");

        string unstagedFile = Path.Combine(repoPath, "unstaged.txt");
        await File.WriteAllTextAsync(unstagedFile, "Alpha\nBeta\nGamma\n");
        await _gitService.StageFileAsync(repoPath, "unstaged.txt");
        await _gitService.CreateCommitAsync(repoPath, "Add unstaged", "Tester", "tester@example.com");

        await File.WriteAllTextAsync(stagedFile, "Line 1 modified\nLine 2\nLine 3\nLine 4\n");
        await _gitService.StageFileAsync(repoPath, "staged.txt");

        await File.WriteAllTextAsync(unstagedFile, "Alpha\nDelta\n");

        string untrackedFile = Path.Combine(repoPath, "untracked.txt");
        await File.WriteAllTextAsync(untrackedFile, "One\nTwo\nThree\nFour\nFive\n");

        var changes = await _gitService.GetWorkingTreeChangesAsync(repoPath);

        var stagedChange = changes.First(c => c.Path == "staged.txt" && c.IsStaged);
        Assert.Equal(3, stagedChange.LinesAdded);
        Assert.Equal(1, stagedChange.LinesDeleted);
        Assert.Equal(Path.GetFullPath(stagedFile), stagedChange.FullPath);
        Assert.Equal("staged.txt", stagedChange.DisplayToolTip);

        var unstagedChange = changes.First(c => c.Path == "unstaged.txt" && !c.IsStaged);
        Assert.Equal(1, unstagedChange.LinesAdded);
        Assert.Equal(2, unstagedChange.LinesDeleted);
        Assert.Equal(Path.GetFullPath(unstagedFile), unstagedChange.FullPath);
        Assert.Equal("unstaged.txt", unstagedChange.DisplayToolTip);

        var untrackedChange = changes.First(c => c.Path == "untracked.txt" && !c.IsStaged);
        Assert.Equal(5, untrackedChange.LinesAdded);
        Assert.Equal(0, untrackedChange.LinesDeleted);
        Assert.Equal(Path.GetFullPath(untrackedFile), untrackedChange.FullPath);
        Assert.Equal("untracked.txt", untrackedChange.DisplayToolTip);
    }

    [Fact]
    public void WorkingTreeChange_DisplayToolTip_ReturnsPath()
    {
        WorkingTreeChange change = new() { Path = "Src/App.cs", FullPath = "/repo/Src/App.cs" };

        Assert.Equal("Src/App.cs", change.DisplayToolTip);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                DeleteDirectoryReadOnlySafe(_testDirectory);
            }
            catch
            {
            }
        }
    }

    private static void DeleteDirectoryReadOnlySafe(string targetDir)
    {
        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectoryReadOnlySafe(dir);
        }

        Directory.Delete(targetDir, false);
    }
}
