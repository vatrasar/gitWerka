namespace GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.RepositoryManager.Domain.Enums;
using GitWerka.Features.RepositoryManager.Domain.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;

/// <summary>
/// ViewModel for the RepositoryManager screen handling repository selection, cloning, and initialization.
/// </summary>
public partial class RepositoryManagerViewModel : ViewModelBase<RepositoryManagerState>, IRoutableViewModel
{
    private readonly IRecentRepositoriesRepository _recentRepositoriesRepository;
    private readonly IGitService _gitService;
    private readonly ISystemLauncherService _systemLauncherService;
    private readonly IProfileRepository? _profileRepository;
    private readonly IApplicationSettingsRepository? _applicationSettingsRepository;
    private readonly IAiConfigurationRepository? _aiConfigurationRepository;
    private readonly IAiCommitMessageService? _aiCommitMessageService;
    private readonly RepositorySettingsCoordinator _settingsCoordinator;

    public string? UrlPathSegment => "repository-manager";

    public IScreen HostScreen { get; }

    public RepositoryManagerViewModel(
        IScreen hostScreen,
        IRecentRepositoriesRepository recentRepositoriesRepository,
        IGitService gitService,
        ISystemLauncherService? systemLauncherService = null,
        IProfileRepository? profileRepository = null,
        IApplicationSettingsRepository? applicationSettingsRepository = null,
        IAiConfigurationRepository? aiConfigurationRepository = null,
        IAiCommitMessageService? aiCommitMessageService = null) : base(new RepositoryManagerState())
    {
        HostScreen = hostScreen;
        _recentRepositoriesRepository = recentRepositoriesRepository;
        _gitService = gitService;
        _systemLauncherService = systemLauncherService ?? new GitWerka.Infrastructure.Services.SystemLauncherService();
        _profileRepository = profileRepository ?? Locator.Current.GetService<IProfileRepository>();
        _applicationSettingsRepository = applicationSettingsRepository ?? Locator.Current.GetService<IApplicationSettingsRepository>();
        _aiConfigurationRepository = aiConfigurationRepository ?? Locator.Current.GetService<IAiConfigurationRepository>();
        _aiCommitMessageService = aiCommitMessageService ?? Locator.Current.GetService<IAiCommitMessageService>();
        _settingsCoordinator = new RepositorySettingsCoordinator(_applicationSettingsRepository, _aiConfigurationRepository);

        LoadRecentRepositoriesCommand.Execute().Subscribe();
        LoadApplicationSettingsCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Updates the current search query and filters the repository collection.
    /// </summary>
    public void UpdateSearchQuery(string query)
    {
        string trimmed = query?.Trim() ?? string.Empty;
        var result = RepositoryGroupingHelper.BuildCollections(
            State.AllRepositories,
            State.FavoriteRepositories,
            trimmed,
            State.ExpandedFavoriteGroupNames,
            State.SelectedTab);

        UpdateState(s => s with
        {
            SearchQuery = trimmed,
            FilteredRepositories = result.FilteredAll,
            FilteredFavoriteRepositories = result.FilteredFavorites,
            FavoriteGroups = result.Groups,
            UngroupedFavoriteRepositories = result.Ungrouped,
            DisplayedRepositories = result.Displayed
        });
    }

    /// <summary>
    /// Selects the active tab in the repository manager view.
    /// </summary>
    public void SelectTab(RepositoryManagerTab tab)
    {
        UpdateState(s => s with
        {
            SelectedTab = tab,
            DisplayedRepositories = tab == RepositoryManagerTab.Favorites ? s.FilteredFavoriteRepositories : s.FilteredRepositories
        });
    }

    [ReactiveCommand]
    private void SelectFavoritesTab() => SelectTab(RepositoryManagerTab.Favorites);

    [ReactiveCommand]
    private void SelectRecentTab() => SelectTab(RepositoryManagerTab.Recent);

    /// <summary>
    /// Sets the destination directory for the clone operation modal.
    /// </summary>
    public void SetCloneTargetDirectory(string path) =>
        UpdateState(s => s with { CloneTargetDirectory = path ?? string.Empty });

    /// <summary>
    /// Sets the source URL for the clone operation modal.
    /// </summary>
    public void SetCloneSourceUrl(string url) =>
        UpdateState(s => s with { CloneSourceUrl = url ?? string.Empty });

    /// <summary>
    /// Sets the directory path for the initialize operation modal.
    /// </summary>
    public void SetInitDirectoryPath(string path) =>
        UpdateState(s => s with { InitDirectoryPath = path ?? string.Empty });

    /// <summary>
    /// Attempts to open a repository from a file system path.
    /// </summary>
    public async Task OpenRepositoryPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        string resolvedPath = _gitService.FindRepositoryRoot(normalizedPath) ?? normalizedPath;
        resolvedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(resolvedPath));
        if (!_gitService.IsValidRepository(resolvedPath))
        {
            UpdateState(s => s with
            {
                IsNotGitRepositoryModalVisible = true,
                NotGitRepositoryPath = resolvedPath,
                ErrorMessage = null
            });
            return;
        }

