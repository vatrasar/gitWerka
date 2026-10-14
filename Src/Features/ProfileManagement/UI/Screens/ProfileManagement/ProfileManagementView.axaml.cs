/// <summary>
/// Screen: ProfileManagementView
/// Purpose: Screen for managing Git identities (authors), GitHub credentials, and profiles pairing them for commit signing and automated push authentication.
/// Available Functionalities:
/// - Listing, creating, editing, and deleting Git profiles.
/// - Listing, creating, editing, and deleting commit authors (name and email).
/// - Listing, creating, editing, and deleting GitHub credentials (username and access token).
/// - Associating existing authors and credentials with profiles, with inline shortcut creation.
/// - Setting the active profile for the currently open repository.
/// - Segmented tab navigation across Profiles, Authors, and Credentials workspaces.
/// Key UI Elements:
/// - BackButton, LoadingProgressBar, ProfilesTabButton, AuthorsTabButton, CredentialsTabButton.
/// - ErrorMessageBanner, ErrorMessageTextBlock, SuccessMessageBanner, SuccessMessageTextBlock.
/// - ProfilesTabPanel, NewProfileButton, NoProfilesTextBlock, ProfilesListBox, ProfileEditorHeaderTextBlock, ProfileNameTextBox, QuickNewAuthorButton, AuthorComboBox, QuickNewCredentialsButton, CredentialsComboBox, SaveProfileButton, CancelProfileEditButton, DeleteProfileButton, SetActiveForRepoButton.
/// - AuthorsTabPanel, NewAuthorButton, NoAuthorsTextBlock, AuthorsListBox, AuthorEditorHeaderTextBlock, AuthorNameTextBox, AuthorEmailTextBox, SaveAuthorButton, CancelAuthorEditButton, DeleteAuthorButton.
/// - CredentialsTabPanel, NewCredentialsButton, NoCredentialsTextBlock, CredentialsListBox, CredentialsEditorHeaderTextBlock, CredentialsUsernameTextBox, CredentialsAccessTokenTextBox, SaveCredentialsButton, CancelCredentialsEditButton, DeleteCredentialsButton.
/// Navigation:
/// - Navigate From: CommitLogView via Profile dropdown menu "Manage profiles...".
/// - Navigate To: CommitLogView via header BackButton or router back.
/// </summary>
namespace GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.ProfileManagement.Domain.Enums;
using ReactiveUI;

