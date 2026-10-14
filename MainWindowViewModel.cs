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

        RepositoryManagerViewModel initialScreen = new(
            this,
            recentRepositoriesRepository,
            gitService,
            profileRepository: profileRepository,
            applicationSettingsRepository: applicationSettingsRepository,
            aiConfigurationRepository: aiConfigurationRepository,
            aiCommitMessageService: aiCommitMessageService);
        Router.Navigate.Execute(initialScreen).Subscribe();
    }
}
