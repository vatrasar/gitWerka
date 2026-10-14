/// <summary>
/// Screen: HostView
/// Purpose: Top-level shell screen hosting the RoutedViewHost, shell header chrome, settings shortcut, profile manager, and status bar footer.
/// Available Functionalities:
/// - Hosts the active routed screen view via ReactiveUI RoutedViewHost.
/// - Displays current active repository header badge when loaded.
/// - Provides back-navigation button when the navigation stack depth is greater than one.
/// - Provides settings button in header to configure options (global settings on welcome screen, or repository options when loaded).
/// - Provides profile selection dropdown and management shortcut in header on the right of settings button.
/// - Displays application status messages in the footer bar.
/// Key UI Elements:
/// - BackButton, ActiveRepositoryHeaderPanel, ActiveRepositoryNameTextBlock, SettingsButton.
/// - ProfileMenuButton, ActiveProfileNameTextBlock, ProfilesFlyoutListBox, NoProfilesFlyoutTextBlock, ManageProfilesButton.
/// - RoutedViewHostControl, StatusMessageTextBlock.
/// Navigation:
/// - Navigate From: MainWindow initialization.
/// - Navigate To: Child screens (RepositoryManager, CommitLog, ProfileManagement) routed inside RoutedViewHostControl.
/// </summary>
namespace GitWerka.Features.Shell.UI.Screens.Host;

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.Shell.Resources;
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
            this.OneWayBind(ViewModel, x => x.State.IsSettingsButtonVisible, x => x.SettingsButton.IsVisible);

            this.OneWayBind(ViewModel, x => x.State.IsRepositoryLoaded, x => x.ProfileMenuButton.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.AvailableProfiles, x => x.ProfilesFlyoutListBox.ItemsSource);
            this.BindCommand(ViewModel, x => x.NavigateToProfilesCommand, x => x.ManageProfilesButton);

            ProfileMenuButton.Click += (s, e) => ViewModel?.RefreshProfilesCommand.Execute().Subscribe();
            if (ProfileMenuButton.Flyout != null)
            {
                ProfileMenuButton.Flyout.Opened += (s, e) => ViewModel?.RefreshProfilesCommand.Execute().Subscribe();
            }

            ManageProfilesButton.Click += (s, e) => ProfileMenuButton.Flyout?.Hide();

            this.WhenAnyValue(x => x.ViewModel!.State.ActiveProfile)
                .Subscribe(Observer.Create<GitProfile?>(profile =>
                {
                    ActiveProfileNameTextBlock.Text = profile != null ? profile.Name : ShellStrings.NoActiveProfile;
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.AvailableProfiles.Count)
                .Select(count => count == 0)
                .Subscribe(Observer.Create<bool>(empty => NoProfilesFlyoutTextBlock.IsVisible = empty))
                .DisposeWith(disposables);

            ProfilesFlyoutListBox.SelectionChanged += (sender, e) =>
            {
                if (ProfilesFlyoutListBox.SelectedItem is GitProfile profile && ViewModel != null)
                {
                    ViewModel.SelectProfileCommand.Execute(profile).Subscribe();
                    ProfileMenuButton.Flyout?.Hide();
                    ProfilesFlyoutListBox.SelectedItem = null;
                }
            };

            this.WhenAnyValue(x => x.ViewModel!.Router.NavigationStack.Count)
                .Select(count => count > 1)
                .Subscribe(Observer.Create<bool>(canBack => BackButton.IsVisible = canBack))
                .DisposeWith(disposables);
        });
    }
}
