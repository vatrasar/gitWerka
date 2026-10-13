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
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// ViewModel for the RepositoryManager screen handling repository selection, cloning, and initialization.
/// </summary>
public partial class RepositoryManagerViewModel : ViewModelBase<RepositoryManagerState>, IRoutableViewModel
{
    private readonly IRecentRepositoriesRepository _recentRepositoriesRepository;
    private readonly IGitService _gitService;
    private readonly ISystemLauncherService _systemLauncherService;

    public string? UrlPathSegment => "repository-manager";

    public IScreen HostScreen { get; }

    public RepositoryManagerViewModel(
        IScreen hostScreen,
        IRecentRepositoriesRepository recentRepositoriesRepository,
        IGitService gitService,
        ISystemLauncherService? systemLauncherService = null) : base(new RepositoryManagerState())
    {
        HostScreen = hostScreen;
        _recentRepositoriesRepository = recentRepositoriesRepository;
        _gitService = gitService;
        _systemLauncherService = systemLauncherService ?? new GitWerka.Infrastructure.Services.SystemLauncherService();

        LoadRecentRepositoriesCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Updates the current search query and filters the repository collection.
    /// Usages: RepositoryManagerView code-behind.
    /// </summary>
    /// <param name="query">The user search query string.</param>
    public void UpdateSearchQuery(string query)
    {
        string trimmed = query?.Trim() ?? string.Empty;
        var filtered = FilterRepositories(State.AllRepositories, trimmed);

        UpdateState(s => s with
        {
            SearchQuery = trimmed,
            FilteredRepositories = filtered
        });
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

        HostScreen.Router.Navigate.Execute(new CommitLogViewModel(HostScreen, path, _gitService, _systemLauncherService)).Subscribe();
    }

    [ReactiveCommand]
    private async Task LoadRecentRepositories()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            IReadOnlyList<RecentRepository> repos = await _recentRepositoriesRepository.GetRecentRepositoriesAsync();
            var list = repos.ToImmutableList();
            var filtered = FilterRepositories(list, State.SearchQuery);

            UpdateState(s => s with
            {
                IsLoading = false,
                AllRepositories = list,
                FilteredRepositories = filtered
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

            HostScreen.Router.Navigate.Execute(new CommitLogViewModel(HostScreen, clonedPath, _gitService, _systemLauncherService)).Subscribe();
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

            HostScreen.Router.Navigate.Execute(new CommitLogViewModel(HostScreen, initPath, _gitService, _systemLauncherService)).Subscribe();
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
        var list = repos.ToImmutableList();
        var filtered = FilterRepositories(list, State.SearchQuery);

        UpdateState(s => s with
        {
            AllRepositories = list,
            FilteredRepositories = filtered
        });
    }

    private static ImmutableList<RecentRepository> FilterRepositories(ImmutableList<RecentRepository> repos, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return repos;
        }

        return repos
            .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToImmutableList();
    }
}