        await SaveRecentRepositoryAsync(resolvedPath);
        await ReloadRepositoriesAsync();

        HostScreen.Router.Navigate.Execute(CreateCommitLogViewModel(resolvedPath)).Subscribe();
    }

    private CommitLogViewModel CreateCommitLogViewModel(string repositoryPath) =>
        new(
            HostScreen,
            repositoryPath,
            _gitService,
            _systemLauncherService,
            _profileRepository,
            _recentRepositoriesRepository,
            aiCommitMessageService: _aiCommitMessageService,
            aiConfigurationRepository: _aiConfigurationRepository,
            applicationSettingsRepository: _applicationSettingsRepository);

    [ReactiveCommand]
    private void HideNotGitRepositoryModal() =>
        UpdateState(s => s with
        {
            IsNotGitRepositoryModalVisible = false,
            NotGitRepositoryPath = string.Empty
        });

    [ReactiveCommand]
    private async Task ConfirmInitializeNotGitRepository()
    {
        string targetPath = State.NotGitRepositoryPath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            UpdateState(s => s with { IsNotGitRepositoryModalVisible = false });
            return;
        }

        UpdateState(s => s with
        {
            IsNotGitRepositoryModalVisible = false,
            IsOperationRunning = true,
            ErrorMessage = null
        });

        try
        {
            string initPath = await _gitService.InitializeRepositoryAsync(targetPath);
            await SaveRecentRepositoryAsync(initPath);
            await ReloadRepositoriesAsync();

            UpdateState(s => s with
            {
                IsOperationRunning = false,
                NotGitRepositoryPath = string.Empty
            });

            HostScreen.Router.Navigate.Execute(CreateCommitLogViewModel(initPath)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsOperationRunning = false,
                ErrorMessage = ex.Message,
                NotGitRepositoryPath = string.Empty
            });
        }
    }

    [ReactiveCommand]
    private async Task LoadRecentRepositories()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });
        try
        {
            await ReloadRepositoriesAsync();
            UpdateState(s => s with { IsLoading = false });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsLoading = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void ShowCloneModal() =>
        UpdateState(s => s with { IsCloneModalVisible = true, ErrorMessage = null });

    [ReactiveCommand]
    private void HideCloneModal() =>
        UpdateState(s => s with { IsCloneModalVisible = false });

    [ReactiveCommand]
    private void ShowInitModal() =>
        UpdateState(s => s with { IsInitModalVisible = true, ErrorMessage = null });

    [ReactiveCommand]
    private void HideInitModal() =>
        UpdateState(s => s with { IsInitModalVisible = false });

    [ReactiveCommand]
    private void DismissError() =>
        UpdateState(s => s with { ErrorMessage = null });

    [ReactiveCommand]
    private async Task ExecuteClone()
    {
        if (string.IsNullOrWhiteSpace(State.CloneSourceUrl) || string.IsNullOrWhiteSpace(State.CloneTargetDirectory))
        {
            UpdateState(s => s with { ErrorMessage = "Source URL and destination path are required." });
            return;
        }

        UpdateState(s => s with { IsOperationRunning = true, ErrorMessage = null });
        try
        {
            string clonedPath = await _gitService.CloneRepositoryAsync(State.CloneSourceUrl, State.CloneTargetDirectory);
            await SaveRecentRepositoryAsync(clonedPath);
            await ReloadRepositoriesAsync();

            UpdateState(s => s with
            {
                IsOperationRunning = false,
                IsCloneModalVisible = false,
                CloneSourceUrl = string.Empty,
                CloneTargetDirectory = string.Empty
            });

            HostScreen.Router.Navigate.Execute(CreateCommitLogViewModel(clonedPath)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsOperationRunning = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private async Task ExecuteInit()
    {
        if (string.IsNullOrWhiteSpace(State.InitDirectoryPath))
        {
            UpdateState(s => s with { ErrorMessage = "Directory path is required." });
            return;
        }

        UpdateState(s => s with { IsOperationRunning = true, ErrorMessage = null });
        try
        {
            string initPath = await _gitService.InitializeRepositoryAsync(State.InitDirectoryPath);
            await SaveRecentRepositoryAsync(initPath);
            await ReloadRepositoriesAsync();

            UpdateState(s => s with
            {
                IsOperationRunning = false,
                IsInitModalVisible = false,
                InitDirectoryPath = string.Empty
            });

            HostScreen.Router.Navigate.Execute(CreateCommitLogViewModel(initPath)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsOperationRunning = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Updates the custom name input in the Edit Favorite modal.
    /// </summary>
    public void SetEditFavoriteNameInput(string name) =>
        UpdateState(s => s with { EditFavoriteNameInput = name ?? string.Empty });

    /// <summary>
    /// Updates the group name input in the Edit Favorite modal.
    /// </summary>
    public void SetEditFavoriteGroupInput(string group) =>
        UpdateState(s => s with { EditFavoriteGroupInput = group ?? string.Empty });

    [ReactiveCommand]
    private async Task TogglePin(string path)
    {
        await _recentRepositoriesRepository.TogglePinRecentRepositoryAsync(path);
        await ReloadRepositoriesAsync();
    }

    [ReactiveCommand]
    private async Task RemoveRecent(string path)
    {
        await _recentRepositoriesRepository.RemoveRecentRepositoryAsync(path);
        await ReloadRepositoriesAsync();
    }

    private async Task SaveRecentRepositoryAsync(string path)
    {
        string name = new DirectoryInfo(path).Name;
        RecentRepository model = new()
        {
            Path = path,
            Name = name,
            LastOpenedAtUtc = DateTime.UtcNow,
            IsPinned = false
        };
        await _recentRepositoriesRepository.AddOrUpdateRecentRepositoryAsync(model);
    }

    private async Task ReloadRepositoriesAsync()
    {
        var repos = await _recentRepositoriesRepository.GetRecentRepositoriesAsync();
        var favorites = await _recentRepositoriesRepository.GetFavoriteRepositoriesAsync();

        var result = RepositoryGroupingHelper.BuildCollections(
            repos,
            favorites,
            State.SearchQuery,
            State.ExpandedFavoriteGroupNames,
            State.SelectedTab);

        UpdateState(s => s with
        {
            AllRepositories = result.All,
            FilteredRepositories = result.FilteredAll,
            FavoriteRepositories = result.Favorites,
            FilteredFavoriteRepositories = result.FilteredFavorites,
            FavoriteGroups = result.Groups,
            UngroupedFavoriteRepositories = result.Ungrouped,
            ExistingGroupNames = result.GroupNames,
            DisplayedRepositories = result.Displayed
        });
    }

    [ReactiveCommand]
    private void ToggleFavoriteGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        var expanded = State.ExpandedFavoriteGroupNames.Contains(groupName)
            ? State.ExpandedFavoriteGroupNames.Remove(groupName)
            : State.ExpandedFavoriteGroupNames.Add(groupName);

        var (groups, ungrouped) = RepositoryGroupingHelper.BuildFavoriteGroupsAndUngrouped(State.FavoriteRepositories, expanded, State.SearchQuery);
        UpdateState(s => s with
        {
            ExpandedFavoriteGroupNames = expanded,
            FavoriteGroups = groups,
            UngroupedFavoriteRepositories = ungrouped
        });
    }

    [ReactiveCommand]
    private void ShowEditFavoriteModal(string path)
    {
        RecentRepository? repo = State.FavoriteRepositories.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase))
            ?? State.AllRepositories.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));

        UpdateState(s => s with
        {
            IsEditFavoriteModalVisible = true,
            TargetFavoritePath = path,
            EditFavoriteNameInput = repo?.CustomName ?? repo?.Name ?? string.Empty,
            EditFavoriteGroupInput = repo?.GroupName ?? string.Empty
        });
    }

    [ReactiveCommand]
    private void HideEditFavoriteModal() =>
        UpdateState(s => s with { IsEditFavoriteModalVisible = false });

    [ReactiveCommand]
    private async Task ConfirmEditFavorite()
    {
        if (string.IsNullOrWhiteSpace(State.TargetFavoritePath))
        {
            UpdateState(s => s with { IsEditFavoriteModalVisible = false });
            return;
        }

        string rawName = State.EditFavoriteNameInput?.Trim() ?? string.Empty;
        string nameToUse = string.IsNullOrWhiteSpace(rawName)
            ? new DirectoryInfo(State.TargetFavoritePath).Name
            : rawName;
        string? groupToUse = string.IsNullOrWhiteSpace(State.EditFavoriteGroupInput)
            ? null
            : State.EditFavoriteGroupInput.Trim();

        await _recentRepositoriesRepository.SetFavoriteAsync(State.TargetFavoritePath, true, nameToUse, groupToUse);
        await ReloadRepositoriesAsync();
        UpdateState(s => s with { IsEditFavoriteModalVisible = false });
    }

    [ReactiveCommand]
    private async Task RemoveFromFavorites(string path)
    {
        await _recentRepositoriesRepository.SetFavoriteAsync(path, false, null, null);
        await ReloadRepositoriesAsync();
    }

    [ReactiveCommand]
    private async Task LoadApplicationSettings()
    {
        string dir = await _settingsCoordinator.LoadDefaultProjectsDirectoryAsync();
        UpdateState(s => s with
        {
            DefaultProjectsDirectory = dir,
            DefaultProjectsDirectoryInput = dir
        });
    }

    [ReactiveCommand]
    private async Task ShowSettingsModal()
    {
        try
        {
            var (defaultDir, apiKey, model, style, tokenLimit) =
                await _settingsCoordinator.LoadModalSettingsAsync(State.DefaultProjectsDirectory);

            UpdateState(s => s with
            {
                IsSettingsModalVisible = true,
                DefaultProjectsDirectory = defaultDir,
                DefaultProjectsDirectoryInput = defaultDir,
                AiApiKeyInput = apiKey,
                AiModelInput = model,
                AiCommitStyleInput = style,
                AiTokenLimitInput = tokenLimit
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void HideSettingsModal() =>
        UpdateState(s => s with { IsSettingsModalVisible = false });

    [ReactiveCommand]
    private async Task ExecuteSaveSettings()
    {
        UpdateState(s => s with { IsSettingsModalVisible = false });
        try
        {
            string savedDir = await _settingsCoordinator.SaveModalSettingsAsync(
                State.DefaultProjectsDirectoryInput,
                State.AiApiKeyInput,
                State.AiModelInput,
                State.AiCommitStyleInput,
                State.AiTokenLimitInput);

            UpdateState(s => s with { DefaultProjectsDirectory = savedDir });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Updates the default projects directory input field in settings modal.
    /// </summary>
    public void SetDefaultProjectsDirectoryInput(string path) =>
        UpdateState(s => s with { DefaultProjectsDirectoryInput = path ?? string.Empty });

    /// <summary>
    /// Updates the AI API key input field in settings modal.
    /// </summary>
    public void SetAiApiKeyInput(string apiKey) =>
        UpdateState(s => s with { AiApiKeyInput = apiKey ?? string.Empty });

    /// <summary>
    /// Updates the AI model input field in settings modal.
    /// </summary>
    public void SetAiModelInput(string model) =>
        UpdateState(s => s with { AiModelInput = model ?? string.Empty });

    /// <summary>
    /// Updates the AI commit style input field in settings modal.
    /// </summary>
    public void SetAiCommitStyleInput(string style) =>
        UpdateState(s => s with { AiCommitStyleInput = style ?? string.Empty });

    /// <summary>
    /// Updates the AI token limit input field in settings modal.
    /// </summary>
    public void SetAiTokenLimitInput(string limit) =>
        UpdateState(s => s with { AiTokenLimitInput = limit ?? string.Empty });
}
