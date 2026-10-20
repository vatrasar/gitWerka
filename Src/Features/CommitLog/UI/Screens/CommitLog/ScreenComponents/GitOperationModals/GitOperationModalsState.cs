namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Enums;

/// <summary>
/// Immutable state record for the GitOperationModals smart component.
/// </summary>
public record GitOperationModalsState
{
    public bool IsCreateBranchModalVisible { get; init; }

    public string NewBranchNameInput { get; init; } = string.Empty;

    public bool IsCreateRemoteModalVisible { get; init; }

    public string NewRemoteNameInput { get; init; } = string.Empty;

    public string NewRemoteUrlInput { get; init; } = string.Empty;

    public bool IsCreateTagModalVisible { get; init; }

    public string NewTagNameInput { get; init; } = string.Empty;

    public string NewTagMessageInput { get; init; } = string.Empty;

    public bool IsStashModalVisible { get; init; }

    public string NewStashMessageInput { get; init; } = string.Empty;

    public bool IsResetConfirmModalVisible { get; init; }

    public GitResetType PendingResetType { get; init; } = GitResetType.Mixed;

    public CommitSummary? PendingResetCommit { get; init; }

    public bool IsMergeModalVisible { get; init; }

    public string? SelectedMergeBranch { get; init; }

    public GitMergeFastForwardMode MergeFastForwardMode { get; init; } = GitMergeFastForwardMode.FastForward;

    public ImmutableList<BranchInfo> AvailableBranches { get; init; } = ImmutableList<BranchInfo>.Empty;

    public bool IsCredentialsModalVisible { get; init; }

    public GitCredentialType CredentialsAuthType { get; init; } = GitCredentialType.Https;

    public string CredentialsUsernameInput { get; init; } = string.Empty;

    public string CredentialsPasswordInput { get; init; } = string.Empty;

    public string? CredentialsErrorMessage { get; init; }

    public RemoteOperationPending PendingRemoteOperation { get; init; } = RemoteOperationPending.None;
}
