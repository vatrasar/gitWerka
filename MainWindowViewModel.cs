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
        IGitService gitService)
    {
        HostViewModel = new HostViewModel(this);

        RepositoryManagerViewModel initialScreen = new(this, recentRepositoriesRepository, gitService);
        Router.Navigate.Execute(initialScreen).Subscribe();
    }
}
