namespace GitWerka.Features.Shell.UI.Screens.Host;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;

/// <summary>
/// ViewModel for the main application shell hosting the RoutedViewHost and global status frame.
/// </summary>
public partial class HostViewModel : ViewModelBase<HostState>, IRoutableViewModel
{
    private readonly IProfileRepository? _profileRepository;
    private readonly IAppStatusService? _appStatusService;
    private IDisposable? _commitLogSubscription;

    public string? UrlPathSegment => "host";

    public IScreen HostScreen { get; }

    public RoutingState Router => HostScreen.Router;

    public HostViewModel(
        IScreen hostScreen,
        IProfileRepository? profileRepository = null,
        IAppStatusService? appStatusService = null)
        : base(new HostState { AppVersion = ResolveAppVersion() })
    {
        HostScreen = hostScreen;
        _profileRepository = profileRepository ?? Locator.Current.GetService<IProfileRepository>();
        _appStatusService = appStatusService ?? Locator.Current.GetService<IAppStatusService>();

        Router.CurrentViewModel
            .Subscribe(Observer.Create<IRoutableViewModel?>(OnCurrentViewModelChanged));

        _appStatusService?.Status
            .Subscribe(Observer.Create<AppStatus>(status =>
            {
                UpdateState(s => s with
                {
                    StatusMessage = status.Message,
                    StatusSeverity = status.Severity
                });
            }));
    }

    /// <summary>
    /// Updates the active repository details displayed in the shell header.
    /// Usages: HostView / Navigation handlers.
    /// </summary>
    /// <param name="name">Repository display name.</param>
    /// <param name="path">Repository filesystem path.</param>
    public void SetActiveRepository(string name, string path)
    {
        UpdateState(s => s with
        {
            CurrentRepositoryName = name,
            CurrentRepositoryPath = path,
            IsRepositoryLoaded = !string.IsNullOrWhiteSpace(path)
        });
    }

    /// <summary>
    /// Updates the global status message displayed in the shell bottom bar.
    /// Usages: HostView / Global notifications.
    /// </summary>
    /// <param name="message">Status message text.</param>
    /// <param name="severity">Optional severity level.</param>
    public void SetStatusMessage(string message, AppStatusSeverity severity = AppStatusSeverity.Ready)
    {
        if (_appStatusService != null)
        {
            ForwardStatusToService(message, severity);
            return;
        }

        UpdateState(s => s with
        {
            StatusMessage = message ?? "Ready",
            StatusSeverity = severity
        });
    }

    private void ForwardStatusToService(string message, AppStatusSeverity severity)
    {
        switch (severity)
        {
            case AppStatusSeverity.InProgress:
                _appStatusService?.SetInProgress(message);
                break;
            case AppStatusSeverity.Success:
                _appStatusService?.SetSuccess(message);
                break;
            case AppStatusSeverity.Error:
                _appStatusService?.SetError(message);
                break;
            default:
                _appStatusService?.SetReady(message);
                break;
        }
    }

    /// <summary>
    /// Selects an active Git profile for the current repository and synchronizes with the active workspace.
    /// Usages: HostView profile dropdown list selection.
    /// </summary>
    /// <param name="profile">The profile to activate.</param>
    /// <returns>Task tracking async profile selection.</returns>
    public async Task SelectProfileAsync(GitProfile? profile)
    {
        UpdateState(s => s with { ActiveProfile = profile });

        if (Router.NavigationStack.LastOrDefault() is CommitLogViewModel commitLogVm)
        {
            await commitLogVm.SelectProfileAsync(profile);
            return;
        }

        if (_profileRepository != null && !string.IsNullOrWhiteSpace(State.CurrentRepositoryPath))
        {
            if (profile != null)
            {
                await _profileRepository.SetActiveProfileForRepositoryAsync(State.CurrentRepositoryPath, profile.Id);
            }
            else
            {
                await _profileRepository.ClearActiveProfileForRepositoryAsync(State.CurrentRepositoryPath);
            }
        }
    }

    /// <summary>
    /// Navigates to the ProfileManagement screen.
    /// Usages: HostView profile flyout manage button.
    /// </summary>
    public void NavigateToProfileManagement()
    {
        IProfileRepository? repo = _profileRepository;
        if (repo == null && Router.NavigationStack.LastOrDefault() is CommitLogViewModel commitLogVm)
        {
            commitLogVm.NavigateToProfileManagement();
            return;
        }

        if (repo != null)
        {
            HostScreen.Router.Navigate.Execute(
                new ProfileManagementViewModel(HostScreen, repo, State.CurrentRepositoryPath)
            ).Subscribe();
        }
    }

    [ReactiveCommand]
    private void NavigateBack()
    {
        if (Router.NavigationStack.Count > 1)
        {
            Router.NavigateBack.Execute().Subscribe();
        }
    }

