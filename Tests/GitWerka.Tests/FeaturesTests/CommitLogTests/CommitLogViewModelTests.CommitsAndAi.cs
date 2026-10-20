namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using ReactiveUI;
using Xunit;

public class CommitLogCommitsAndAiTests
{
    [Fact]
    public void UpdateCommitMessageInput_ValidText_UpdatesState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();

        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        vm.UpdateCommitMessageInput("feat: add new feature");

        Assert.Equal("feat: add new feature", vm.State.CommitMessageInput);
    }

    [Fact]
    public void ToggleAmendCommit_WhenEnabled_LoadsPreviousCommitMessageAndSetsAmendFlag()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "c1", Message = "First commit message" });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ToggleAmendCommit(true);

        Assert.True(vm.State.IsAmendCommit);
        Assert.Equal("First commit message", vm.State.CommitMessageInput);
    }

    [Fact]
    public void ToggleAmendCommit_WhenDisabled_ClearsAmendFlag()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);

        vm.ToggleAmendCommit(true);
        Assert.True(vm.State.IsAmendCommit);

        vm.ToggleAmendCommit(false);
        Assert.False(vm.State.IsAmendCommit);
    }

    [Fact]
    public async Task ExecuteCommit_WhenAmendEnabled_PassesAmendFlagAndResetsState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        gitService.Commits.Add(new CommitSummary { Sha = "c1", Message = "Original commit" });
        CommitLogViewModel vm = new(screen, "/repo/path", gitService);
        await vm.SelectProfileAsync(new GitProfile
        {
            Id = 1,
            Name = "Default Profile",
            Author = new GitAuthor { Name = "Tester", Email = "tester@example.com" }
        });

        vm.ToggleAmendCommit(true);
        vm.UpdateCommitMessageInput("Amended commit message");
        await vm.ExecuteCommitCommand.Execute().FirstAsync();

        Assert.True(gitService.LastCommitWasAmend);
        Assert.Equal("Amended commit message", gitService.LastCommitMessage);
        Assert.False(vm.State.IsAmendCommit);
        Assert.Equal(string.Empty, vm.State.CommitMessageInput);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenSuccessful_SetsCommitMessageInput()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ProposalToReturn = "feat(auth): add login flow" };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Equal("feat(auth): add login flow", vm.State.CommitMessageInput);
        Assert.Equal("feat(auth): add login flow", vm.WorkingTreePanel.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Null(vm.State.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenProposalSubjectExceedsLimit_TruncatesSubjectToHardLimit()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        string longSubject = new('A', 85);
        MockAiCommitMessageService aiService = new() { ProposalToReturn = longSubject };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Equal(72, vm.State.CommitMessageInput.Length);
        Assert.Equal(new string('A', 72), vm.State.CommitMessageInput);
        Assert.Equal(0, vm.State.CommitSubjectRemainingChars);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenProposalHasBody_ExtractsOnlySubjectUnderLimit()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        string longSubject = new('X', 80);
        string body = "\n\nThis is the detailed commit description.";
        MockAiCommitMessageService aiService = new() { ProposalToReturn = longSubject + body };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        string expected = new string('X', 72);
        Assert.Equal(expected, vm.State.CommitMessageInput);
        Assert.Equal(0, vm.State.CommitSubjectRemainingChars);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenFailsWithApiKeyError_SetsAiErrorMessageAndSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ExceptionToThrow = new InvalidOperationException("OpenRouter API key is not configured.") };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Empty(vm.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Equal("OpenRouter API key is not configured.", vm.State.AiErrorMessage);
        Assert.True(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenFailsWithNetworkError_SetsAiErrorMessageWithoutSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new() { ExceptionToThrow = new InvalidOperationException("Failed to connect to OpenRouter API.") };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Empty(vm.State.CommitMessageInput);
        Assert.False(vm.State.IsGeneratingAiCommit);
        Assert.Equal("Failed to connect to OpenRouter API.", vm.State.AiErrorMessage);
        Assert.False(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public void DismissAiErrorCommand_ClearsAiErrorMessageAndSettingsAction()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/test", gitService);

        vm.UpdateAiApiKeyInput("sk-test");
        vm.DismissAiErrorCommand.Execute().Subscribe();

        Assert.Null(vm.State.AiErrorMessage);
        Assert.False(vm.State.IsAiErrorSettingsActionVisible);
    }

    [Fact]
    public async Task ExecuteGenerateAiCommitMessageCommand_WhenRetryProgressReported_UpdatesStateWithRetryNotification()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiCommitMessageService aiService = new()
        {
            OnGenerate = progress =>
            {
                progress?.Report(new AiRetryAttempt(1, 3, TimeSpan.FromSeconds(2), true, "429"));
            }
        };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.True(vm.State.CommitMessageInput.Length > 0);
        Assert.False(vm.State.IsAiRetrying);
        Assert.Null(vm.State.AiErrorMessage);
    }

    [Fact]
    public async Task HandleAiRetryProgress_UpdatesStateWithCountdownAndRetryFlag()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        List<string> errorMessages = new();
        List<bool> retryFlags = new();

        MockAiCommitMessageService aiService = new()
        {
            OnGenerate = progress =>
            {
                progress?.Report(new AiRetryAttempt(1, 3, TimeSpan.FromSeconds(2), true, "429"));
            }
        };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiCommitMessageService: aiService);
        vm.WhenAnyValue(x => x.State.AiErrorMessage).Subscribe(Observer.Create<string?>(msg =>
        {
            if (msg != null)
            {
                errorMessages.Add(msg);
            }
        }));
        vm.WhenAnyValue(x => x.State.IsAiRetrying).Subscribe(Observer.Create<bool>(flag => retryFlags.Add(flag)));

        await vm.ExecuteGenerateAiCommitMessageCommand.Execute().FirstAsync();

        Assert.Contains(errorMessages, m => m.Contains("429") && m.Contains("2s"));
        Assert.Contains(true, retryFlags);
    }

    [Fact]
    public void DismissAiErrorCommand_WhenRetrying_ResetsRetryState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        CommitLogViewModel vm = new(screen, "/repo/test", gitService);

        vm.DismissAiErrorCommand.Execute().Subscribe();

        Assert.False(vm.State.IsAiRetrying);
        Assert.Null(vm.State.AiErrorMessage);
    }

    [Fact]
    public async Task ShowSettingsModal_LoadsAiConfigurationAndProjectDescription()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiConfigurationRepository aiRepo = new()
        {
            ConfigurationToReturn = new AiConfiguration
            {
                ApiKey = "sk-secret-123",
                Model = "anthropic/claude-3.5-sonnet",
                CommitStyle = "gitmoji",
                TokenLimit = 6000
            }
        };
        aiRepo.Descriptions["/repo/test"] = "Test repository project";

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiConfigurationRepository: aiRepo);

        await vm.ShowSettingsModalCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsSettingsModalVisible);
        Assert.Equal("sk-secret-123", vm.State.AiApiKeyInput);
        Assert.Equal("anthropic/claude-3.5-sonnet", vm.State.AiModelInput);
        Assert.Equal("gitmoji", vm.State.AiCommitStyleInput);
        Assert.Equal("6000", vm.State.AiTokenLimitInput);
        Assert.Equal("Test repository project", vm.State.AiProjectDescriptionInput);
    }

    [Fact]
    public async Task ExecuteSaveSettings_SavesAiConfigurationAndProjectDescription()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockAiConfigurationRepository aiRepo = new();

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, aiConfigurationRepository: aiRepo);

        vm.UpdateAiApiKeyInput("sk-new-key");
        vm.UpdateAiModelInput("google/gemini-2.5-flash");
        vm.UpdateAiCommitStyleInput("Conventional Commits");
        vm.UpdateAiTokenLimitInput("5000");
        vm.UpdateAiProjectDescriptionInput("Updated description");

        await vm.ExecuteSaveSettingsCommand.Execute().FirstAsync();

        Assert.NotNull(aiRepo.LastSavedConfiguration);
        Assert.Equal("sk-new-key", aiRepo.LastSavedConfiguration.ApiKey);
        Assert.Equal("google/gemini-2.5-flash", aiRepo.LastSavedConfiguration.Model);
        Assert.Equal("Conventional Commits", aiRepo.LastSavedConfiguration.CommitStyle);
        Assert.Equal(5000, aiRepo.LastSavedConfiguration.TokenLimit);

        Assert.NotNull(aiRepo.LastSavedDescription);
        Assert.Equal("/repo/test", aiRepo.LastSavedDescription.Value.Path);
        Assert.Equal("Updated description", aiRepo.LastSavedDescription.Value.Desc);
    }

    [Fact]
    public async Task LoadApplicationSettings_PopulatesDefaultProjectsDirectoryInState()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository settingsRepo = new()
        {
            Settings = new ApplicationSettings { DefaultProjectsDirectory = "/custom/projects/path" }
        };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, applicationSettingsRepository: settingsRepo);

        await vm.LoadApplicationSettingsCommand.Execute().FirstAsync();

        Assert.Equal("/custom/projects/path", vm.State.DefaultProjectsDirectory);
        Assert.Equal("/custom/projects/path", vm.State.DefaultProjectsDirectoryInput);
    }

    [Fact]
    public async Task ExecuteSaveSettings_UpdatesDefaultProjectsDirectoryInStateAndRepository()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository settingsRepo = new();

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, applicationSettingsRepository: settingsRepo);

        vm.UpdateDefaultProjectsDirectoryInput("/new/projects/path");

        await vm.ExecuteSaveSettingsCommand.Execute().FirstAsync();

        Assert.Equal("/new/projects/path", vm.State.DefaultProjectsDirectory);
        Assert.Equal("/new/projects/path", settingsRepo.Settings.DefaultProjectsDirectory);
    }

    [Fact]
    public async Task ShowSettingsModal_LoadsMaxVisibleBranchesFromApplicationSettings()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository appSettingsRepo = new()
        {
            Settings = new ApplicationSettings { MaxVisibleBranches = 7, DefaultProjectsDirectory = "/test/projects" }
        };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, applicationSettingsRepository: appSettingsRepo);

        await vm.ShowSettingsModalCommand.Execute().FirstAsync();

        Assert.True(vm.State.IsSettingsModalVisible);
        Assert.Equal("7", vm.State.MaxVisibleBranchesInput);
        Assert.Equal("/test/projects", vm.State.DefaultProjectsDirectoryInput);
    }

    [Fact]
    public async Task ExecuteSaveSettings_SavesMaxVisibleBranchesToApplicationSettings()
    {
        MockScreen screen = new();
        MockGitService gitService = new();
        MockApplicationSettingsRepository appSettingsRepo = new()
        {
            Settings = new ApplicationSettings { MaxVisibleBranches = 5 }
        };

        CommitLogViewModel vm = new(screen, "/repo/test", gitService, applicationSettingsRepository: appSettingsRepo);

        vm.UpdateMaxVisibleBranchesInput("9");
        vm.UpdateDefaultProjectsDirectoryInput("/new/projects");

        await vm.ExecuteSaveSettingsCommand.Execute().FirstAsync();

        Assert.Equal(9, appSettingsRepo.Settings.MaxVisibleBranches);
        Assert.Equal("/new/projects", appSettingsRepo.Settings.DefaultProjectsDirectory);
    }

    [Theory]
    [InlineData("", 72)]
    [InlineData("feat: short title", 55)]
    [InlineData("feat: exactly seventy-two characters long title for conventional commit!", 0)]
    [InlineData("feat: a commit title that is seventy-five characters long for test purpose!", -3)]
    [InlineData("feat: title line\n\nLong body line with lots of details and explanations.", 56)]
    public void CommitSubjectRemainingChars_CalculatesFirstLineRemainingAccurately(string input, int expectedRemaining)
    {
        CommitLogState state = new() { CommitMessageInput = input };
        Assert.Equal(expectedRemaining, state.CommitSubjectRemainingChars);
    }
}
