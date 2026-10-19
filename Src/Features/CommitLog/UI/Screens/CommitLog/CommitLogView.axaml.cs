/// <summary>
/// Screen: CommitLogView
/// Purpose: Main repository workspace screen showing visual commit history graph, branches, remotes, stashes, commit details, diff inspection, branch merging, interactive rebasing, system shortcuts, and authentication dialogs.
/// Available Functionalities:
/// - Visual commit graph rendering with multi-lane topology, dynamic column width sizing, and configurable maximum visible branches limit (default 5 recently updated branches).
/// - Responsive commit item layout with CommitTitlePanel: badges and title side-by-side when space allows, badges on top and wrapped title below when constrained.
/// - Commit searching and filtering by message, author, or hash.
/// - Branch listing, checkout, and new branch creation modal.
/// - Branch merging modal with fast-forward strategy selection.
/// - Interactive rebase visual step editor (pick, reword, edit, squash, fixup, drop, step reordering).
/// - Ongoing repository operation (merge / rebase) conflict detection with Abort and Continue actions.
/// - Remote repositories listing, add remote modal, per-remote fetch and delete actions.
/// - Stash listing, new stash creation modal, and per-stash pop and drop actions.
/// - Remote operations (Fetch, Pull, Push) with automatic credentials authentication dialog on challenge.
/// - Profiles management: switch active Git profile from toolbar dropdown menu, or navigate to ProfileManagementView.
/// - System shortcuts: Open repository in system file manager, default terminal, or code editor (direct toolbar buttons on wide layouts or compact 'Open in' dropdown).
/// - Periodic automatic repository status refresh every 5 seconds and automatic background remote fetch every 30 seconds.
/// - Uncommitted working tree changes displayed directly as a // WIP row in the commit history graph (GitKraken style).
/// - Credentials modal for HTTPS username/token and SSH key passphrase input.
/// - Working tree changes tracking (staged / unstaged) with bulk staging and commit composition.
/// - GitKraken-style merge conflict resolution: replaces staged/unstaged changes with Conflicted Files and Resolved Files when conflicts are detected, provides Mark All Resolved action, and opens 3-panel ConflictResolverView on file selection.
/// - Amend previous commit toggle in commit composer.
/// - AI-assisted commit message generation from staged changes via OpenRouter integration with automatic retry mechanism on rate limits (429) and status notifications.
/// - Commit file changes and structured diff inspection.
/// - Git configuration modal for user name and email settings, default projects directory, max visible branches in graph, AI API key, model, commit style, token limit, and per-repository project description.
/// - Commit context actions: Cherry-pick, Revert, Reset current branch (Soft, Mixed, Hard), Create branch or tag from commit, Interactive rebase onto commit.
/// - Favorites drawer dock: shows recently opened repository not in favorites, grouped and ungrouped favorite repositories with expandable categories, add and edit favorite with custom name and group modal, remove from favorites button, switch repository on item selection.
/// Key UI Elements:
/// - FavoritesToggleButton, FavoritesDockBackdrop, FavoritesDockPanel, CloseFavoritesDockButton, OpenFolderFromDockButton.
/// - RecentNonFavoriteSectionPanel, RecentNonFavoriteNameTextBlock, RecentNonFavoritePathTextBlock, OpenRecentNonFavoriteRepoButton, AddRecentToFavoritesButton.
/// - FavoritesDividerRectangle, EmptyFavoritesTextBlock, FavoritesScrollViewer, FavoritesContainerPanel, FavoriteGroupsItemsControl, UngroupedFavoritesSectionHeaderTextBlock, UngroupedFavoritesItemsControl.
/// - AddFavoriteModalBackdrop, FavoriteModalTitleTextBlock, FavoriteNameTextBox, FavoriteGroupAutoCompleteBox, TargetFavoritePathTextBlock, CancelAddFavoriteButton, ConfirmAddFavoriteButton.
/// - CurrentBranchTextBlock, FetchButton, PullButton, PushButton, MergeButton.
/// - SeparateOpenActionsPanel, OpenFileManagerButton, OpenTerminalButton, OpenEditorButton.
/// - OpenInMenuButton, OpenInFileManagerMenuItem, OpenInTerminalMenuItem, OpenInEditorMenuItem.
/// - OperationStatusBanner, OperationTitleTextBlock, OperationSubtitleTextBlock, AbortOperationButton, ContinueOperationButton.
/// - SearchCommitsTextBox, BranchesTabButton, WorkingTreeTabButton, RemotesTabButton, StashesTabButton.
/// - BranchesListBox, BranchesContextMenu, BranchMergeMenuItem, NewBranchActionButton.
/// - RemotesListBox, RemotesContextMenu, RemoteFetchMenuItem, RemoteDeleteMenuItem, ItemFetchRemoteButton, ItemDeleteRemoteButton, NewRemoteActionButton.
/// - CreateRemoteModalBackdrop, NewRemoteNameTextBox, NewRemoteUrlTextBox, CancelCreateRemoteButton, ConfirmCreateRemoteButton.
/// - StagedChangesListBox, UnstagedChangesListBox, StashesListBox, StashesContextMenu, StashPopMenuItem, StashDropMenuItem, ItemPopStashButton, ItemDropStashButton, NewStashActionButton.
/// - NormalWorkingTreePanel, ConflictWorkingTreePanel, ConflictedFilesHeaderTextBlock, MarkAllResolvedButton, ConflictedFilesListBox, ResolvedFilesHeaderTextBlock, ResolvedFilesListBox, ConflictMergingDetailsTextBlock.
/// - CommitMessageTextBox, AmendCommitCheckBox, CommitMessageRemainingCountTextBlock, CommitButton, GenerateAiCommitButton, GenerateAiCommitNormalIcon, GenerateAiCommitLoadingIcon, GenerateAiCommitButtonText, AiGeneratingProgressBar, AiErrorBanner, AiErrorIcon, AiErrorTextBlock, AiErrorSettingsButton, AiErrorDismissButton, StageAllButton, UnstageAllButton.
/// - CommitsListBox, CommitsContextMenu, ContextCherryPickMenuItem, ContextRevertMenuItem, ContextResetMenuItem, ContextResetSoftMenuItem, ContextResetMixedMenuItem, ContextResetHardMenuItem, ContextCreateBranchMenuItem, ContextCreateTagMenuItem, ContextInteractiveRebaseMenuItem.
/// - ChangedFilesListBox, DiffLinesItemsControl.
/// - CreateBranchModalBackdrop, MergeModalBackdrop, MergeBranchesComboBox, MergeFastForwardRadioButton, MergeNoFastForwardRadioButton, CancelMergeButton, ConfirmMergeButton.
/// - InteractiveRebaseModalBackdrop, RebaseUpstreamSubtitleTextBlock, RebaseStepsListBox, MoveUpStepButton, MoveDownStepButton, ActionPickButton, ActionRewordButton, ActionEditButton, ActionSquashButton, ActionFixupButton, ActionDropButton, RebaseStepMessageTextBox, CancelInteractiveRebaseButton, ConfirmInteractiveRebaseButton.
/// - CredentialsModalBackdrop, CredentialsSubtitleTextBlock, CredentialsHttpsRadioButton, CredentialsSshRadioButton, CredentialsUsernamePanel, CredentialsUsernameTextBox, CredentialsPasswordPanel, CredentialsPasswordLabelTextBlock, CredentialsPasswordTextBox, CredentialsErrorMessageTextBlock, CancelCredentialsButton, SubmitCredentialsButton.
/// - CreateTagModalBackdrop, CreateStashModalBackdrop, SettingsModalBackdrop, ResetConfirmModalBackdrop, CancelResetButton, ConfirmResetButton, CancelSettingsButton, ConfirmSettingsButton, DefaultProjectsDirectoryTextBox, BrowseDefaultProjectsDirectoryButton, AiApiKeyTextBox, AiModelTextBox, AiTokenLimitTextBox, AiCommitStyleTextBox, AiProjectDescriptionTextBox.
/// Navigation:
/// - Navigate From: RepositoryManagerView on opening a repository.
/// - Navigate To: RepositoryManagerView via shell header BackButton; ConflictResolverView on selecting a conflicted or resolved file.
/// </summary>
namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Domain.Enums;
using GitWerka.Features.CommitLog.Resources;
using ReactiveUI;

