/// <summary>
/// Screen: CommitLogView
/// Purpose: Main repository workspace screen showing visual commit history graph, branches, tags, stashes, commit details, diff inspection, branch merging, interactive rebasing, system shortcuts, and authentication dialogs.
/// Available Functionalities:
/// - Visual commit graph rendering with multi-lane topology.
/// - Commit searching and filtering by message, author, or hash.
/// - Branch listing, checkout, and new branch creation modal.
/// - Branch merging modal with fast-forward strategy selection.
/// - Interactive rebase visual step editor (pick, reword, edit, squash, fixup, drop, step reordering).
/// - Ongoing repository operation (merge / rebase) conflict detection with Abort and Continue actions.
/// - Tag listing and new tag creation modal.
/// - Stash listing and new stash creation modal.
/// - Remote operations (Fetch, Pull, Push) with automatic credentials authentication dialog on challenge.
/// - System shortcuts: Open repository in system file manager, default terminal, or code editor (direct toolbar buttons on wide layouts or compact 'Open in' dropdown).
/// - Periodic automatic repository status refresh every 5 seconds.
/// - Credentials modal for HTTPS username/token and SSH key passphrase input.
/// - Working tree changes tracking (staged / unstaged) with bulk staging and commit composition.
/// - Amend previous commit toggle in commit composer.
/// - Commit file changes and structured diff inspection.
/// - Git configuration modal for user name and email settings.
/// - Commit context actions: Cherry-pick, Revert, Reset current branch (Soft, Mixed, Hard), Create branch or tag from commit, Interactive rebase onto commit.
/// Key UI Elements:
/// - CurrentBranchTextBlock, FetchButton, PullButton, PushButton, MergeButton.
/// - SeparateOpenActionsPanel, OpenFileManagerButton, OpenTerminalButton, OpenEditorButton.
/// - OpenInMenuButton, OpenInFileManagerMenuItem, OpenInTerminalMenuItem, OpenInEditorMenuItem.
/// - OperationStatusBanner, OperationTitleTextBlock, OperationSubtitleTextBlock, AbortOperationButton, ContinueOperationButton.
/// - SearchCommitsTextBox, BranchesTabButton, WorkingTreeTabButton, TagsTabButton, StashesTabButton.
/// - BranchesListBox, BranchesContextMenu, BranchMergeMenuItem, NewBranchActionButton.
/// - StagedChangesListBox, UnstagedChangesListBox, TagsListBox, StashesListBox, NewTagActionButton, NewStashActionButton.
/// - CommitMessageTextBox, AmendCommitCheckBox, CommitButton, StageAllButton, UnstageAllButton.
/// - CommitsListBox, CommitsContextMenu, ContextCherryPickMenuItem, ContextRevertMenuItem, ContextResetMenuItem, ContextResetSoftMenuItem, ContextResetMixedMenuItem, ContextResetHardMenuItem, ContextCreateBranchMenuItem, ContextCreateTagMenuItem, ContextInteractiveRebaseMenuItem.
/// - ChangedFilesListBox, DiffLinesItemsControl.
/// - CreateBranchModalBackdrop, MergeModalBackdrop, MergeBranchesComboBox, MergeFastForwardRadioButton, MergeNoFastForwardRadioButton, CancelMergeButton, ConfirmMergeButton.
/// - InteractiveRebaseModalBackdrop, RebaseUpstreamSubtitleTextBlock, RebaseStepsListBox, MoveUpStepButton, MoveDownStepButton, ActionPickButton, ActionRewordButton, ActionEditButton, ActionSquashButton, ActionFixupButton, ActionDropButton, RebaseStepMessageTextBox, CancelInteractiveRebaseButton, ConfirmInteractiveRebaseButton.
/// - CredentialsModalBackdrop, CredentialsSubtitleTextBlock, CredentialsHttpsRadioButton, CredentialsSshRadioButton, CredentialsUsernamePanel, CredentialsUsernameTextBox, CredentialsPasswordPanel, CredentialsPasswordLabelTextBlock, CredentialsPasswordTextBox, CredentialsErrorMessageTextBlock, CancelCredentialsButton, SubmitCredentialsButton.
/// - CreateTagModalBackdrop, CreateStashModalBackdrop, SettingsModalBackdrop, ResetConfirmModalBackdrop, CancelResetButton, ConfirmResetButton, CancelSettingsButton, ConfirmSettingsButton.
/// Navigation:
/// - Navigate From: RepositoryManagerView on opening a repository.
/// - Navigate To: RepositoryManagerView via shell header BackButton.
/// </summary>
namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
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
        this.OneWayBind(ViewModel, x => x.State.Tags, x => x.TagsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.Stashes, x => x.StashesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.FilteredCommits, x => x.CommitsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.SelectedCommitDetail!.ChangedFiles, x => x.ChangedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.DiffLines, x => x.DiffLinesItemsControl.ItemsSource);

        this.OneWayBind(ViewModel, x => x.State.IsCreateBranchModalVisible, x => x.CreateBranchModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsMergeModalVisible, x => x.MergeModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsInteractiveRebaseModalVisible, x => x.InteractiveRebaseModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.RebaseSteps, x => x.RebaseStepsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsCreateTagModalVisible, x => x.CreateTagModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsStashModalVisible, x => x.CreateStashModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsSettingsModalVisible, x => x.SettingsModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsResetConfirmModalVisible, x => x.ResetConfirmModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCredentialsModalVisible, x => x.CredentialsModalBackdrop.IsVisible);

        this.OneWayBind(ViewModel, x => x.State.ConfigUserNameInput, x => x.ConfigUserNameTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.ConfigUserEmailInput, x => x.ConfigUserEmailTextBox.Text);

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
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.FetchCommand, x => x.FetchButton);
        this.BindCommand(ViewModel, x => x.PullCommand, x => x.PullButton);
        this.BindCommand(ViewModel, x => x.PushCommand, x => x.PushButton);

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

        this.BindCommand(ViewModel, x => x.ShowCreateTagModalCommand, x => x.NewTagActionButton);
        this.BindCommand(ViewModel, x => x.HideCreateTagModalCommand, x => x.CancelCreateTagButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateTagCommand, x => x.ConfirmCreateTagButton);

        this.BindCommand(ViewModel, x => x.ShowStashModalCommand, x => x.NewStashActionButton);
        this.BindCommand(ViewModel, x => x.HideStashModalCommand, x => x.CancelCreateStashButton);
        this.BindCommand(ViewModel, x => x.ExecuteCreateStashCommand, x => x.ConfirmCreateStashButton);

        this.BindCommand(ViewModel, x => x.HideSettingsModalCommand, x => x.CancelSettingsButton);
        this.BindCommand(ViewModel, x => x.ExecuteSaveSettingsCommand, x => x.ConfirmSettingsButton);

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

    private bool IsAnyModalOpen()
    {
        if (ViewModel == null)
        {
            return false;
        }

        return ViewModel.State.IsCreateBranchModalVisible ||
               ViewModel.State.IsMergeModalVisible ||
               ViewModel.State.IsInteractiveRebaseModalVisible ||
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
        TagsTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.Tags);
        StashesTabButton.Click += (s, e) => ViewModel?.SetActiveSidebarTab(CommitLogSidebarTab.Stashes);

        this.WhenAnyValue(x => x.ViewModel!.State.ActiveSidebarTab)
            .Subscribe(Observer.Create<CommitLogSidebarTab>(tab =>
            {
                BranchesTabPanel.IsVisible = tab == CommitLogSidebarTab.Branches;
                WorkingTreeTabPanel.IsVisible = tab == CommitLogSidebarTab.WorkingTree;
                TagsTabPanel.IsVisible = tab == CommitLogSidebarTab.Tags;
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

        this.WhenAnyValue(x => x.NewStashMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateNewStashMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ConfigUserNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateConfigUserNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ConfigUserEmailTextBox.Text)
            .Subscribe(Observer.Create<string?>(email => ViewModel?.UpdateConfigUserEmailInput(email ?? string.Empty)))
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
}