public partial class ProfileManagementView : ReactiveUserControl<ProfileManagementViewModel>
{
    public ProfileManagementView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            SetupNavigation(disposables);
            SetupTabSwitching(disposables);
            SetupCollections(disposables);
            SetupProfileEditor(disposables);
            SetupAuthorEditor(disposables);
            SetupCredentialsEditor(disposables);
            SetupFeedbackBanners(disposables);
        });
    }

    private void SetupNavigation(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.NavigateBackCommand, x => x.BackButton);
        this.OneWayBind(ViewModel, x => x.State.IsLoading, x => x.LoadingProgressBar.IsVisible);
    }

    private void SetupTabSwitching(CompositeDisposable disposables)
    {
        ProfilesTabButton.Click += (s, e) => ViewModel?.SetActiveTab(ProfileManagementTab.Profiles);
        AuthorsTabButton.Click += (s, e) => ViewModel?.SetActiveTab(ProfileManagementTab.Authors);
        CredentialsTabButton.Click += (s, e) => ViewModel?.SetActiveTab(ProfileManagementTab.Credentials);

        QuickNewAuthorButton.Click += (s, e) =>
        {
            ViewModel?.SetActiveTab(ProfileManagementTab.Authors);
            ViewModel?.StartCreateAuthorCommand.Execute().Subscribe();
        };

        QuickNewCredentialsButton.Click += (s, e) =>
        {
            ViewModel?.SetActiveTab(ProfileManagementTab.Credentials);
            ViewModel?.StartCreateCredentialsCommand.Execute().Subscribe();
        };

        this.WhenAnyValue(x => x.ViewModel!.State.ActiveTab)
            .Subscribe(Observer.Create<ProfileManagementTab>(tab =>
            {
                ProfilesTabPanel.IsVisible = tab == ProfileManagementTab.Profiles;
                AuthorsTabPanel.IsVisible = tab == ProfileManagementTab.Authors;
                CredentialsTabPanel.IsVisible = tab == ProfileManagementTab.Credentials;

                ProfilesTabButton.Classes.Set("active", tab == ProfileManagementTab.Profiles);
                AuthorsTabButton.Classes.Set("active", tab == ProfileManagementTab.Authors);
                CredentialsTabButton.Classes.Set("active", tab == ProfileManagementTab.Credentials);
            }))
            .DisposeWith(disposables);
    }

    private void SetupCollections(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.Profiles, x => x.ProfilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.Authors, x => x.AuthorsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.Authors, x => x.AuthorComboBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.CredentialsList, x => x.CredentialsListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.CredentialsList, x => x.CredentialsComboBox.ItemsSource);

        this.WhenAnyValue(x => x.ViewModel!.State.Profiles.Count)
            .Select(count => count == 0)
            .Subscribe(Observer.Create<bool>(empty => NoProfilesTextBlock.IsVisible = empty))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.Authors.Count)
            .Select(count => count == 0)
            .Subscribe(Observer.Create<bool>(empty => NoAuthorsTextBlock.IsVisible = empty))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsList.Count)
            .Select(count => count == 0)
            .Subscribe(Observer.Create<bool>(empty => NoCredentialsTextBlock.IsVisible = empty))
            .DisposeWith(disposables);
    }

    private void SetupProfileEditor(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.StartCreateProfileCommand, x => x.NewProfileButton);
        this.BindCommand(ViewModel, x => x.SaveProfileCommand, x => x.SaveProfileButton);
        this.BindCommand(ViewModel, x => x.CancelProfileEditCommand, x => x.CancelProfileEditButton);

        this.WhenAnyValue(x => x.ProfileNameTextBox.Text)
            .WhereNotNull()
            .Subscribe(Observer.Create<string>(text => ViewModel?.UpdateProfileNameInput(text)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ProfileNameInput)
            .Subscribe(Observer.Create<string>(text =>
            {
                if (ProfileNameTextBox.Text != text)
                {
                    ProfileNameTextBox.Text = text;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AuthorComboBox.SelectedItem)
            .Subscribe(Observer.Create<object?>(item => ViewModel?.SelectAuthorForProfile(item as GitAuthor)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedAuthor)
            .Subscribe(Observer.Create<GitAuthor?>(author => AuthorComboBox.SelectedItem = author))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsComboBox.SelectedItem)
            .Subscribe(Observer.Create<object?>(item => ViewModel?.SelectCredentialsForProfile(item as GitProfileCredentials)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedCredentials)
            .Subscribe(Observer.Create<GitProfileCredentials?>(creds => CredentialsComboBox.SelectedItem = creds))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ProfilesListBox.SelectedItem)
            .WhereNotNull()
            .OfType<GitProfile>()
            .Subscribe(Observer.Create<GitProfile>(profile =>
            {
                ViewModel?.StartEditProfileCommand.Execute(profile).Subscribe();
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.EditingProfileId)
            .Subscribe(Observer.Create<int?>(id =>
            {
                bool isExisting = id.HasValue;
                DeleteProfileButton.IsVisible = isExisting;
                SetActiveForRepoButton.IsVisible = isExisting && !string.IsNullOrWhiteSpace(ViewModel?.State.RepositoryPath);
            }))
            .DisposeWith(disposables);

        DeleteProfileButton.Click += (s, e) =>
        {
            if (ProfilesListBox.SelectedItem is GitProfile profile)
            {
                ViewModel?.DeleteProfileCommand.Execute(profile).Subscribe();
            }
        };

        SetActiveForRepoButton.Click += (s, e) =>
        {
            if (ProfilesListBox.SelectedItem is GitProfile profile)
            {
                ViewModel?.SetAsActiveProfileCommand.Execute(profile).Subscribe();
            }
        };
    }

    private void SetupAuthorEditor(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.StartCreateAuthorCommand, x => x.NewAuthorButton);
        this.BindCommand(ViewModel, x => x.SaveAuthorCommand, x => x.SaveAuthorButton);
        this.BindCommand(ViewModel, x => x.CancelAuthorEditCommand, x => x.CancelAuthorEditButton);

        this.WhenAnyValue(x => x.AuthorNameTextBox.Text)
            .WhereNotNull()
            .Subscribe(Observer.Create<string>(text => ViewModel?.UpdateAuthorNameInput(text)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AuthorNameInput)
            .Subscribe(Observer.Create<string>(text =>
            {
                if (AuthorNameTextBox.Text != text)
                {
                    AuthorNameTextBox.Text = text;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AuthorEmailTextBox.Text)
            .WhereNotNull()
            .Subscribe(Observer.Create<string>(text => ViewModel?.UpdateAuthorEmailInput(text)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AuthorEmailInput)
            .Subscribe(Observer.Create<string>(text =>
            {
                if (AuthorEmailTextBox.Text != text)
                {
                    AuthorEmailTextBox.Text = text;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AuthorsListBox.SelectedItem)
            .WhereNotNull()
            .OfType<GitAuthor>()
            .Subscribe(Observer.Create<GitAuthor>(author =>
            {
                ViewModel?.StartEditAuthorCommand.Execute(author).Subscribe();
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.EditingAuthorId)
            .Subscribe(Observer.Create<int?>(id => DeleteAuthorButton.IsVisible = id.HasValue))
            .DisposeWith(disposables);

        DeleteAuthorButton.Click += (s, e) =>
        {
            if (AuthorsListBox.SelectedItem is GitAuthor author)
            {
                ViewModel?.DeleteAuthorCommand.Execute(author).Subscribe();
            }
        };
    }

    private void SetupCredentialsEditor(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.StartCreateCredentialsCommand, x => x.NewCredentialsButton);
        this.BindCommand(ViewModel, x => x.SaveCredentialsCommand, x => x.SaveCredentialsButton);
        this.BindCommand(ViewModel, x => x.CancelCredentialsEditCommand, x => x.CancelCredentialsEditButton);

        this.WhenAnyValue(x => x.CredentialsUsernameTextBox.Text)
            .WhereNotNull()
            .Subscribe(Observer.Create<string>(text => ViewModel?.UpdateCredentialsUsernameInput(text)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsUsernameInput)
            .Subscribe(Observer.Create<string>(text =>
            {
                if (CredentialsUsernameTextBox.Text != text)
                {
                    CredentialsUsernameTextBox.Text = text;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsAccessTokenTextBox.Text)
            .WhereNotNull()
            .Subscribe(Observer.Create<string>(text => ViewModel?.UpdateCredentialsAccessTokenInput(text)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsAccessTokenInput)
            .Subscribe(Observer.Create<string>(text =>
            {
                if (CredentialsAccessTokenTextBox.Text != text)
                {
                    CredentialsAccessTokenTextBox.Text = text;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsListBox.SelectedItem)
            .WhereNotNull()
            .OfType<GitProfileCredentials>()
            .Subscribe(Observer.Create<GitProfileCredentials>(creds =>
            {
                ViewModel?.StartEditCredentialsCommand.Execute(creds).Subscribe();
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.EditingCredentialsId)
            .Subscribe(Observer.Create<int?>(id => DeleteCredentialsButton.IsVisible = id.HasValue))
            .DisposeWith(disposables);

        DeleteCredentialsButton.Click += (s, e) =>
        {
            if (CredentialsListBox.SelectedItem is GitProfileCredentials creds)
            {
                ViewModel?.DeleteCredentialsCommand.Execute(creds).Subscribe();
            }
        };
    }

    private void SetupFeedbackBanners(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.ErrorMessage, x => x.ErrorMessageTextBlock.Text);
        this.WhenAnyValue(x => x.ViewModel!.State.ErrorMessage)
            .Select(msg => !string.IsNullOrWhiteSpace(msg))
            .Subscribe(Observer.Create<bool>(visible => ErrorMessageBanner.IsVisible = visible))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.SuccessMessage, x => x.SuccessMessageTextBlock.Text);
        this.WhenAnyValue(x => x.ViewModel!.State.SuccessMessage)
            .Select(msg => !string.IsNullOrWhiteSpace(msg))
            .Subscribe(Observer.Create<bool>(visible => SuccessMessageBanner.IsVisible = visible))
            .DisposeWith(disposables);
    }
}
