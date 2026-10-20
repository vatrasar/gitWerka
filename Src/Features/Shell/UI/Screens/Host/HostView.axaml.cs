/// <summary>
/// Screen: HostView
/// Purpose: Top-level shell screen hosting the RoutedViewHost, shell header chrome, settings shortcut, profile manager, and status bar footer.
/// Available Functionalities:
/// - Hosts the active routed screen view via ReactiveUI RoutedViewHost.
/// - Displays current active repository header badge when loaded.
/// - Provides back-navigation button returning directly to the welcome screen when repository or child screens are active.
/// - Provides settings button in header to configure options (global settings on welcome screen, or repository options when loaded).
/// - Provides profile selection dropdown and management shortcut in header on the right of settings button.
/// - Displays application status messages in the footer bar.
/// - Displays active repository path or application version info in the footer bar.
/// Key UI Elements:
/// - BackButton, ActiveRepositoryHeaderPanel, ActiveRepositoryNameTextBlock, SettingsButton.
/// - ProfileMenuButton, ActiveProfileNameTextBlock, ProfilesFlyoutListBox, NoProfilesFlyoutTextBlock, ManageProfilesButton.
/// - RoutedViewHostControl, StatusLoadingIcon, StatusSuccessIcon, StatusErrorIcon, StatusReadyIcon, StatusMessageTextBlock, FooterInfoTextBlock.
/// Navigation:
/// - Navigate From: MainWindow initialization.
/// - Navigate To: Child screens (RepositoryManager, CommitLog, ProfileManagement) routed inside RoutedViewHostControl.
/// </summary>
namespace GitWerka.Features.Shell.UI.Screens.Host;

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
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
            this.OneWayBind(ViewModel, x => x.State.IsStatusInProgress, x => x.StatusLoadingIcon.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.IsStatusSuccess, x => x.StatusSuccessIcon.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.IsStatusError, x => x.StatusErrorIcon.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.IsStatusReady, x => x.StatusReadyIcon.IsVisible);
            this.OneWayBind(ViewModel, x => x.State.FooterInfoText, x => x.FooterInfoTextBlock.Text);

            this.WhenAnyValue(x => x.ViewModel!.State.StatusMessage)
                .Subscribe(Observer.Create<string>(msg => ToolTip.SetTip(StatusMessageTextBlock, msg)))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.FooterInfoText)
                .Subscribe(Observer.Create<string>(text => ToolTip.SetTip(FooterInfoTextBlock, text)))
                .DisposeWith(disposables);

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
