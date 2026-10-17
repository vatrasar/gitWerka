namespace GitWerka;

using System;
using System.Reactive.Linq;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using GitWerka.Features.RepositoryManager.UI.Screens.RepositoryManager;
using GitWerka.Features.Shell.UI.Screens.Host;
using ReactiveUI;

/// <summary>
/// Main window ViewModel owning the application routing state and top-level screen host.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IScreen
{
    private readonly RepositoryManagerViewModel _initialScreen;

    public RoutingState Router { get; } = new RoutingState();

    public HostViewModel HostViewModel { get; }

    public MainWindowViewModel(
        IRecentRepositoriesRepository recentRepositoriesRepository,
        IGitService gitService,
        IProfileRepository? profileRepository = null,
        IApplicationSettingsRepository? applicationSettingsRepository = null,
        IAiConfigurationRepository? aiConfigurationRepository = null,
        IAiCommitMessageService? aiCommitMessageService = null)
    {
        HostViewModel = new HostViewModel(this, profileRepository);

        _initialScreen = new(
            this,
            recentRepositoriesRepository,
            gitService,
            profileRepository: profileRepository,
            applicationSettingsRepository: applicationSettingsRepository,
            aiConfigurationRepository: aiConfigurationRepository,
            aiCommitMessageService: aiCommitMessageService);
        Router.Navigate.Execute(_initialScreen).Subscribe();
    }

    /// <summary>
    /// Opens the specified file system path as the active repository.
    /// Usages: App.OnFrameworkInitializationCompleted.
    /// </summary>
    /// <param name="path">The directory path of the repository.</param>
    public void OpenInitialRepository(string path)
    {
        _ = _initialScreen.OpenRepositoryPathAsync(path);
    }
}
