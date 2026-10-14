namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Enums;

/// <summary>
/// Immutable state record for the CommitLog workspace screen.
/// </summary>
public record CommitLogState
{
    public string RepositoryPath { get; init; } = string.Empty;

    public string RepositoryName { get; init; } = string.Empty;

    public string CurrentBranch { get; init; } = string.Empty;

    public bool IsLoading { get; init; }

    public string SearchQuery { get; init; } = string.Empty;

    public CommitLogSidebarTab ActiveSidebarTab { get; init; } = CommitLogSidebarTab.Branches;

    public ImmutableList<CommitSummary> AllCommits { get; init; } = ImmutableList<CommitSummary>.Empty;

    public ImmutableList<CommitSummary> FilteredCommits { get; init; } = ImmutableList<CommitSummary>.Empty;

    public CommitSummary? SelectedCommit { get; init; }

    public CommitDetail? SelectedCommitDetail { get; init; }

    public ImmutableList<BranchInfo> Branches { get; init; } = ImmutableList<BranchInfo>.Empty;

    public ImmutableList<TagInfo> Tags { get; init; } = ImmutableList<TagInfo>.Empty;

    public ImmutableList<StashInfo> Stashes { get; init; } = ImmutableList<StashInfo>.Empty;

    public ImmutableList<RemoteInfo> Remotes { get; init; } = ImmutableList<RemoteInfo>.Empty;

    public ImmutableList<WorkingTreeChange> WorkingTreeChanges { get; init; } = ImmutableList<WorkingTreeChange>.Empty;

    public ImmutableList<WorkingTreeChange> StagedChanges => WorkingTreeChanges.FindAll(c => c.IsStaged);

    public ImmutableList<WorkingTreeChange> UnstagedChanges => WorkingTreeChanges.FindAll(c => !c.IsStaged);

    public FileDiffSummary? SelectedFileDiff { get; init; }

    public ImmutableList<DiffLine> DiffLines { get; init; } = ImmutableList<DiffLine>.Empty;

    public string CommitMessageInput { get; init; } = string.Empty;

    public int CommitSubjectRemainingChars
    {
        get
        {
            if (string.IsNullOrEmpty(CommitMessageInput))
            {
                return 72;
            }

            int newlineIndex = CommitMessageInput.IndexOf('\n');
            string firstLine = newlineIndex >= 0
                ? CommitMessageInput.Substring(0, newlineIndex).TrimEnd('\r')
                : CommitMessageInput;

            return 72 - firstLine.Length;
        }
    }

    public bool IsGeneratingAiCommit { get; init; }

    public string? AiErrorMessage { get; init; }

    public bool IsAiErrorSettingsActionVisible { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsCreateBranchModalVisible { get; init; }

    public string NewBranchNameInput { get; init; } = string.Empty;

    public bool IsCreateTagModalVisible { get; init; }

    public string NewTagNameInput { get; init; } = string.Empty;

    public string NewTagMessageInput { get; init; } = string.Empty;

    public bool IsStashModalVisible { get; init; }

    public string NewStashMessageInput { get; init; } = string.Empty;

    public bool IsSettingsModalVisible { get; init; }

    public string ConfigUserNameInput { get; init; } = string.Empty;

    public string ConfigUserEmailInput { get; init; } = string.Empty;

    public string DefaultProjectsDirectoryInput { get; init; } = string.Empty;

    public string AiApiKeyInput { get; init; } = string.Empty;

    public string AiModelInput { get; init; } = "google/gemini-2.5-flash";

    public string AiCommitStyleInput { get; init; } = "Conventional Commits: <type>(<scope>): <subject>";

    public string AiTokenLimitInput { get; init; } = "4000";

    public string AiProjectDescriptionInput { get; init; } = string.Empty;

    public bool IsResetConfirmModalVisible { get; init; }

    public GitResetType PendingResetType { get; init; } = GitResetType.Mixed;

    public CommitSummary? PendingResetCommit { get; init; }

    public RepositoryOperationStatus OperationStatus { get; init; } = new();

    public bool IsMergeModalVisible { get; init; }

    public string? SelectedMergeBranch { get; init; }

    public GitMergeFastForwardMode MergeFastForwardMode { get; init; } = GitMergeFastForwardMode.FastForward;

    public bool IsInteractiveRebaseModalVisible { get; init; }

    public string? RebaseUpstreamSha { get; init; }

    public string? RebaseUpstreamShortSha { get; init; }

    public ImmutableList<RebaseStep> RebaseSteps { get; init; } = ImmutableList<RebaseStep>.Empty;

    public RebaseStep? SelectedRebaseStep { get; init; }

    public bool IsAmendCommit { get; init; }

    public bool IsCredentialsModalVisible { get; init; }

    public GitCredentialType CredentialsAuthType { get; init; } = GitCredentialType.Https;

    public string CredentialsUsernameInput { get; init; } = string.Empty;

    public string CredentialsPasswordInput { get; init; } = string.Empty;

    public string? CredentialsErrorMessage { get; init; }

    public RemoteOperationPending PendingRemoteOperation { get; init; } = RemoteOperationPending.None;

    public ImmutableList<GitProfile> AvailableProfiles { get; init; } = ImmutableList<GitProfile>.Empty;

    public GitProfile? ActiveProfile { get; init; }

    public bool IsFavoritesDockOpen { get; init; }

    public RecentRepository? RecentNonFavoriteRepository { get; init; }

    public ImmutableList<RecentRepository> FavoriteRepositories { get; init; } = ImmutableList<RecentRepository>.Empty;

    public bool IsAddFavoriteModalVisible { get; init; }

    public string TargetFavoritePath { get; init; } = string.Empty;

    public string NewFavoriteNameInput { get; init; } = string.Empty;
}
