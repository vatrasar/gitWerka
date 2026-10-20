/// <summary>
/// Screen: CommitLogView
/// Purpose: Main repository workspace screen showing visual commit history graph, branches, remotes, stashes, commit details, diff inspection, branch merging, interactive rebasing, system shortcuts, and authentication dialogs.
/// Available Functionalities:
/// - Visual commit graph rendering with multi-lane topology, dynamic column width sizing, and configurable maximum visible branches limit (default 5 recently updated branches).
/// - Responsive commit item layout with CommitTitlePanel: badges and title side-by-side when space allows, badges on top and wrapped title below when constrained.
/// - Commit searching and filtering by message, author, or hash.
/// - Branch listing with default 'All branches' (or recently modified count when exceeding visibility limit) filter option, per-branch commit filtering, checkout on double click, and new branch creation modal.
/// - Branch merging modal with fast-forward strategy selection.
/// - Interactive rebase visual step editor (pick, reword, edit, squash, fixup, drop, step reordering).
/// - Ongoing repository operation (merge / rebase) conflict detection with Abort and Continue actions.
/// - Remote repositories listing, add remote modal, per-remote fetch and delete actions.
/// - Stash listing, new stash creation modal, and per-stash pop and drop actions.
/// - Remote operations (Fetch, Pull, Push) with automatic credentials authentication dialog on challenge.
/// - Profiles management: switch active Git profile from toolbar dropdown menu, or navigate to ProfileManagementView; enforce active Git profile requirement to execute commits with alert guidance.
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
/// - Commit context actions: Checkout commit, Cherry-pick, Revert, Reset current branch (Soft, Mixed, Hard), Create branch or tag from commit, Interactive rebase onto commit.
/// - Favorites drawer dock: shows recently opened repository not in favorites, grouped and ungrouped favorite repositories with expandable categories, add and edit favorite with custom name and group modal, remove from favorites button, switch repository on item selection.
/// Key UI Elements:
/// - Smart Components: WorkingTreePanelControl, FavoritesDockControl, SettingsModalControl, InteractiveRebaseModalControl, GitModalsControl.
/// - FavoritesToggleButton, CurrentBranchTextBlock, FetchButton, PullButton, PushButton, MergeButton.
/// - SeparateOpenActionsPanel, OpenFileManagerButton, OpenTerminalButton, OpenEditorButton.
/// - OpenInMenuButton, OpenInFileManagerMenuItem, OpenInTerminalMenuItem, OpenInEditorMenuItem.
/// - OperationStatusBanner, OperationTitleTextBlock, OperationSubtitleTextBlock, AbortOperationButton, ContinueOperationButton.
/// - SearchCommitsTextBox, BranchesTabButton, WorkingTreeTabButton, RemotesTabButton, StashesTabButton.
/// - BranchesListBox, BranchesContextMenu, BranchMergeMenuItem, NewBranchActionButton.
/// - RemotesListBox, RemotesContextMenu, RemoteFetchMenuItem, RemoteDeleteMenuItem, ItemFetchRemoteButton, ItemDeleteRemoteButton, NewRemoteActionButton.
/// - StashesListBox, StashesContextMenu, StashPopMenuItem, StashDropMenuItem, ItemPopStashButton, ItemDropStashButton, NewStashActionButton.
/// - CommitsListBox, CommitsContextMenu, ContextCheckoutCommitMenuItem, ContextCherryPickMenuItem, ContextRevertMenuItem, ContextResetMenuItem, ContextResetSoftMenuItem, ContextResetMixedMenuItem, ContextResetHardMenuItem, ContextCreateBranchMenuItem, ContextCreateTagMenuItem, ContextInteractiveRebaseMenuItem.
/// - ChangedFilesListBox, DiffLinesItemsControl.
/// Navigation:
/// - Navigate From: RepositoryManagerView on opening a repository.
/// - Navigate To: RepositoryManagerView via shell header BackButton; ConflictResolverView on selecting a conflicted or resolved file.
/// </summary>
namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.FavoritesDock, x => x.FavoritesDockControl.ViewModel);
        this.OneWayBind(ViewModel, x => x.SettingsModal, x => x.SettingsModalControl.ViewModel);
        this.OneWayBind(ViewModel, x => x.InteractiveRebaseModal, x => x.InteractiveRebaseModalControl.ViewModel);
        this.OneWayBind(ViewModel, x => x.GitModals, x => x.GitModalsControl.ViewModel);
        this.OneWayBind(ViewModel, x => x.WorkingTreePanel, x => x.WorkingTreePanelControl.ViewModel);

        this.OneWayBind(ViewModel, x => x.State.CurrentBranch, x => x.CurrentBranchTextBlock.Text);
        this.OneWayBind(ViewModel, x => x.State.BranchListItems, x => x.BranchesListBox.ItemsSource);

        this.WhenAnyValue(x => x.ViewModel!.State.BranchListItems, x => x.ViewModel!.State.SelectedBranchName)
            .Subscribe(Observer.Create<(ImmutableList<BranchListItem> Items, string? SelectedName)>(t =>
            {
                if (t.Items.IsEmpty)
                {
                    return;
                }

                BranchListItem? match = string.IsNullOrEmpty(t.SelectedName)
                    ? t.Items.FirstOrDefault(i => i.IsAllBranches)
                    : t.Items.FirstOrDefault(i => string.Equals(i.Name, t.SelectedName, StringComparison.OrdinalIgnoreCase));

                BranchListItem? target = match ?? t.Items.FirstOrDefault();
                if (!ReferenceEquals(BranchesListBox.SelectedItem, target))
                {
                    BranchesListBox.SelectedItem = target;
                }
            }))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.Remotes, x => x.RemotesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.Stashes, x => x.StashesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.FilteredCommits, x => x.CommitsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.SelectedCommitDetail!.ChangedFiles, x => x.ChangedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.DiffLines, x => x.DiffLinesItemsControl.ItemsSource);

        this.WhenAnyValue(x => x.ViewModel!.State.OperationStatus)
            .Subscribe(Observer.Create<RepositoryOperationStatus>(status =>
            {
                OperationStatusBanner.IsVisible = status != null && status.CurrentOperation != RepositoryOperationType.None;
                OperationTitleTextBlock.Text = GetOperationTitle(status);
                OperationSubtitleTextBlock.Text = GetOperationSubtitle(status);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedCommit)
            .Subscribe(Observer.Create<CommitSummary?>(commit =>
            {
                bool isRealCommit = commit != null && !commit.IsWip;
                ContextCheckoutCommitMenuItem.IsEnabled = isRealCommit;
                ContextCherryPickMenuItem.IsEnabled = isRealCommit;
                ContextRevertMenuItem.IsEnabled = isRealCommit;
                ContextResetMenuItem.IsEnabled = isRealCommit;
                ContextCreateBranchMenuItem.IsEnabled = isRealCommit;
                ContextCreateTagMenuItem.IsEnabled = isRealCommit;
                ContextInteractiveRebaseMenuItem.IsEnabled = isRealCommit;
            }))
            .DisposeWith(disposables);
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

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.FetchCommand, x => x.FetchButton);
        this.BindCommand(ViewModel, x => x.PullCommand, x => x.PullButton);
        this.BindCommand(ViewModel, x => x.PushCommand, x => x.PushButton);

        this.BindCommand(ViewModel, x => x.ToggleFavoritesDockCommand, x => x.FavoritesToggleButton);

        this.BindCommand(ViewModel, x => x.ShowCreateBranchModalCommand, x => x.NewBranchActionButton);

        MergeButton.Click += (s, e) => ViewModel?.ShowMergeModalCommand.Execute(null!).Subscribe();
        BranchMergeMenuItem.Click += (s, e) => HandleBranchMergeMenuItemClick();
        BranchesContextMenu.Opening += (s, e) =>
        {
            bool isRealBranch = BranchesListBox.SelectedItem is BranchListItem item && !item.IsAllBranches;
            BranchMergeMenuItem.IsEnabled = isRealBranch;
        };

        ContextInteractiveRebaseMenuItem.Click += (s, e) => HandleInteractiveRebaseMenuItemClick();

        this.BindCommand(ViewModel, x => x.AbortOperationCommand, x => x.AbortOperationButton);
        this.BindCommand(ViewModel, x => x.ContinueOperationCommand, x => x.ContinueOperationButton);

        this.BindCommand(ViewModel, x => x.ShowCreateRemoteModalCommand, x => x.NewRemoteActionButton);
        SetupRemotesList(disposables);

        this.BindCommand(ViewModel, x => x.ShowStashModalCommand, x => x.NewStashActionButton);
        SetupStashesList(disposables);

        this.BindCommand(ViewModel, x => x.CheckoutCommitCommand, x => x.ContextCheckoutCommitMenuItem);
        this.BindCommand(ViewModel, x => x.CherryPickCommitCommand, x => x.ContextCherryPickMenuItem);
        this.BindCommand(ViewModel, x => x.RevertCommitCommand, x => x.ContextRevertMenuItem);
        this.BindCommand(ViewModel, x => x.CreateBranchFromCommitCommand, x => x.ContextCreateBranchMenuItem);
        this.BindCommand(ViewModel, x => x.CreateTagFromCommitCommand, x => x.ContextCreateTagMenuItem);

        ContextResetSoftMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Soft).Subscribe();
        ContextResetMixedMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Mixed).Subscribe();
        ContextResetHardMenuItem.Click += (s, e) => ViewModel?.RequestResetBranchCommand.Execute(GitResetType.Hard).Subscribe();

        this.BindCommand(ViewModel, x => x.OpenFileManagerCommand, x => x.OpenFileManagerButton);
        this.BindCommand(ViewModel, x => x.OpenTerminalCommand, x => x.OpenTerminalButton);
        this.BindCommand(ViewModel, x => x.OpenEditorCommand, x => x.OpenEditorButton);

        this.BindCommand(ViewModel, x => x.OpenFileManagerCommand, x => x.OpenInFileManagerMenuItem);
        this.BindCommand(ViewModel, x => x.OpenTerminalCommand, x => x.OpenInTerminalMenuItem);
        this.BindCommand(ViewModel, x => x.OpenEditorCommand, x => x.OpenInEditorMenuItem);
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
                WorkingTreePanelControl.IsVisible = tab == CommitLogSidebarTab.WorkingTree;
                RemotesTabPanel.IsVisible = tab == CommitLogSidebarTab.Remotes;
                StashesTabPanel.IsVisible = tab == CommitLogSidebarTab.Stashes;
            }))
            .DisposeWith(disposables);
    }

    private void HandleBranchMergeMenuItemClick()
    {
        if (BranchesListBox.SelectedItem is BranchListItem item && !item.IsAllBranches && ViewModel != null)
        {
            ViewModel.ShowMergeModalCommand.Execute(item.Name).Subscribe();
        }
    }

    private void HandleInteractiveRebaseMenuItemClick()
    {
        if (CommitsListBox.SelectedItem is CommitSummary commit && ViewModel != null)
        {
            ViewModel.StartInteractiveRebaseCommand.Execute(commit).Subscribe();
        }
    }

    private void HandleCommitsPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(CommitsListBox).Properties.IsRightButtonPressed)
        {
            return;
        }

        Visual? visual = e.Source as Visual;
        ListBoxItem? item = visual?.FindAncestorOfType<ListBoxItem>(true);
        if (item?.DataContext is CommitSummary commit)
        {
            CommitsListBox.SelectedItem = commit;
        }
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.SearchCommitsTextBox.Text)
            .Skip(1)
            .Subscribe(Observer.Create<string?>(query => ViewModel?.UpdateSearchQuery(query ?? string.Empty)))
            .DisposeWith(disposables);
    }

    private void SetupSelectionHandlers(CompositeDisposable disposables)
    {
        CommitsListBox.AddHandler(InputElement.PointerPressedEvent, (sender, e) => HandleCommitsPointerPressed(e), RoutingStrategies.Tunnel);

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

        BranchesListBox.SelectionChanged += async (sender, e) =>
        {
            if (BranchesListBox.SelectedItem is BranchListItem item && ViewModel != null)
            {
                await ViewModel.SelectBranchAsync(item);
            }
        };

        BranchesListBox.DoubleTapped += (sender, e) =>
        {
            if (BranchesListBox.SelectedItem is BranchListItem item && !item.IsAllBranches && ViewModel != null)
            {
                ViewModel.CheckoutBranchCommand.Execute(item.Name).Subscribe();
            }
        };
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
}