    [ReactiveCommand]
    private void OpenSettings()
    {
        if (Router.NavigationStack.LastOrDefault() is CommitLogViewModel commitLogVm)
        {
            commitLogVm.ShowSettingsModalCommand.Execute().Subscribe();
        }
        else if (Router.NavigationStack.LastOrDefault() is RepositoryManagerViewModel repoManagerVm)
        {
            repoManagerVm.ShowSettingsModalCommand.Execute().Subscribe();
        }
    }

    /// <summary>
    /// Refreshes available profiles and active profile from repository.
    /// Usages: HostView profile menu opening, navigation sync.
    /// </summary>
    /// <returns>Task tracking async refresh.</returns>
    public async Task RefreshProfilesAsync()
    {
        if (Router.NavigationStack.LastOrDefault() is CommitLogViewModel commitLogVm)
        {
            await commitLogVm.ReloadProfilesAsync();
            return;
        }

        if (_profileRepository != null)
        {
            try
            {
                var profiles = await _profileRepository.GetAllProfilesAsync();
                GitProfile? active = null;
                if (!string.IsNullOrWhiteSpace(State.CurrentRepositoryPath))
                {
                    active = await _profileRepository.GetActiveProfileForRepositoryAsync(State.CurrentRepositoryPath);
                }

                UpdateState(s => s with
                {
                    AvailableProfiles = profiles.ToImmutableList(),
                    ActiveProfile = active
                });
            }
            catch (Exception ex)
            {
                UpdateState(s => s with { StatusMessage = ex.Message });
            }
        }
    }

    [ReactiveCommand]
    private async Task RefreshProfiles()
    {
        await RefreshProfilesAsync();
    }

    [ReactiveCommand]
    private async Task SelectProfile(GitProfile? profile)
    {
        await SelectProfileAsync(profile);
    }

    [ReactiveCommand]
    private void NavigateToProfiles()
    {
        NavigateToProfileManagement();
    }

    private void OnCurrentViewModelChanged(IRoutableViewModel? vm)
    {
        if (vm is CommitLogViewModel commitLogVm)
        {
            SetActiveRepository(commitLogVm.State.RepositoryName, commitLogVm.State.RepositoryPath);
            SubscribeToCommitLog(commitLogVm);
            _ = commitLogVm.ReloadProfilesAsync();
            UpdateState(s => s with { IsSettingsButtonVisible = true });
            return;
        }

        CommitLogViewModel? activeCommitLogVm = Router.NavigationStack.OfType<CommitLogViewModel>().LastOrDefault();
        if (activeCommitLogVm != null)
        {
            SetActiveRepository(activeCommitLogVm.State.RepositoryName, activeCommitLogVm.State.RepositoryPath);
            UpdateState(s => s with { IsSettingsButtonVisible = false });
            return;
        }

        _commitLogSubscription?.Dispose();
        _commitLogSubscription = null;
        SetActiveRepository(string.Empty, string.Empty);
        bool isSettingsVisible = vm is RepositoryManagerViewModel;
        UpdateState(s => s with
        {
            ActiveProfile = null,
            AvailableProfiles = ImmutableList<GitProfile>.Empty,
            IsSettingsButtonVisible = isSettingsVisible
        });
    }

    private void SubscribeToCommitLog(CommitLogViewModel commitLogVm)
    {
        _commitLogSubscription?.Dispose();
        _commitLogSubscription = Observable.CombineLatest(
            commitLogVm.WhenAnyValue(x => x.State.ActiveProfile),
            commitLogVm.WhenAnyValue(x => x.State.AvailableProfiles),
            commitLogVm.WhenAnyValue(x => x.State.RepositoryName),
            commitLogVm.WhenAnyValue(x => x.State.RepositoryPath),
            (active, profiles, repoName, repoPath) => (active, profiles, repoName, repoPath))
            .Subscribe(Observer.Create<(GitProfile? Active, ImmutableList<GitProfile> Profiles, string RepoName, string RepoPath)>(tuple =>
            {
                UpdateState(s => s with
                {
                    ActiveProfile = tuple.Active,
                    AvailableProfiles = tuple.Profiles,
                    CurrentRepositoryName = tuple.RepoName,
                    CurrentRepositoryPath = tuple.RepoPath,
                    IsRepositoryLoaded = !string.IsNullOrWhiteSpace(tuple.RepoPath)
                });
            }));
    }

    private static string ResolveAppVersion()
    {
        Version? version = typeof(HostViewModel).Assembly.GetName().Version;
        if (version == null)
        {
            return "v1.0.0";
        }

        return version.Build >= 0
            ? $"v{version.Major}.{version.Minor}.{version.Build}"
            : $"v{version.Major}.{version.Minor}";
    }
}
