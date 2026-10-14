namespace GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.RepositoryManager.Domain.Enums;
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

        LoadRecentRepositoriesCommand.Execute().Subscribe();
        LoadApplicationSettingsCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Updates the current search query and filters the repository collection.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="query">The user search query string.</param>
    public void UpdateSearchQuery(string query)
    {
        string trimmed = query?.Trim() ?? string.Empty;
        var filteredAll = FilterRepositories(State.AllRepositories, trimmed);
        var filteredFav = FilterRepositories(State.FavoriteRepositories, trimmed);
        var displayed = State.SelectedTab == RepositoryManagerTab.Favorites
            ? filteredFav
            : filteredAll;

        UpdateState(s => s with
        {
            SearchQuery = trimmed,
            FilteredRepositories = filteredAll,
            FilteredFavoriteRepositories = filteredFav,
            DisplayedRepositories = displayed
        });
    }

    /// <summary>
    /// Selects the active tab in the repository manager view.
    /// Usages: RepositoryManagerView tab buttons.
    /// </summary>
    /// <param name="tab">The tab to activate.</param>
    public void SelectTab(RepositoryManagerTab tab)
    {
        UpdateState(s =>
        {
            var displayed = tab == RepositoryManagerTab.Favorites
                ? s.FilteredFavoriteRepositories
                : s.FilteredRepositories;

            return s with
            {
                SelectedTab = tab,
                DisplayedRepositories = displayed
            };
        });
    }

    [ReactiveCommand]
    private void SelectFavoritesTab()
    {
        SelectTab(RepositoryManagerTab.Favorites);
    }

    [ReactiveCommand]
    private void SelectRecentTab()
    {
        SelectTab(RepositoryManagerTab.Recent);
    }

    /// <summary>
    /// Sets the destination directory for the clone operation modal.
    /// Usages: RepositoryManagerView code-behind folder picker.
    /// </summary>
    /// <param name="path">Selected target directory path.</param>
    public void SetCloneTargetDirectory(string path)
    {
        UpdateState(s => s with { CloneTargetDirectory = path ?? string.Empty });
    }

    /// <summary>
    /// Sets the source URL for the clone operation modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="url">Source remote repository URL.</param>
    public void SetCloneSourceUrl(string url)
    {
        UpdateState(s => s with { CloneSourceUrl = url ?? string.Empty });
    }

    /// <summary>
    /// Sets the directory path for the initialize operation modal.
    /// Usages: RepositoryManagerView code-behind folder picker.
    /// </summary>
    /// <param name="path">Selected target directory path.</param>
    public void SetInitDirectoryPath(string path)
    {
        UpdateState(s => s with { InitDirectoryPath = path ?? string.Empty });
    }

    /// <summary>
    /// Attempts to open a repository from a file system path.
    /// Usages: RepositoryManagerView code-behind folder picker.
    /// </summary>
    /// <param name="path">Directory path selected by the user.</param>
    /// <returns>Task representing the async open operation.</returns>
    public async Task OpenRepositoryPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!_gitService.IsValidRepository(path))
        {
            UpdateState(s => s with { ErrorMessage = "Selected directory is not a valid Git repository." });
            return;
        }

        await SaveRecentRepositoryAsync(path);
        await ReloadRepositoriesAsync();

        HostScreen.Router.Navigate.Execute(new CommitLogViewModel(
            HostScreen,
            path,
            _gitService,
            _systemLauncherService,
            _profileRepository,
            _recentRepositoriesRepository,
            aiCommitMessageService: _aiCommitMessageService,
            aiConfigurationRepository: _aiConfigurationRepository,
            applicationSettingsRepository: _applicationSettingsRepository)).Subscribe();
    }

    [ReactiveCommand]
    private async Task LoadRecentRepositories()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            IReadOnlyList<RecentRepository> repos = await _recentRepositoriesRepository.GetRecentRepositoriesAsync();
            IReadOnlyList<RecentRepository> favorites = await _recentRepositoriesRepository.GetFavoriteRepositoriesAsync();

            var allList = repos.ToImmutableList();
            var favList = favorites.ToImmutableList();

            var filteredAll = FilterRepositories(allList, State.SearchQuery);
            var filteredFav = FilterRepositories(favList, State.SearchQuery);

            var displayed = State.SelectedTab == RepositoryManagerTab.Favorites
                ? filteredFav
                : filteredAll;

            UpdateState(s => s with
            {
                IsLoading = false,
                AllRepositories = allList,
                FilteredRepositories = filteredAll,
                FavoriteRepositories = favList,
                FilteredFavoriteRepositories = filteredFav,
                DisplayedRepositories = displayed
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void ShowCloneModal()
    {
        UpdateState(s => s with { IsCloneModalVisible = true, ErrorMessage = null });
    }

    [ReactiveCommand]
    private void HideCloneModal()
    {
        UpdateState(s => s with { IsCloneModalVisible = false });
    }

    [ReactiveCommand]
    private void ShowInitModal()
    {
        UpdateState(s => s with { IsInitModalVisible = true, ErrorMessage = null });
    }

    [ReactiveCommand]
    private void HideInitModal()
    {
        UpdateState(s => s with { IsInitModalVisible = false });
    }

    [ReactiveCommand]
    private void DismissError()
    {
        UpdateState(s => s with { ErrorMessage = null });
    }

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

            HostScreen.Router.Navigate.Execute(new CommitLogViewModel(
                HostScreen,
                clonedPath,
                _gitService,
                _systemLauncherService,
                _profileRepository,
                _recentRepositoriesRepository,
                aiCommitMessageService: _aiCommitMessageService,
                aiConfigurationRepository: _aiConfigurationRepository,
                applicationSettingsRepository: _applicationSettingsRepository)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsOperationRunning = false,
                ErrorMessage = ex.Message
            });
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

            HostScreen.Router.Navigate.Execute(new CommitLogViewModel(
                HostScreen,
                initPath,
                _gitService,
                _systemLauncherService,
                _profileRepository,
                _recentRepositoriesRepository,
                aiCommitMessageService: _aiCommitMessageService,
                aiConfigurationRepository: _aiConfigurationRepository,
                applicationSettingsRepository: _applicationSettingsRepository)).Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsOperationRunning = false,
                ErrorMessage = ex.Message
            });
        }
    }

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
        IReadOnlyList<RecentRepository> repos = await _recentRepositoriesRepository.GetRecentRepositoriesAsync();
        IReadOnlyList<RecentRepository> favorites = await _recentRepositoriesRepository.GetFavoriteRepositoriesAsync();

        var allList = repos.ToImmutableList();
        var favList = favorites.ToImmutableList();

        var filteredAll = FilterRepositories(allList, State.SearchQuery);
        var filteredFav = FilterRepositories(favList, State.SearchQuery);

        var displayed = State.SelectedTab == RepositoryManagerTab.Favorites
            ? filteredFav
            : filteredAll;

        UpdateState(s => s with
        {
            AllRepositories = allList,
            FilteredRepositories = filteredAll,
            FavoriteRepositories = favList,
            FilteredFavoriteRepositories = filteredFav,
            DisplayedRepositories = displayed
        });
    }

    private static ImmutableList<RecentRepository> FilterRepositories(ImmutableList<RecentRepository> repos, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return repos;
        }

        return repos
            .Where(r => r.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToImmutableList();
    }

    [ReactiveCommand]
    private async Task LoadApplicationSettings()
    {
        if (_applicationSettingsRepository == null)
        {
            return;
        }

        ApplicationSettings settings = await _applicationSettingsRepository.GetSettingsAsync();
        UpdateState(s => s with
        {
            DefaultProjectsDirectory = settings.DefaultProjectsDirectory ?? string.Empty,
            DefaultProjectsDirectoryInput = settings.DefaultProjectsDirectory ?? string.Empty
        });
    }

    [ReactiveCommand]
    private async Task ShowSettingsModal()
    {
        try
        {
            AiConfiguration aiConfig = _aiConfigurationRepository != null
                ? await _aiConfigurationRepository.GetConfigurationAsync()
                : new AiConfiguration();

            ApplicationSettings appSettings = _applicationSettingsRepository != null
                ? await _applicationSettingsRepository.GetSettingsAsync()
                : new ApplicationSettings { DefaultProjectsDirectory = State.DefaultProjectsDirectory };

            UpdateState(s => s with
            {
                IsSettingsModalVisible = true,
                DefaultProjectsDirectory = appSettings.DefaultProjectsDirectory,
                DefaultProjectsDirectoryInput = appSettings.DefaultProjectsDirectory,
                AiApiKeyInput = aiConfig.ApiKey ?? string.Empty,
                AiModelInput = aiConfig.Model,
                AiCommitStyleInput = aiConfig.CommitStyle,
                AiTokenLimitInput = aiConfig.TokenLimit.ToString()
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void HideSettingsModal()
    {
        UpdateState(s => s with { IsSettingsModalVisible = false });
    }

    [ReactiveCommand]
    private async Task ExecuteSaveSettings()
    {
        UpdateState(s => s with { IsSettingsModalVisible = false });
        try
        {
            string trimmedDir = State.DefaultProjectsDirectoryInput?.Trim() ?? string.Empty;
            if (_applicationSettingsRepository != null)
            {
                await _applicationSettingsRepository.SaveSettingsAsync(new ApplicationSettings
                {
                    DefaultProjectsDirectory = trimmedDir
                });
            }

            if (_aiConfigurationRepository != null)
            {
                int.TryParse(State.AiTokenLimitInput, out int tokenLimit);
                if (tokenLimit <= 0)
                {
                    tokenLimit = 4000;
                }

                AiConfiguration updatedAiConfig = new()
                {
                    ApiKey = string.IsNullOrWhiteSpace(State.AiApiKeyInput) ? null : State.AiApiKeyInput.Trim(),
                    Model = string.IsNullOrWhiteSpace(State.AiModelInput) ? "google/gemini-2.5-flash" : State.AiModelInput.Trim(),
                    CommitStyle = string.IsNullOrWhiteSpace(State.AiCommitStyleInput) ? "Conventional Commits: <type>(<scope>): <subject>" : State.AiCommitStyleInput.Trim(),
                    TokenLimit = tokenLimit
                };

                await _aiConfigurationRepository.SaveConfigurationAsync(updatedAiConfig);
            }

            UpdateState(s => s with
            {
                DefaultProjectsDirectory = trimmedDir
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Updates the default projects directory input field in settings modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="path">Selected directory path.</param>
    public void SetDefaultProjectsDirectoryInput(string path)
    {
        UpdateState(s => s with { DefaultProjectsDirectoryInput = path ?? string.Empty });
    }

    /// <summary>
    /// Updates the AI API key input field in settings modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="apiKey">OpenRouter API key.</param>
    public void SetAiApiKeyInput(string apiKey)
    {
        UpdateState(s => s with { AiApiKeyInput = apiKey ?? string.Empty });
    }

    /// <summary>
    /// Updates the AI model input field in settings modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="model">Model identifier string.</param>
    public void SetAiModelInput(string model)
    {
        UpdateState(s => s with { AiModelInput = model ?? string.Empty });
    }

    /// <summary>
    /// Updates the AI commit style input field in settings modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="style">Commit style guideline.</param>
    public void SetAiCommitStyleInput(string style)
    {
        UpdateState(s => s with { AiCommitStyleInput = style ?? string.Empty });
    }

    /// <summary>
    /// Updates the AI token limit input field in settings modal.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="limit">Token limit integer string.</param>
    public void SetAiTokenLimitInput(string limit)
    {
        UpdateState(s => s with { AiTokenLimitInput = limit ?? string.Empty });
    }
}