public partial class CommitLogView : ReactiveUserControl<CommitLogViewModel>
{
    public CommitLogView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel?.LoadRepositoryDataCommand.Execute().Subscribe();
            SetupBindings(disposables);
            SetupCommands(disposables);
            SetupTabSwitching(disposables);
            SetupTextInputs(disposables);
            SetupSelectionHandlers(disposables);
            SetupResponsiveLayout(disposables);
            SetupAutoRefresh(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.CurrentBranch, x => x.CurrentBranchTextBlock.Text);
        this.OneWayBind(ViewModel, x => x.State.Branches, x => x.BranchesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.StagedChanges, x => x.StagedChangesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.UnstagedChanges, x => x.UnstagedChangesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.ConflictedFiles, x => x.ConflictedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.ResolvedFiles, x => x.ResolvedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsInConflictMode, x => x.ConflictWorkingTreePanel.IsVisible);

        this.WhenAnyValue(x => x.ViewModel!.State.IsInConflictMode)
            .Subscribe(Observer.Create<bool>(inConflict => NormalWorkingTreePanel.IsVisible = !inConflict))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ConflictedFiles.Count)
            .Subscribe(Observer.Create<int>(count =>
            {
                ConflictedFilesHeaderTextBlock.Text = string.Format(CommitLogStrings.ConflictedFilesHeader, count);
                MarkAllResolvedButton.IsVisible = count > 0;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ResolvedFiles.Count)
            .Subscribe(Observer.Create<int>(count =>
            {
                ResolvedFilesHeaderTextBlock.Text = string.Format(CommitLogStrings.ResolvedFilesHeader, count);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.MergeSourceBranch, x => x.ViewModel!.State.MergeTargetBranch)
            .Subscribe(Observer.Create<(string Source, string Target)>(t =>
            {
                ConflictMergingDetailsTextBlock.Text = string.Format(CommitLogStrings.MergingBranchFormat, t.Source, t.Target);
            }))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.Remotes, x => x.RemotesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.Stashes, x => x.StashesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.FilteredCommits, x => x.CommitsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.SelectedCommitDetail!.ChangedFiles, x => x.ChangedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.DiffLines, x => x.DiffLinesItemsControl.ItemsSource);

        this.OneWayBind(ViewModel, x => x.State.IsCreateBranchModalVisible, x => x.CreateBranchModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsMergeModalVisible, x => x.MergeModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsInteractiveRebaseModalVisible, x => x.InteractiveRebaseModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.RebaseSteps, x => x.RebaseStepsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsCreateRemoteModalVisible, x => x.CreateRemoteModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCreateTagModalVisible, x => x.CreateTagModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsStashModalVisible, x => x.CreateStashModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsSettingsModalVisible, x => x.SettingsModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsResetConfirmModalVisible, x => x.ResetConfirmModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCredentialsModalVisible, x => x.CredentialsModalBackdrop.IsVisible);

        this.OneWayBind(ViewModel, x => x.State.IsFavoritesDockOpen, x => x.FavoritesDockBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.FavoriteGroups, x => x.FavoriteGroupsItemsControl.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.UngroupedFavoriteRepositories, x => x.UngroupedFavoritesItemsControl.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsAddFavoriteModalVisible, x => x.AddFavoriteModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.TargetFavoritePath, x => x.TargetFavoritePathTextBlock.Text);

        this.WhenAnyValue(x => x.ViewModel!.State.IsEditingExistingFavorite)
            .Subscribe(Observer.Create<bool>(isEditing =>
            {
                FavoriteModalTitleTextBlock.Text = isEditing
                    ? CommitLogStrings.EditFavoriteModalTitle
                    : CommitLogStrings.AddFavoriteModalTitle;
                ConfirmAddFavoriteButton.Content = isEditing
                    ? Shared.Resources.GlobalStrings.SaveButton
                    : CommitLogStrings.ConfirmAddFavoriteButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(
            x => x.ViewModel!.State.FavoriteGroups.Count,
            x => x.ViewModel!.State.UngroupedFavoriteRepositories.Count)
            .Subscribe(Observer.Create<(int Groups, int Ungrouped)>(counts =>
            {
                UngroupedFavoritesSectionHeaderTextBlock.IsVisible = counts.Groups > 0 && counts.Ungrouped > 0;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.RecentNonFavoriteRepository)
            .Subscribe(Observer.Create<RecentRepository?>(repo =>
            {
                RecentNonFavoriteSectionPanel.IsVisible = repo != null;
                FavoritesDividerRectangle.IsVisible = repo != null;
                RecentNonFavoriteNameTextBlock.Text = repo?.DisplayName ?? string.Empty;
                RecentNonFavoritePathTextBlock.Text = repo?.Path ?? string.Empty;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.FavoriteRepositories.Count)
            .Select(count => count == 0)
            .Subscribe(Observer.Create<bool>(empty => EmptyFavoritesTextBlock.IsVisible = empty))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.IsAddFavoriteModalVisible)
            .Where(visible => visible)
            .Subscribe(Observer.Create<bool>(_ => FavoriteNameTextBox.Focus()))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.ConfigUserNameInput, x => x.ConfigUserNameTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.ConfigUserEmailInput, x => x.ConfigUserEmailTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.DefaultProjectsDirectoryInput, x => x.DefaultProjectsDirectoryTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.MaxVisibleBranchesInput, x => x.MaxVisibleBranchesTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiApiKeyInput, x => x.AiApiKeyTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiModelInput, x => x.AiModelTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiTokenLimitInput, x => x.AiTokenLimitTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiCommitStyleInput, x => x.AiCommitStyleTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiProjectDescriptionInput, x => x.AiProjectDescriptionTextBox.Text);

        this.WhenAnyValue(x => x.ViewModel!.State.IsGeneratingAiCommit)
            .Subscribe(Observer.Create<bool>(generating =>
            {
                GenerateAiCommitButton.IsEnabled = !generating;
                GenerateAiCommitNormalIcon.IsVisible = !generating;
                GenerateAiCommitLoadingIcon.IsVisible = generating;
                AiGeneratingProgressBar.IsVisible = generating;
                GenerateAiCommitButtonText.Text = generating
                    ? CommitLogStrings.GeneratingAiCommitMessage
                    : CommitLogStrings.GenerateAiCommitButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiErrorMessage)
            .Subscribe(Observer.Create<string?>(err =>
            {
                AiErrorBanner.IsVisible = !string.IsNullOrWhiteSpace(err);
                AiErrorTextBlock.Text = err ?? string.Empty;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.IsAiRetrying)
            .Subscribe(Observer.Create<bool>(isRetrying =>
            {
                AiErrorBanner.Classes.Set("AiRetryState", isRetrying);
                AiErrorIcon.Kind = isRetrying
                    ? Material.Icons.MaterialIconKind.ClockOutline
                    : Material.Icons.MaterialIconKind.AlertCircleOutline;
            }))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.IsAiErrorSettingsActionVisible, x => x.AiErrorSettingsButton.IsVisible);

        this.WhenAnyValue(x => x.ViewModel!.State.OperationStatus)
            .Subscribe(Observer.Create<RepositoryOperationStatus>(status =>
            {
                OperationStatusBanner.IsVisible = status != null && status.CurrentOperation != RepositoryOperationType.None;
                OperationTitleTextBlock.Text = GetOperationTitle(status);
                OperationSubtitleTextBlock.Text = GetOperationSubtitle(status);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.Branches)
            .Subscribe(Observer.Create<ImmutableList<BranchInfo>>(branches =>
            {
                MergeBranchesComboBox.ItemsSource = branches?.Where(b => !b.IsCurrentHead).ToList();
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedMergeBranch)
            .Subscribe(Observer.Create<string?>(selectedName =>
            {
                if (MergeBranchesComboBox.ItemsSource is IEnumerable<BranchInfo> items)
                {
                    MergeBranchesComboBox.SelectedItem = items.FirstOrDefault(b => b.Name == selectedName);
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.RebaseUpstreamShortSha)
            .Subscribe(Observer.Create<string?>(sha =>
            {
                RebaseUpstreamSubtitleTextBlock.Text = string.Format(CommitLogStrings.InteractiveRebaseSubtitle, sha ?? string.Empty);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedRebaseStep)
            .Subscribe(Observer.Create<RebaseStep?>(step =>
            {
                RebaseStepMessageTextBox.Text = step?.NewMessage ?? string.Empty;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.IsAmendCommit)
            .Subscribe(Observer.Create<bool>(isAmend =>
            {
                AmendCommitCheckBox.IsChecked = isAmend;
                CommitButton.Content = isAmend ? CommitLogStrings.AmendCommitButton : CommitLogStrings.CommitButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CommitMessageInput)
            .Subscribe(Observer.Create<string>(msg =>
            {
                if (CommitMessageTextBox.Text != msg)
                {
                    CommitMessageTextBox.Text = msg;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CommitSubjectRemainingChars)
            .Subscribe(Observer.Create<int>(remaining =>
            {
                if (remaining >= 0)
                {
                    CommitMessageRemainingCountTextBlock.Text = string.Format(CommitLogStrings.CommitSubjectRemainingChars, remaining);
                }
                else
                {
                    CommitMessageRemainingCountTextBlock.Text = string.Format(CommitLogStrings.CommitSubjectOverLimitChars, Math.Abs(remaining));
                }

                CommitMessageRemainingCountTextBlock.Classes.Set("CommitCounterWarning", remaining >= 0 && remaining <= 10);
                CommitMessageRemainingCountTextBlock.Classes.Set("CommitCounterError", remaining < 0);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsAuthType)
            .Subscribe(Observer.Create<GitCredentialType>(type =>
            {
                bool isHttps = type == GitCredentialType.Https;
                CredentialsHttpsRadioButton.IsChecked = isHttps;
                CredentialsSshRadioButton.IsChecked = !isHttps;
                CredentialsUsernamePanel.IsVisible = isHttps;
                CredentialsPasswordLabelTextBlock.Text = isHttps
                    ? CommitLogStrings.CredentialsPasswordLabel
                    : CommitLogStrings.CredentialsPassphraseLabel;
                CredentialsSubtitleTextBlock.Text = isHttps
                    ? CommitLogStrings.CredentialsHttpsSubtitle
                    : CommitLogStrings.CredentialsSshSubtitle;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsUsernameInput)
            .Subscribe(Observer.Create<string>(user =>
            {
                if (CredentialsUsernameTextBox.Text != user)
                {
                    CredentialsUsernameTextBox.Text = user;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsPasswordInput)
            .Subscribe(Observer.Create<string>(pass =>
            {
                if (CredentialsPasswordTextBox.Text != pass)
                {
                    CredentialsPasswordTextBox.Text = pass;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsErrorMessage)
            .Subscribe(Observer.Create<string?>(err =>
            {
                CredentialsErrorMessageTextBlock.IsVisible = !string.IsNullOrWhiteSpace(err);
                CredentialsErrorMessageTextBlock.Text = err ?? string.Empty;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedCommit)
            .Subscribe(Observer.Create<CommitSummary?>(commit =>
            {
                bool isRealCommit = commit != null && !commit.IsWip;
                ContextCherryPickMenuItem.IsEnabled = isRealCommit;
                ContextRevertMenuItem.IsEnabled = isRealCommit;
                ContextResetMenuItem.IsEnabled = isRealCommit;
                ContextCreateBranchMenuItem.IsEnabled = isRealCommit;
                ContextCreateTagMenuItem.IsEnabled = isRealCommit;
                ContextInteractiveRebaseMenuItem.IsEnabled = isRealCommit;
            }))
            .DisposeWith(disposables);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.FetchCommand, x => x.FetchButton);
        this.BindCommand(ViewModel, x => x.PullCommand, x => x.PullButton);
        this.BindCommand(ViewModel, x => x.PushCommand, x => x.PushButton);
        this.BindCommand(ViewModel, x => x.MarkAllResolvedCommand, x => x.MarkAllResolvedButton);

        this.BindCommand(ViewModel, x => x.ToggleFavoritesDockCommand, x => x.FavoritesToggleButton);
        this.BindCommand(ViewModel, x => x.CloseFavoritesDockCommand, x => x.CloseFavoritesDockButton);
        this.BindCommand(ViewModel, x => x.HideAddFavoriteModalCommand, x => x.CancelAddFavoriteButton);
        this.BindCommand(ViewModel, x => x.ConfirmAddFavoriteCommand, x => x.ConfirmAddFavoriteButton);

        FavoritesBackdropDismissBorder.PointerPressed += (s, e) => ViewModel?.CloseFavoritesDockCommand.Execute().Subscribe();
        OpenFolderFromDockButton.Click += async (s, e) => await HandleOpenFolderFromDockClickAsync();
        OpenRecentNonFavoriteRepoButton.Click += (s, e) => HandleOpenRecentNonFavoriteRepoClick();
        AddRecentToFavoritesButton.Click += (s, e) => HandleAddRecentToFavoritesClick();
        SetupFavoritesList(disposables);

        this.BindCommand(ViewModel, x => x.ShowCreateBranchModalCommand, x => x.NewBranchActionButton);
        this.BindCommand(ViewModel, x => x.HideCreateBranchModalCommand, x => x.CancelCreateBranchButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateBranchCommand, x => x.ConfirmCreateBranchButton);

        MergeButton.Click += (s, e) => ViewModel?.ShowMergeModalCommand.Execute(null!).Subscribe();
        BranchMergeMenuItem.Click += (s, e) => HandleBranchMergeMenuItemClick();
        this.BindCommand(ViewModel, x => x.HideMergeModalCommand, x => x.CancelMergeButton);
        this.BindCommand(ViewModel, x => x.ExecuteMergeCommand, x => x.ConfirmMergeButton);
        MergeFastForwardRadioButton.IsCheckedChanged += (s, e) => HandleFastForwardOptionChanged();
        MergeNoFastForwardRadioButton.IsCheckedChanged += (s, e) => HandleNoFastForwardOptionChanged();

        ContextInteractiveRebaseMenuItem.Click += (s, e) => HandleInteractiveRebaseMenuItemClick();
        this.BindCommand(ViewModel, x => x.HideInteractiveRebaseModalCommand, x => x.CancelInteractiveRebaseButton);
        this.BindCommand(ViewModel, x => x.ExecuteInteractiveRebaseCommand, x => x.ConfirmInteractiveRebaseButton);
        MoveUpStepButton.Click += (s, e) => ViewModel?.MoveRebaseStepUpCommand.Execute(null!).Subscribe();
        MoveDownStepButton.Click += (s, e) => ViewModel?.MoveRebaseStepDownCommand.Execute(null!).Subscribe();

        ActionPickButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Pick);
        ActionRewordButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Reword);
        ActionEditButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Edit);
        ActionSquashButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Squash);
        ActionFixupButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Fixup);
        ActionDropButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Drop);

        this.BindCommand(ViewModel, x => x.AbortOperationCommand, x => x.AbortOperationButton);
        this.BindCommand(ViewModel, x => x.ContinueOperationCommand, x => x.ContinueOperationButton);

        this.BindCommand(ViewModel, x => x.ShowCreateRemoteModalCommand, x => x.NewRemoteActionButton);
        this.BindCommand(ViewModel, x => x.HideCreateRemoteModalCommand, x => x.CancelCreateRemoteButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateRemoteCommand, x => x.ConfirmCreateRemoteButton);
        SetupRemotesList(disposables);

        this.BindCommand(ViewModel, x => x.HideCreateTagModalCommand, x => x.CancelCreateTagButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateTagCommand, x => x.ConfirmCreateTagButton);

        this.BindCommand(ViewModel, x => x.ShowStashModalCommand, x => x.NewStashActionButton);
        this.BindCommand(ViewModel, x => x.HideStashModalCommand, x => x.CancelCreateStashButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateStashCommand, x => x.ConfirmCreateStashButton);
        SetupStashesList(disposables);

        this.BindCommand(ViewModel, x => x.HideSettingsModalCommand, x => x.CancelSettingsButton);
        this.BindCommand(ViewModel, x => x.ExecuteSaveSettingsCommand, x => x.ConfirmSettingsButton);
        BrowseDefaultProjectsDirectoryButton.Click += async (s, e) => await HandleBrowseDefaultProjectsDirectoryClickAsync();

        this.BindCommand(ViewModel, x => x.ExecuteGenerateAiCommitMessageCommand, x => x.GenerateAiCommitButton);
        this.BindCommand(ViewModel, x => x.DismissAiErrorCommand, x => x.AiErrorDismissButton);
        this.BindCommand(ViewModel, x => x.ShowSettingsModalCommand, x => x.AiErrorSettingsButton);
        this.BindCommand(ViewModel, x => x.ExecuteCommitCommand, x => x.CommitButton);
        AmendCommitCheckBox.IsCheckedChanged += (s, e) => HandleAmendCommitOptionChanged();
        this.BindCommand(ViewModel, x => x.StageAllCommand, x => x.StageAllButton);
        this.BindCommand(ViewModel, x => x.UnstageAllCommand, x => x.UnstageAllButton);

        this.BindCommand(ViewModel, x => x.CherryPickCommitCommand, x => x.ContextCherryPickMenuItem);
        this.BindCommand(ViewModel, x => x.RevertCommitCommand, x => x.ContextRevertMenuItem);
        this.BindCommand(ViewModel, x => x.CreateBranchFromCommitCommand, x => x.ContextCreateBranchMenuItem);
        this.BindCommand(ViewModel, x => x.CreateTagFromCommitCommand, x => x.ContextCreateTagMenuItem);
        this.BindCommand(ViewModel, x => x.CancelResetBranchCommand, x => x.CancelResetButton);
        this.BindCommand(ViewModel, x => x.ExecuteResetBranchCommand, x => x.ConfirmResetButton);

        ContextResetSoftMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Soft).Subscribe();
        ContextResetMixedMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Mixed).Subscribe();
        ContextResetHardMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Hard).Subscribe();

        this.BindCommand(ViewModel, x => x.OpenFileManagerCommand, x => x.OpenFileManagerButton);
        this.BindCommand(ViewModel, x => x.OpenTerminalCommand, x => x.OpenTerminalButton);
        this.BindCommand(ViewModel, x => x.OpenEditorCommand, x => x.OpenEditorButton);

        this.BindCommand(ViewModel, x => x.OpenFileManagerCommand, x => x.OpenInFileManagerMenuItem);
        this.BindCommand(ViewModel, x => x.OpenTerminalCommand, x => x.OpenInTerminalMenuItem);
        this.BindCommand(ViewModel, x => x.OpenEditorCommand, x => x.OpenInEditorMenuItem);

        this.BindCommand(ViewModel, x => x.CancelCredentialsCommand, x => x.CancelCredentialsButton);
        this.BindCommand(ViewModel, x => x.SubmitCredentialsCommand, x => x.SubmitCredentialsButton);

        CredentialsHttpsRadioButton.IsCheckedChanged += (s, e) => HandleCredentialsAuthTypeChanged();
        CredentialsSshRadioButton.IsCheckedChanged += (s, e) => HandleCredentialsAuthTypeChanged();
    }

    private void SetupResponsiveLayout(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.Bounds.Width)
            .Subscribe(Observer.Create<double>(width =>
            {
                bool isWide = width >= 1050;
                SeparateOpenActionsPanel.IsVisible = isWide;
                OpenInMenuButton.IsVisible = !isWide;
            }))
            .DisposeWith(disposables);
    }

    private void SetupAutoRefresh(CompositeDisposable disposables)
    {
        Observable.Interval(TimeSpan.FromSeconds(5), RxApp.MainThreadScheduler)
            .Subscribe(Observer.Create<long>(_ => TriggerPeriodicAutoRefresh()))
            .DisposeWith(disposables);

        Observable.Interval(TimeSpan.FromSeconds(30), RxApp.MainThreadScheduler)
            .Subscribe(Observer.Create<long>(_ => TriggerPeriodicAutoFetch()))
            .DisposeWith(disposables);
    }

    private void TriggerPeriodicAutoRefresh()
    {
        if (ViewModel == null || ViewModel.State.IsLoading)
        {
            return;
        }

        if (IsAnyModalOpen())
        {
            return;
        }

        ViewModel.LoadRepositoryDataCommand.Execute().Subscribe();
    }

    private void TriggerPeriodicAutoFetch()
    {
        if (ViewModel == null || ViewModel.State.IsLoading)
        {
            return;
        }

        if (IsAnyModalOpen())
        {
            return;
        }

        ViewModel.AutoFetchCommand.Execute().Subscribe();
    }

    private bool IsAnyModalOpen()
    {
        if (ViewModel == null)
        {
            return false;
        }

        return ViewModel.State.IsCreateBranchModalVisible ||
               ViewModel.State.IsMergeModalVisible ||
               ViewModel.State.IsInteractiveRebaseModalVisible ||
               ViewModel.State.IsCreateRemoteModalVisible ||
               ViewModel.State.IsCreateTagModalVisible ||
               ViewModel.State.IsStashModalVisible ||
               ViewModel.State.IsSettingsModalVisible ||
               ViewModel.State.IsResetConfirmModalVisible ||
               ViewModel.State.IsCredentialsModalVisible;
    }

    private void HandleCredentialsAuthTypeChanged()
    {
        if (CredentialsHttpsRadioButton.IsChecked == true)
        {
            ViewModel?.SetCredentialsAuthType(GitCredentialType.Https);
            return;
        }

        if (CredentialsSshRadioButton.IsChecked == true)
        {
            ViewModel?.SetCredentialsAuthType(GitCredentialType.SshKeyPassphrase);
        }
    }

    private void HandleAmendCommitOptionChanged()
    {
        bool isChecked = AmendCommitCheckBox.IsChecked == true;
        if (ViewModel != null && ViewModel.State.IsAmendCommit != isChecked)
        {
            ViewModel.ToggleAmendCommit(isChecked);
        }
    }

    private void HandleBranchMergeMenuItemClick()
    {
        if (BranchesListBox.SelectedItem is BranchInfo branch && ViewModel != null)
        {
            ViewModel.ShowMergeModalCommand.Execute(branch.Name).Subscribe();
        }
    }

    private void HandleFastForwardOptionChanged()
    {
        if (MergeFastForwardRadioButton.IsChecked == true)
        {
            ViewModel?.SetMergeFastForwardMode(GitMergeFastForwardMode.FastForward);
        }
    }

    private void HandleNoFastForwardOptionChanged()
    {
        if (MergeNoFastForwardRadioButton.IsChecked == true)
        {
            ViewModel?.SetMergeFastForwardMode(GitMergeFastForwardMode.NoFastForward);
        }
    }

    private void HandleInteractiveRebaseMenuItemClick()
    {
        if (CommitsListBox.SelectedItem is CommitSummary commit && ViewModel != null)
        {
            ViewModel.StartInteractiveRebaseCommand.Execute(commit).Subscribe();
        }
    }

    private void ApplyRebaseAction(RebaseStepAction action)
    {
        if (ViewModel?.State.SelectedRebaseStep is RebaseStep step)
        {
            ViewModel.ChangeRebaseStepAction(step, action);
        }
    }

    private void SetupTabSwitching(CompositeDisposable disposables)
    {
        BranchesTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.Branches);
        WorkingTreeTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.WorkingTree);
        RemotesTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.Remotes);
        StashesTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.Stashes);

        this.WhenAnyValue(x => x.ViewModel!.State.ActiveSidebarTab)
            .Subscribe(Observer.Create<CommitLogSidebarTab>(tab =>
            {
                BranchesTabPanel.IsVisible = tab == CommitLogSidebarTab.Branches;
                WorkingTreeTabPanel.IsVisible = tab == CommitLogSidebarTab.WorkingTree;
                RemotesTabPanel.IsVisible = tab == CommitLogSidebarTab.Remotes;
                StashesTabPanel.IsVisible = tab == CommitLogSidebarTab.Stashes;
            }))
            .DisposeWith(disposables);
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.SearchCommitsTextBox.Text)
            .Skip(1)
            .Subscribe(Observer.Create<string?>(query => ViewModel?.UpdateSearchQuery(query ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CommitMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateCommitMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewBranchNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewBranchNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewTagNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewTagNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewTagMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateNewTagMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewRemoteNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewRemoteNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewRemoteUrlTextBox.Text)
            .Subscribe(Observer.Create<string?>(url => ViewModel?.UpdateNewRemoteUrlInput(url ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewStashMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateNewStashMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ConfigUserNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateConfigUserNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ConfigUserEmailTextBox.Text)
            .Subscribe(Observer.Create<string?>(email => ViewModel?.UpdateConfigUserEmailInput(email ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.DefaultProjectsDirectoryTextBox.Text)
            .Subscribe(Observer.Create<string?>(dir => ViewModel?.UpdateDefaultProjectsDirectoryInput(dir ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.MaxVisibleBranchesTextBox.Text)
            .Subscribe(Observer.Create<string?>(branches => ViewModel?.UpdateMaxVisibleBranchesInput(branches ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiApiKeyTextBox.Text)
            .Subscribe(Observer.Create<string?>(key => ViewModel?.UpdateAiApiKeyInput(key ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiModelTextBox.Text)
            .Subscribe(Observer.Create<string?>(model => ViewModel?.UpdateAiModelInput(model ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiTokenLimitTextBox.Text)
            .Subscribe(Observer.Create<string?>(limit => ViewModel?.UpdateAiTokenLimitInput(limit ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiCommitStyleTextBox.Text)
            .Subscribe(Observer.Create<string?>(style => ViewModel?.UpdateAiCommitStyleInput(style ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiProjectDescriptionTextBox.Text)
            .Subscribe(Observer.Create<string?>(desc => ViewModel?.UpdateAiProjectDescriptionInput(desc ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.RebaseStepMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(text =>
            {
                if (ViewModel?.State.SelectedRebaseStep is RebaseStep step && text != null && text != step.NewMessage)
                {
                    ViewModel.UpdateRebaseStepMessage(step, text);
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsUsernameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateCredentialsUsernameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsPasswordTextBox.Text)
            .Subscribe(Observer.Create<string?>(pass => ViewModel?.UpdateCredentialsPasswordInput(pass ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.FavoriteNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewFavoriteNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.NewFavoriteNameInput)
            .Subscribe(Observer.Create<string>(name =>
            {
                if (FavoriteNameTextBox.Text != name)
                {
                    FavoriteNameTextBox.Text = name;
                }
            }))
            .DisposeWith(disposables);

        FavoriteNameTextBox.KeyUp += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ViewModel?.ConfirmAddFavoriteCommand.Execute().Subscribe();
            }
        };

        this.WhenAnyValue(x => x.FavoriteGroupAutoCompleteBox.Text)
            .Subscribe(Observer.Create<string?>(group => ViewModel?.UpdateNewFavoriteGroupInput(group ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.NewFavoriteGroupInput)
            .Subscribe(Observer.Create<string>(group =>
            {
                if (FavoriteGroupAutoCompleteBox.Text != group)
                {
                    FavoriteGroupAutoCompleteBox.Text = group;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ExistingGroupNames)
            .DistinctUntilChanged()
            .Subscribe(Observer.Create<ImmutableList<string>>(groups =>
            {
                if (!ReferenceEquals(FavoriteGroupAutoCompleteBox.ItemsSource, groups))
                {
                    FavoriteGroupAutoCompleteBox.ItemsSource = groups;
                }
            }))
            .DisposeWith(disposables);

        FavoriteGroupAutoCompleteBox.GotFocus += (s, e) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (string.IsNullOrWhiteSpace(FavoriteGroupAutoCompleteBox.Text) &&
                    FavoriteGroupAutoCompleteBox.ItemsSource is IEnumerable<string> items &&
                    items.Any())
                {
                    FavoriteGroupAutoCompleteBox.IsDropDownOpen = true;
                }
            });
        };

        FavoriteGroupAutoCompleteBox.KeyUp += (s, e) =>
        {
            if (e.Key == Key.Enter && !FavoriteGroupAutoCompleteBox.IsDropDownOpen)
            {
                ViewModel?.ConfirmAddFavoriteCommand.Execute().Subscribe();
            }
        };
    }

    private void SetupSelectionHandlers(CompositeDisposable disposables)
    {
        CommitsListBox.SelectionChanged += async (sender, e) =>
        {
            if (CommitsListBox.SelectedItem is CommitSummary selected && ViewModel != null)
            {
                await ViewModel.SelectCommitAsync(selected);
            }
        };

        ChangedFilesListBox.SelectionChanged += async (sender, e) =>
        {
            if (ChangedFilesListBox.SelectedItem is FileDiffSummary selected && ViewModel != null)
            {
                await ViewModel.SelectFileDiffAsync(selected);
            }
        };

        BranchesListBox.DoubleTapped += (sender, e) =>
        {
            if (BranchesListBox.SelectedItem is BranchInfo branch && ViewModel != null)
            {
                ViewModel.CheckoutBranchCommand.Execute(branch.Name).Subscribe();
            }
        };

        UnstagedChangesListBox.DoubleTapped += (sender, e) =>
        {
            if (UnstagedChangesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.StageFileCommand.Execute(item.Path).Subscribe();
            }
        };

        StagedChangesListBox.DoubleTapped += (sender, e) =>
        {
            if (StagedChangesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.UnstageFileCommand.Execute(item.Path).Subscribe();
            }
        };

        MergeBranchesComboBox.SelectionChanged += (sender, e) =>
        {
            if (MergeBranchesComboBox.SelectedItem is BranchInfo branch && ViewModel != null)
            {
                ViewModel.SelectMergeBranch(branch.Name);
            }
        };

        RebaseStepsListBox.SelectionChanged += (sender, e) =>
        {
            if (RebaseStepsListBox.SelectedItem is RebaseStep step && ViewModel != null)
            {
                ViewModel.SelectRebaseStep(step);
            }
        };

        ConflictedFilesListBox.SelectionChanged += (sender, e) =>
        {
            if (ConflictedFilesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.OpenConflictResolver(item);
                ConflictedFilesListBox.SelectedItem = null;
            }
        };

        ResolvedFilesListBox.SelectionChanged += (sender, e) =>
        {
            if (ResolvedFilesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.OpenConflictResolver(item);
                ResolvedFilesListBox.SelectedItem = null;
            }
        };
    }

    private static string GetOperationTitle(RepositoryOperationStatus? status)
    {
        if (status == null)
        {
            return string.Empty;
        }

        if (status.CurrentOperation == RepositoryOperationType.Merge)
        {
            return CommitLogStrings.OngoingMergeTitle;
        }

        if (status.CurrentOperation == RepositoryOperationType.Rebase)
        {
            if (status.TotalSteps > 0)
            {
                return $"{CommitLogStrings.OngoingRebaseTitle} ({status.CurrentStep}/{status.TotalSteps})";
            }

            return CommitLogStrings.OngoingRebaseTitle;
        }

        return string.Empty;
    }

    private static string GetOperationSubtitle(RepositoryOperationStatus? status)
    {
        if (status == null)
        {
            return string.Empty;
        }

        if (status.HasConflicts)
        {
            return CommitLogStrings.ConflictsDetectedSubtitle;
        }

        return status.Message ?? string.Empty;
    }

    private void SetupFavoritesList(CompositeDisposable disposables)
    {
        FavoritesScrollViewer.AddHandler(Button.ClickEvent, (sender, e) =>
        {
            if (e.Source is not Visual source || ViewModel == null)
            {
                return;
            }

            Button? button = source as Button ?? source.FindAncestorOfType<Button>(true);
            if (button == null)
            {
                return;
            }

            if (button.Classes.Contains("GroupHeaderButton") && button.DataContext is FavoriteRepositoryGroup group)
            {
                ViewModel.ToggleFavoriteGroupCommand.Execute(group.Name).Subscribe();
                e.Handled = true;
                return;
            }

            if (button.Classes.Contains("RepoSelectButton") && button.DataContext is RecentRepository repoToOpen)
            {
                ViewModel.OpenRepositoryFromDockCommand.Execute(repoToOpen.Path).Subscribe();
                e.Handled = true;
                return;
            }

            if (button.Classes.Contains("EditFavoriteButton") && button.DataContext is RecentRepository repoToEdit)
            {
                ViewModel.ShowEditFavoriteModalCommand.Execute(repoToEdit.Path).Subscribe();
                e.Handled = true;
                return;
            }

            if (button.Classes.Contains("DeleteFavoriteButton") && button.DataContext is RecentRepository repoToDelete)
            {
                ViewModel.RemoveFromFavoritesCommand.Execute(repoToDelete.Path).Subscribe();
                e.Handled = true;
                return;
            }
        });
    }

    private void SetupRemotesList(CompositeDisposable disposables)
    {
        RemotesListBox.SelectionChanged += (sender, e) =>
        {
            if (RemotesListBox.SelectedItem is RemoteInfo remote && ViewModel != null)
            {
                ViewModel.SelectRemote(remote);
            }
        };

        RemoteFetchMenuItem.Click += (sender, e) =>
        {
            if (RemotesListBox.SelectedItem is RemoteInfo remote && ViewModel != null)
            {
                ViewModel.FetchRemoteCommand.Execute(remote.Name).Subscribe();
            }
        };

        RemoteDeleteMenuItem.Click += (sender, e) =>
        {
            if (RemotesListBox.SelectedItem is RemoteInfo remote && ViewModel != null)
            {
                ViewModel.DeleteRemoteCommand.Execute(remote.Name).Subscribe();
            }
        };

        RemotesListBox.AddHandler(Button.ClickEvent, (sender, e) => HandleRemoteItemButtonClick(e));
    }

    private void HandleRemoteItemButtonClick(RoutedEventArgs e)
    {
        if (e.Source is not Visual source || ViewModel == null)
        {
            return;
        }

        Button? button = source as Button ?? source.FindAncestorOfType<Button>(true);
        if (button?.DataContext is not RemoteInfo remote)
        {
            return;
        }

        if (button.Name == "ItemFetchRemoteButton")
        {
            ViewModel.FetchRemoteCommand.Execute(remote.Name).Subscribe();
            e.Handled = true;
        }
        else if (button.Name == "ItemDeleteRemoteButton")
        {
            ViewModel.DeleteRemoteCommand.Execute(remote.Name).Subscribe();
            e.Handled = true;
        }
    }

    private void SetupStashesList(CompositeDisposable disposables)
    {
        StashesListBox.SelectionChanged += (sender, e) =>
        {
            if (StashesListBox.SelectedItem is StashInfo stash && ViewModel != null)
            {
                ViewModel.SelectStash(stash);
            }
        };

        StashPopMenuItem.Click += (sender, e) =>
        {
            if (StashesListBox.SelectedItem is StashInfo stash && ViewModel != null)
            {
                ViewModel.PopStashCommand.Execute(stash.Index).Subscribe();
            }
        };

        StashDropMenuItem.Click += (sender, e) =>
        {
            if (StashesListBox.SelectedItem is StashInfo stash && ViewModel != null)
            {
                ViewModel.DropStashCommand.Execute(stash.Index).Subscribe();
            }
        };

        StashesListBox.AddHandler(Button.ClickEvent, (sender, e) => HandleStashItemButtonClick(e));
    }

    private void HandleStashItemButtonClick(RoutedEventArgs e)
    {
        if (e.Source is not Visual source || ViewModel == null)
        {
            return;
        }

        Button? button = source as Button ?? source.FindAncestorOfType<Button>(true);
        if (button?.DataContext is not StashInfo stash)
        {
            return;
        }

        if (button.Name == "ItemPopStashButton")
        {
            ViewModel.PopStashCommand.Execute(stash.Index).Subscribe();
            e.Handled = true;
        }
        else if (button.Name == "ItemDropStashButton")
        {
            ViewModel.DropStashCommand.Execute(stash.Index).Subscribe();
            e.Handled = true;
        }
    }

    private void HandleOpenRecentNonFavoriteRepoClick()
    {
        if (ViewModel?.State.RecentNonFavoriteRepository != null)
        {
            ViewModel.OpenRepositoryFromDockCommand.Execute(ViewModel.State.RecentNonFavoriteRepository.Path).Subscribe();
        }
    }

    private void HandleAddRecentToFavoritesClick()
    {
        if (ViewModel?.State.RecentNonFavoriteRepository != null)
        {
            ViewModel.ShowAddFavoriteModalCommand.Execute(ViewModel.State.RecentNonFavoriteRepository.Path).Subscribe();
        }
    }

    private async Task HandleOpenFolderFromDockClickAsync()
    {
        string? pickedPath = await PickFolderAsync();
        if (!string.IsNullOrEmpty(pickedPath) && ViewModel != null)
        {
            ViewModel.OpenRepositoryFromDockCommand.Execute(pickedPath).Subscribe();
        }
    }

    private async Task<string?> PickFolderAsync(string? suggestedPath = null)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return null;
        }

        IStorageFolder? startLocation = null;
        string? effectivePath = !string.IsNullOrWhiteSpace(suggestedPath)
            ? suggestedPath
            : ViewModel?.State.DefaultProjectsDirectory;

        if (!string.IsNullOrWhiteSpace(effectivePath) && Directory.Exists(effectivePath))
        {
            startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(effectivePath);
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Git Repository Directory",
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
        });

        if (folders.Count == 0)
        {
            return null;
        }

        return folders[0].Path.LocalPath;
    }

    private async Task HandleBrowseDefaultProjectsDirectoryClickAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        IStorageFolder? startLocation = null;
        string? currentInput = ViewModel?.State.DefaultProjectsDirectoryInput;
        if (!string.IsNullOrWhiteSpace(currentInput) && Directory.Exists(currentInput))
        {
            startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentInput);
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Default Projects Directory",
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
        });

        if (folders.Count > 0 && ViewModel != null)
        {
            ViewModel.UpdateDefaultProjectsDirectoryInput(folders[0].Path.LocalPath);
        }
    }
}
