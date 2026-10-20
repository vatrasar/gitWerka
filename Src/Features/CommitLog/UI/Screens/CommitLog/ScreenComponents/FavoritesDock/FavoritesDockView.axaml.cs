namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.FavoritesDock;

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
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Resources;
using ReactiveUI;

/// <summary>
/// Component: FavoritesDockView
/// Purpose: Displays favorite repositories drawer and modal for adding or editing favorites.
/// Usage: Bound to FavoritesDockViewModel in CommitLogView.
/// Key UI elements: FavoritesDockBackdrop, FavoritesDockPanel, FavoriteGroupsItemsControl, UngroupedFavoritesItemsControl, AddFavoriteModalBackdrop.
/// Used In: CommitLogView
/// </summary>
public partial class FavoritesDockView : ReactiveUserControl<FavoritesDockViewModel>
{
    public FavoritesDockView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            SetupBindings(disposables);
            SetupCommands(disposables);
            SetupTextInputs(disposables);
            SetupFavoritesList(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
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
                    ? GitWerka.Shared.Resources.GlobalStrings.SaveButton
                    : CommitLogStrings.ConfirmAddFavoriteButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.RecentNonFavoriteRepository)
            .Subscribe(Observer.Create<RecentRepository?>(recent =>
            {
                bool hasRecent = recent != null;
                RecentNonFavoriteSectionPanel.IsVisible = hasRecent;
                FavoritesDividerRectangle.IsVisible = hasRecent;
                if (recent != null)
                {
                    RecentNonFavoriteNameTextBlock.Text = recent.DisplayName;
                    RecentNonFavoritePathTextBlock.Text = recent.Path;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.FavoriteRepositories.Count)
            .Subscribe(Observer.Create<int>(count =>
            {
                EmptyFavoritesTextBlock.IsVisible = count == 0;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.UngroupedFavoriteRepositories.Count, x => x.ViewModel!.State.FavoriteGroups.Count)
            .Subscribe(Observer.Create<(int UngroupedCount, int GroupsCount)>(t =>
            {
                UngroupedFavoritesSectionHeaderTextBlock.IsVisible = t.GroupsCount > 0 && t.UngroupedCount > 0;
            }))
            .DisposeWith(disposables);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.CloseFavoritesDockCommand, x => x.CloseFavoritesDockButton);
        this.BindCommand(ViewModel, x => x.HideAddFavoriteModalCommand, x => x.CancelAddFavoriteButton);
        this.BindCommand(ViewModel, x => x.ConfirmAddFavoriteCommand, x => x.ConfirmAddFavoriteButton);

        FavoritesBackdropDismissBorder.PointerPressed += (s, e) => ViewModel?.CloseFavoritesDockCommand.Execute().Subscribe();
        OpenFolderFromDockButton.Click += async (s, e) => await HandleOpenFolderFromDockClickAsync();
        OpenRecentNonFavoriteRepoButton.Click += (s, e) => HandleOpenRecentNonFavoriteRepoClick();
        AddRecentToFavoritesButton.Click += (s, e) => HandleAddRecentToFavoritesClick();
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
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
                ViewModel.OpenRepositoryCommand.Execute(repoToOpen.Path).Subscribe();
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
            }
        });
    }

    private void HandleOpenRecentNonFavoriteRepoClick()
    {
        if (ViewModel?.State.RecentNonFavoriteRepository != null)
        {
            ViewModel.OpenRepositoryCommand.Execute(ViewModel.State.RecentNonFavoriteRepository.Path).Subscribe();
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
            ViewModel.OpenRepositoryCommand.Execute(pickedPath).Subscribe();
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return null;
        }

        IStorageFolder? startLocation = null;
        string? effectivePath = ViewModel?.State.DefaultProjectsDirectory;
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
}
