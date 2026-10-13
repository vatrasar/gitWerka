/// <summary>
/// Screen: HostView
/// Purpose: Top-level shell screen hosting the RoutedViewHost, shell header chrome, settings shortcut, and status bar footer.
/// Available Functionalities:
/// - Hosts the active routed screen view via ReactiveUI RoutedViewHost.
/// - Displays current active repository header badge when loaded.
/// - Provides back-navigation button when the navigation stack depth is greater than one.
/// - Provides settings button in header to configure repository options when loaded.
/// - Displays application status messages in the footer bar.
/// Key UI Elements:
/// - BackButton, ActiveRepositoryHeaderPanel, ActiveRepositoryNameTextBlock, SettingsButton.
/// - RoutedViewHostControl, StatusMessageTextBlock.
/// Navigation:
/// - Navigate From: MainWindow initialization.
/// - Navigate To: Child screens (RepositoryManager, etc.) routed inside RoutedViewHostControl.
/// </summary>
namespace GitWerka.Features.Shell.UI.Screens.Host;

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

public partial class HostView : ReactiveUserControl<HostViewModel>
{
    public HostView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, x => x.Router, x => x.RoutedViewHostControl.Router);
            this.OneWayBind(ViewModel, x => x.State.CurrentRepositoryName, x => x.ActiveRepositoryNameTextBlock.Text);
            this.OneWayBind(ViewModel, x => x.State.IsRepositoryLoaded, x => x.ActiveRepositoryHeaderPanel.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.StatusMessage, x => x.StatusMessageTextBlock.Text);

            this.BindCommand(ViewModel, x => x.NavigateBackCommand, x => x.BackButton);
            this.BindCommand(ViewModel, x => x.OpenSettingsCommand, x => x.SettingsButton);
            this.OneWayBind(ViewModel, x => x.State.IsRepositoryLoaded, x => x.SettingsButton.IsVisible);

            this.WhenAnyValue(x => x.ViewModel!.Router.NavigationStack.Count)
                .Select(count => count > 1)
                .Subscribe(Observer.Create<bool>(canBack => BackButton.IsVisible = canBack))
                .DisposeWith(disposables);
        });
    }
}
