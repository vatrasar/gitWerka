namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
using Xunit;

public class CommitLogProfilesTests
{

    [Fact]
    public async Task SelectProfileCommand_SetsActiveProfileAndSavesMapping()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@example.com" };
        GitProfileCredentials creds = new() { Id = 1, Username = "alicegh", AccessToken = "token" };
        GitProfile profile = new() { Id = 5, Name = "AliceProfile", AuthorId = 1, CredentialsId = 1, Author = author, Credentials = creds };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        Assert.NotNull(vm.State.ActiveProfile);
        Assert.Equal("AliceProfile", vm.State.ActiveProfile.Name);
        Assert.True(profileRepo.Mappings.ContainsKey("/my/repo"));
        Assert.Equal(5, profileRepo.Mappings["/my/repo"]);
    }

    [Fact]
    public async Task PushCommand_WhenActiveProfileHasCredentials_AutomaticallyPassesCredentials()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 1, Name = "Alice", Email = "alice@example.com" };
        GitProfileCredentials creds = new() { Id = 1, Username = "alicegh", AccessToken = "ghp_secure_access_token_999" };
        GitProfile profile = new() { Id = 7, Name = "AliceProfile", AuthorId = 1, CredentialsId = 1, Author = author, Credentials = creds };
        profileRepo.Profiles.Add(profile);
        profileRepo.Mappings["/my/repo"] = 7;

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        await vm.PushCommand.Execute().FirstAsync();

        Assert.True(gitService.PushCalled);
        Assert.NotNull(gitService.LastCredentialsUsed);
        Assert.Equal("alicegh", gitService.LastCredentialsUsed.Username);
        Assert.Equal("ghp_secure_access_token_999", gitService.LastCredentialsUsed.PasswordOrToken);
        Assert.Equal(GitCredentialType.Https, gitService.LastCredentialsUsed.Type);
    }

    [Fact]
    public async Task CreateCommitCommand_WhenActiveProfileHasAuthor_UsesAuthorForCommitSignature()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        TestProfileRepository profileRepo = new();

        GitAuthor author = new() { Id = 3, Name = "Bob Dev", Email = "bob@company.com" };
        GitProfile profile = new() { Id = 9, Name = "WorkProfile", AuthorId = 3, Author = author };
        profileRepo.Profiles.Add(profile);

        CommitLogViewModel vm = new(screen, "/my/repo", gitService, profileRepository: profileRepo);
        await vm.SelectProfileAsync(profile);

        vm.UpdateCommitMessageInput("feat: add feature");
        await vm.ExecuteCommitCommand.Execute().FirstAsync();

        Assert.Equal("feat: add feature", gitService.LastCommitMessage);
        Assert.Equal("Bob Dev", gitService.LastCommitAuthorName);
        Assert.Equal("bob@company.com", gitService.LastCommitAuthorEmail);
    }

    [Fact]
    public void ExecuteCommit_WhenNoActiveProfile_CanCommitIsFalseAndExecutionIsBlocked()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        List<AppStatus> recorded = new();
        statusService.Status.Subscribe(s => recorded.Add(s));
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, appStatusService: statusService);

        bool? canCommitInitial = null;
        vm.CanCommit.Subscribe(c => canCommitInitial = c);
        Assert.False(canCommitInitial);

        bool canExecute = false;
        vm.ExecuteCommitCommand.CanExecute.Subscribe(c => canExecute = c);
        Assert.False(canExecute);

        Assert.Null(gitService.LastCommitMessage);
    }

    [Fact]
    public async Task ExecuteCommit_WhenProfileSelected_CanCommitBecomesTrue()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        List<bool> canCommitStates = new();
        vm.CanCommit.Subscribe(canCommitStates.Add);

        Assert.False(canCommitStates.Last());

        await vm.SelectProfileAsync(new GitProfile
        {
            Id = 1,
            Name = "Dev Profile",
            Author = new GitAuthor { Name = "Dev", Email = "dev@example.com" }
        });

        Assert.True(canCommitStates.Last());

        await vm.SelectProfileAsync(null);

        Assert.False(canCommitStates.Last());
    }

    [Fact]
    public async Task ExecuteCommit_UpdatesAppStatus_ToInProgress_AndThenSuccess()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        List<AppStatus> recorded = new();
        statusService.Status.Subscribe(s => recorded.Add(s));
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, appStatusService: statusService);
        await vm.SelectProfileAsync(new GitProfile
        {
            Id = 1,
            Name = "Dev",
            Author = new GitAuthor { Name = "Dev", Email = "dev@example.com" }
        });

        vm.UpdateCommitMessageInput("feat: test commit status");
        await vm.ExecuteCommitCommand.Execute().FirstAsync();

        Assert.Contains(recorded, s => s.Message == CommitLogStrings.CommittingChanges && s.Severity == AppStatusSeverity.InProgress);
        Assert.Contains(recorded, s => s.Message == CommitLogStrings.CommitSucceeded && s.Severity == AppStatusSeverity.Success);
    }

    [Fact]
    public async Task ExecuteCommit_WhenGitServiceThrows_UpdatesAppStatusToError()
    {
        MockScreen screen = new();
        MockGitService gitService = new() { CreateCommitException = new InvalidOperationException("Failed to write tree") };
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        List<AppStatus> recorded = new();
        statusService.Status.Subscribe(s => recorded.Add(s));
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, appStatusService: statusService);
        await vm.SelectProfileAsync(new GitProfile
        {
            Id = 1,
            Name = "Dev",
            Author = new GitAuthor { Name = "Dev", Email = "dev@example.com" }
        });

        vm.UpdateCommitMessageInput("feat: failing commit");
        await vm.ExecuteCommitCommand.Execute().FirstAsync();

        Assert.Contains(recorded, s => s.Message == CommitLogStrings.CommittingChanges && s.Severity == AppStatusSeverity.InProgress);
        Assert.Contains(recorded, s => s.Message.StartsWith(CommitLogStrings.CommitFailed) && s.Severity == AppStatusSeverity.Error);
    }

    [Fact]
    public async Task PushCommand_UpdatesAppStatus_ToInProgress_AndThenSuccess()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        List<AppStatus> recorded = new();
        statusService.Status.Subscribe(s => recorded.Add(s));
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, appStatusService: statusService);

        await vm.PushCommand.Execute().FirstAsync();

        Assert.Contains(recorded, s => s.Message == CommitLogStrings.PushingChanges && s.Severity == AppStatusSeverity.InProgress);
        Assert.Contains(recorded, s => s.Message == CommitLogStrings.PushSucceeded && s.Severity == AppStatusSeverity.Success);
    }

    [Fact]
    public async Task PushCommand_WhenGitServiceThrows_UpdatesAppStatusToError()
    {
        MockScreen screen = new();
        MockGitService gitService = new() { PushException = new InvalidOperationException("Remote rejected branch") };
        HistoricalScheduler scheduler = new();
        using AppStatusService statusService = new(scheduler);
        List<AppStatus> recorded = new();
        statusService.Status.Subscribe(s => recorded.Add(s));
        CommitLogViewModel vm = new(screen, "/repo/path", gitService, appStatusService: statusService);

        await vm.PushCommand.Execute().FirstAsync();

        Assert.Contains(recorded, s => s.Message == CommitLogStrings.PushingChanges && s.Severity == AppStatusSeverity.InProgress);
        Assert.Contains(recorded, s => s.Message.StartsWith(CommitLogStrings.PushFailed) && s.Severity == AppStatusSeverity.Error);
    }
}
