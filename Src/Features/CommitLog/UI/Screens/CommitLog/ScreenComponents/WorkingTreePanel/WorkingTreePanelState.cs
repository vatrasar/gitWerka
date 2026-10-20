namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;

/// <summary>
/// Immutable state record for the WorkingTreePanel smart component.
/// </summary>
public record WorkingTreePanelState
{
    public ImmutableList<WorkingTreeChange> WorkingTreeChanges { get; init; } = ImmutableList<WorkingTreeChange>.Empty;

    public ImmutableList<WorkingTreeChange> StagedChanges => WorkingTreeChanges.FindAll(c => c.IsStaged && c.Status != FileChangeStatus.Conflicted);

    public ImmutableList<WorkingTreeChange> UnstagedChanges => WorkingTreeChanges.FindAll(c => !c.IsStaged && c.Status != FileChangeStatus.Conflicted);

    public ImmutableList<WorkingTreeChange> ConflictedFiles => WorkingTreeChanges.FindAll(c => c.Status == FileChangeStatus.Conflicted);

    public ImmutableList<WorkingTreeChange> ResolvedFiles => WorkingTreeChanges.FindAll(c => c.IsStaged);

    public bool IsInConflictMode => ConflictedFiles.Count > 0 || OperationStatus.CurrentOperation == RepositoryOperationType.Merge || OperationStatus.HasConflicts;

    public string MergeSourceBranch { get; init; } = string.Empty;

    public string MergeTargetBranch { get; init; } = string.Empty;

    public string CommitMessageInput { get; init; } = string.Empty;

    public int CommitSubjectRemainingChars
    {
        get
        {
            if (string.IsNullOrEmpty(CommitMessageInput))
            {
                return IAiCommitMessageService.MaxCommitSubjectLength;
            }

            int newlineIndex = CommitMessageInput.IndexOf('\n');
            string firstLine = newlineIndex >= 0
                ? CommitMessageInput.Substring(0, newlineIndex).TrimEnd('\r')
                : CommitMessageInput;

            return IAiCommitMessageService.MaxCommitSubjectLength - firstLine.Length;
        }
    }

    public bool IsAmendCommit { get; init; }

    public bool IsGeneratingAiCommit { get; init; }

    public string? AiErrorMessage { get; init; }

    public bool IsAiErrorSettingsActionVisible { get; init; }

    public GitProfile? ActiveProfile { get; init; }

    public RepositoryOperationStatus OperationStatus { get; init; } = new();
}
