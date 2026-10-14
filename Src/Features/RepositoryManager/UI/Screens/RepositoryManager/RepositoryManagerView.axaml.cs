/// <summary>
/// Screen: RepositoryManagerView
/// Purpose: Initial welcome screen for opening, cloning, creating, and managing recent Git repositories.
/// Available Functionalities:
/// - List and filter recently opened repositories.
/// - Open an existing local Git repository via system directory picker.
/// - Clone a remote Git repository via URL and local directory path.
/// - Initialize a new local Git repository in a specified folder.
/// - Configure global application settings (default projects directory and AI assistant) via settings modal overlay.
/// - Display operation loading states and validation error banners.
/// Key UI Elements:
/// - OpenRepositoryTileButton, CloneRepositoryTileButton, InitRepositoryTileButton, SettingsTileButton.
/// - FavoritesTabButton, RecentTabButton, SearchInputTextBox, RecentRepositoriesListBox, EmptyRepositoriesTextBlock.
/// - CloneModalOverlay, InitModalOverlay, OperationRunningOverlay, ErrorBannerBorder.
/// - SettingsModalOverlay, DefaultProjectsDirectoryTextBox, BrowseDefaultProjectsDirectoryButton, CancelSettingsButton, ConfirmSettingsButton.
/// - AiApiKeyTextBox, AiModelTextBox, AiTokenLimitTextBox, AiCommitStyleTextBox.
/// Navigation:
/// - Navigate From: App startup / Shell host screen.
/// - Navigate To: CommitLogView workspace.
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
using GitWerka.Features.RepositoryManager.Domain.Enums;
using GitWerka.Features.RepositoryManager.Resources;
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
        this.OneWayBind(ViewModel, x => x.State.DisplayedRepositories, x => x.RecentRepositoriesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsLoading, x => x.LoadingProgressBar.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCloneModalVisible, x => x.CloneModalOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsInitModalVisible, x => x.InitModalOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsSettingsModalVisible, x => x.SettingsModalOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsOperationRunning, x => x.OperationRunningOverlay.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.ErrorMessage, x => x.ErrorBannerTextBlock.Text);

        this.OneWayBind(ViewModel, x => x.State.DefaultProjectsDirectoryInput, x => x.DefaultProjectsDirectoryTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiApiKeyInput, x => x.AiApiKeyTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiModelInput, x => x.AiModelTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiTokenLimitInput, x => x.AiTokenLimitTextBox.Text);
        this.OneWayBind(ViewModel, x => x.State.AiCommitStyleInput, x => x.AiCommitStyleTextBox.Text);

        this.BindCommand(ViewModel, x => x.SelectFavoritesTabCommand, x => x.FavoritesTabButton);
        this.BindCommand(ViewModel, x => x.SelectRecentTabCommand, x => x.RecentTabButton);
        this.BindCommand(ViewModel, x => x.ShowCloneModalCommand, x => x.CloneRepositoryTileButton);
        this.BindCommand(ViewModel, x => x.ShowInitModalCommand, x => x.InitRepositoryTileButton);
        this.BindCommand(ViewModel, x => x.ShowSettingsModalCommand, x => x.SettingsTileButton);
        this.BindCommand(ViewModel, x => x.HideCloneModalCommand, x => x.CancelCloneButton);
        this.BindCommand(ViewModel, x => x.ExecuteCloneCommand, x => x.ConfirmCloneButton);
        this.BindCommand(ViewModel, x => x.HideInitModalCommand, x => x.CancelInitButton);
        this.BindCommand(ViewModel, x => x.ExecuteInitCommand, x => x.ConfirmInitButton);
        this.BindCommand(ViewModel, x => x.HideSettingsModalCommand, x => x.CancelSettingsButton);
        this.BindCommand(ViewModel, x => x.ExecuteSaveSettingsCommand, x => x.ConfirmSettingsButton);
        this.BindCommand(ViewModel, x => x.DismissErrorCommand, x => x.DismissErrorButton);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedTab)
            .Subscribe(Observer.Create<RepositoryManagerTab>(tab =>
            {
                FavoritesTabButton.Classes.Set("active", tab == RepositoryManagerTab.Favorites);
                RecentTabButton.Classes.Set("active", tab == RepositoryManagerTab.Recent);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ErrorMessage)
            .Select(error => !string.IsNullOrWhiteSpace(error))
            .Subscribe(Observer.Create<bool>(visible => ErrorBannerBorder.IsVisible = visible))
            .DisposeWith(disposables);

        this.WhenAnyValue(
            x => x.ViewModel!.State.DisplayedRepositories,
            x => x.ViewModel!.State.SelectedTab,
            (repos, tab) => (Count: repos?.Count ?? 0, Tab: tab))
            .Subscribe(Observer.Create<(int Count, RepositoryManagerTab Tab)>(tuple =>
            {
                EmptyRepositoriesTextBlock.IsVisible = tuple.Count == 0;
                EmptyRepositoriesTextBlock.Text = tuple.Tab == RepositoryManagerTab.Favorites
                    ? RepositoryManagerStrings.NoFavoriteRepositories
                    : RepositoryManagerStrings.NoRecentRepositories;
            }))
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

        this.WhenAnyValue(x => x.DefaultProjectsDirectoryTextBox.Text)
            .Subscribe(Observer.Create<string?>(dir => ViewModel?.SetDefaultProjectsDirectoryInput(dir ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiApiKeyTextBox.Text)
            .Subscribe(Observer.Create<string?>(key => ViewModel?.SetAiApiKeyInput(key ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiModelTextBox.Text)
            .Subscribe(Observer.Create<string?>(model => ViewModel?.SetAiModelInput(model ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiTokenLimitTextBox.Text)
            .Subscribe(Observer.Create<string?>(limit => ViewModel?.SetAiTokenLimitInput(limit ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiCommitStyleTextBox.Text)
            .Subscribe(Observer.Create<string?>(style => ViewModel?.SetAiCommitStyleInput(style ?? string.Empty)))
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
            string? pickedPath = await PickFolderAsync(CloneTargetDirectoryTextBox.Text);
            if (!string.IsNullOrEmpty(pickedPath))
            {
                CloneTargetDirectoryTextBox.Text = pickedPath;
            }
        };

        BrowseInitDirectoryButton.Click += async (sender, e) =>
        {
            string? pickedPath = await PickFolderAsync(InitDirectoryPathTextBox.Text);
            if (!string.IsNullOrEmpty(pickedPath))
            {
                InitDirectoryPathTextBox.Text = pickedPath;
            }
        };

        BrowseDefaultProjectsDirectoryButton.Click += async (sender, e) =>
        {
            string? pickedPath = await PickFolderAsync(ViewModel?.State.DefaultProjectsDirectoryInput);
            if (!string.IsNullOrEmpty(pickedPath) && ViewModel != null)
            {
                ViewModel.SetDefaultProjectsDirectoryInput(pickedPath);
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
}
