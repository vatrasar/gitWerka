/// <summary>
/// Screen: RepositoryManagerView
/// Purpose: Initial welcome screen for opening, cloning, creating, and managing recent Git repositories.
/// Available Functionalities:
/// - List and filter recently opened repositories.
/// - Open an existing local Git repository via system directory picker.
/// - Clone a remote Git repository via URL and local directory path.
/// - Initialize a new local Git repository in a specified folder.
/// - Display operation loading states and validation error banners.
/// Key UI Elements:
/// - OpenRepositoryTileButton, CloneRepositoryTileButton, InitRepositoryTileButton.
/// - SearchInputTextBox, RecentRepositoriesListBox, EmptyRepositoriesTextBlock.
/// - CloneModalOverlay, InitModalOverlay, OperationRunningOverlay, ErrorBannerBorder.
/// Navigation:
/// - Navigate From: App startup / Shell host screen.
/// - Navigate To: Main repository workspace (future features).
/// </summary>
namespace GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Models;
using ReactiveUI;

public partial class RepositoryManagerView : ReactiveUserControl<RepositoryManagerViewModel>
{
    public RepositoryManagerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            SetupBindings(disposables);
            SetupFolderPickers(disposables);
            SetupSelectionHandler(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.FilteredRepositories, x => x.RecentRepositoriesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsLoading, x => x.LoadingProgressBar.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCloneModalVisible, x => x.CloneModalOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsInitModalVisible, x => x.InitModalOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsOperationRunning, x => x.OperationRunningOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.ErrorMessage, x => x.ErrorBannerTextBlock.Text);

        this.BindCommand(ViewModel, x => x.ShowCloneModalCommand, x => x.CloneRepositoryTileButton);
        this.BindCommand(ViewModel, x => x.ShowInitModalCommand, x => x.InitRepositoryTileButton);
        this.BindCommand(ViewModel, x => x.HideCloneModalCommand, x => x.CancelCloneButton);
        this.BindCommand(ViewModel, x => x.ExecuteCloneCommand, x => x.ConfirmCloneButton);
        this.BindCommand(ViewModel, x => x.HideInitModalCommand, x => x.CancelInitButton);
        this.BindCommand(ViewModel, x => x.ExecuteInitCommand, x => x.ConfirmInitButton);
        this.BindCommand(ViewModel, x => x.DismissErrorCommand, x => x.DismissErrorButton);

        this.WhenAnyValue(x => x.ViewModel!.State.ErrorMessage)
            .Select(error => !string.IsNullOrWhiteSpace(error))
            .Subscribe(Observer.Create<bool>(visible => ErrorBannerBorder.IsVisible = visible))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.FilteredRepositories)
            .Select(list => list == null || list.Count == 0)
            .Subscribe(Observer.Create<bool>(empty => EmptyRepositoriesTextBlock.IsVisible = empty))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.SearchInputTextBox.Text)
            .Skip(1)
            .Subscribe(Observer.Create<string?>(query => ViewModel?.UpdateSearchQuery(query ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CloneSourceUrlTextBox.Text)
            .Subscribe(Observer.Create<string?>(url => ViewModel?.SetCloneSourceUrl(url ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CloneTargetDirectoryTextBox.Text)
            .Subscribe(Observer.Create<string?>(dir => ViewModel?.SetCloneTargetDirectory(dir ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.InitDirectoryPathTextBox.Text)
            .Subscribe(Observer.Create<string?>(dir => ViewModel?.SetInitDirectoryPath(dir ?? string.Empty)))
            .DisposeWith(disposables);
    }

    private void SetupFolderPickers(CompositeDisposable disposables)
    {
        OpenRepositoryTileButton.Click += async (sender, e) =>
        {
            string? pickedPath = await PickFolderAsync();
            if (!string.IsNullOrEmpty(pickedPath) && ViewModel != null)
            {
                await ViewModel.OpenRepositoryPathAsync(pickedPath);
            }
        };

        BrowseCloneDirectoryButton.Click += async (sender, e) =>
        {
            string? pickedPath = await PickFolderAsync();
            if (!string.IsNullOrEmpty(pickedPath))
            {
                CloneTargetDirectoryTextBox.Text = pickedPath;
            }
        };

        BrowseInitDirectoryButton.Click += async (sender, e) =>
        {
            string? pickedPath = await PickFolderAsync();
            if (!string.IsNullOrEmpty(pickedPath))
            {
                InitDirectoryPathTextBox.Text = pickedPath;
            }
        };
    }

    private void SetupSelectionHandler(CompositeDisposable disposables)
    {
        RecentRepositoriesListBox.SelectionChanged += async (sender, e) =>
        {
            if (RecentRepositoriesListBox.SelectedItem is RecentRepository selected && ViewModel != null)
            {
                await ViewModel.OpenRepositoryPathAsync(selected.Path);
                RecentRepositoriesListBox.SelectedItem = null;
            }
        };
    }

    private async Task<string?> PickFolderAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Git Repository Directory",
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return null;
        }

        return folders[0].Path.LocalPath;
    }
}
