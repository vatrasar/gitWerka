namespace GitWerka.Features.Shell.UI.Screens.Host;

using System;
using System.Linq;
using System.Reactive;
using GitWerka.Core.Mvvm;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// ViewModel for the main application shell hosting the RoutedViewHost and global status frame.
/// </summary>
public partial class HostViewModel : ViewModelBase<HostState>, IRoutableViewModel
{
    public string? UrlPathSegment => "host";

    public IScreen HostScreen { get; }

    public RoutingState Router => HostScreen.Router;

    public HostViewModel(IScreen hostScreen) : base(new HostState())
    {
        HostScreen = hostScreen;

        Router.CurrentViewModel
            .Subscribe(Observer.Create<IRoutableViewModel?>(vm =>
            {
                if (vm is CommitLogViewModel commitLogVm)
                {
                    SetActiveRepository(commitLogVm.State.RepositoryName, commitLogVm.State.RepositoryPath);
                    return;
                }

                SetActiveRepository(string.Empty, string.Empty);
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
    public void SetStatusMessage(string message)
    {
        UpdateState(s => s with { StatusMessage = message ?? "Ready" });
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
    }
}
