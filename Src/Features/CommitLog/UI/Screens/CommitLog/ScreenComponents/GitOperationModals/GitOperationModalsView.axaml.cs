namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.GitOperationModals;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Resources;
using ReactiveUI;

/// <summary>
/// Component: GitOperationModalsView
/// Purpose: Groups modals for Git operations: branch creation, remote creation, tag creation, stashes, merge, reset, and credentials authentication.
/// Usage: Bound to GitOperationModalsViewModel in CommitLogView.
/// Key UI elements: CreateBranchModalBackdrop, CreateRemoteModalBackdrop, CreateTagModalBackdrop, CreateStashModalBackdrop, ResetConfirmModalBackdrop, MergeModalBackdrop, CredentialsModalBackdrop.
/// Used In: CommitLogView
/// </summary>
public partial class GitOperationModalsView : ReactiveUserControl<GitOperationModalsViewModel>
{
    public GitOperationModalsView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            SetupBindings(disposables);
            SetupCommands(disposables);
            SetupTextInputs(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.IsCreateBranchModalVisible, x => x.CreateBranchModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCreateRemoteModalVisible, x => x.CreateRemoteModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCreateTagModalVisible, x => x.CreateTagModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsStashModalVisible, x => x.CreateStashModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsResetConfirmModalVisible, x => x.ResetConfirmModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsMergeModalVisible, x => x.MergeModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.IsCredentialsModalVisible, x => x.CredentialsModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.AvailableBranches, x => x.MergeBranchesComboBox.ItemsSource);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedMergeBranch)
            .Subscribe(Observer.Create<string?>(branchName =>
            {
                if (MergeBranchesComboBox.ItemsSource is ImmutableList<BranchInfo> list && !string.IsNullOrEmpty(branchName))
                {
                    BranchInfo? match = list.FirstOrDefault(b => b.Name == branchName);
                    if (match != null && !ReferenceEquals(MergeBranchesComboBox.SelectedItem, match))
                    {
                        MergeBranchesComboBox.SelectedItem = match;
                    }
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsAuthType)
            .Subscribe(Observer.Create<GitCredentialType>(authType =>
            {
                bool isHttps = authType == GitCredentialType.Https;
                CredentialsHttpsRadioButton.IsChecked = isHttps;
                CredentialsSshRadioButton.IsChecked = !isHttps;
                CredentialsUsernamePanel.IsVisible = isHttps;
                CredentialsPasswordLabelTextBlock.Text = isHttps
                    ? CommitLogStrings.CredentialsPasswordLabel
                    : CommitLogStrings.CredentialsPassphraseLabel;
                CredentialsSubtitleTextBlock.Text = isHttps
                    ? CommitLogStrings.CredentialsHttpsSubtitle
                    : CommitLogStrings.CredentialsSshSubtitle;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CredentialsErrorMessage)
            .Subscribe(Observer.Create<string?>(msg =>
            {
                CredentialsErrorMessageTextBlock.IsVisible = !string.IsNullOrEmpty(msg);
                CredentialsErrorMessageTextBlock.Text = msg ?? string.Empty;
            }))
            .DisposeWith(disposables);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.HideCreateBranchModalCommand, x => x.CancelCreateBranchButton);
        this.BindCommand(ViewModel, x => x.ConfirmCreateBranchCommand, x => x.ConfirmCreateBranchButton);

        this.BindCommand(ViewModel, x => x.HideCreateRemoteModalCommand, x => x.CancelCreateRemoteButton);
        this.BindCommand(ViewModel, x => x.ConfirmCreateRemoteCommand, x => x.ConfirmCreateRemoteButton);

        this.BindCommand(ViewModel, x => x.HideCreateTagModalCommand, x => x.CancelCreateTagButton);
        this.BindCommand(ViewModel, x => x.ConfirmCreateTagCommand, x => x.ConfirmCreateTagButton);

        this.BindCommand(ViewModel, x => x.HideStashModalCommand, x => x.CancelCreateStashButton);
        this.BindCommand(ViewModel, x => x.ConfirmCreateStashCommand, x => x.ConfirmCreateStashButton);

        this.BindCommand(ViewModel, x => x.HideResetConfirmModalCommand, x => x.CancelResetButton);
        this.BindCommand(ViewModel, x => x.ConfirmResetCommand, x => x.ConfirmResetButton);

        this.BindCommand(ViewModel, x => x.HideMergeModalCommand, x => x.CancelMergeButton);
        this.BindCommand(ViewModel, x => x.ConfirmMergeCommand, x => x.ConfirmMergeButton);

        this.BindCommand(ViewModel, x => x.HideCredentialsModalCommand, x => x.CancelCredentialsButton);
        this.BindCommand(ViewModel, x => x.SubmitCredentialsCommand, x => x.SubmitCredentialsButton);

        MergeBranchesComboBox.SelectionChanged += (sender, e) =>
        {
            if (MergeBranchesComboBox.SelectedItem is BranchInfo branch && ViewModel != null)
            {
                ViewModel.SelectMergeBranch(branch.Name);
            }
        };

        MergeFastForwardRadioButton.IsCheckedChanged += (s, e) =>
        {
            if (MergeFastForwardRadioButton.IsChecked == true && ViewModel != null)
            {
                ViewModel.SetMergeFastForwardMode(GitMergeFastForwardMode.FastForward);
            }
        };

        MergeNoFastForwardRadioButton.IsCheckedChanged += (s, e) =>
        {
            if (MergeNoFastForwardRadioButton.IsChecked == true && ViewModel != null)
            {
                ViewModel.SetMergeFastForwardMode(GitMergeFastForwardMode.NoFastForward);
            }
        };

        CredentialsHttpsRadioButton.IsCheckedChanged += (s, e) =>
        {
            if (CredentialsHttpsRadioButton.IsChecked == true && ViewModel != null)
            {
                ViewModel.SetCredentialsAuthType(GitCredentialType.Https);
            }
        };

        CredentialsSshRadioButton.IsCheckedChanged += (s, e) =>
        {
            if (CredentialsSshRadioButton.IsChecked == true && ViewModel != null)
            {
                ViewModel.SetCredentialsAuthType(GitCredentialType.SshKeyPassphrase);
            }
        };
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.NewBranchNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewBranchNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewRemoteNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewRemoteNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewRemoteUrlTextBox.Text)
            .Subscribe(Observer.Create<string?>(url => ViewModel?.UpdateNewRemoteUrlInput(url ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewTagNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateNewTagNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewTagMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateNewTagMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.NewStashMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateNewStashMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsUsernameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateCredentialsUsernameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.CredentialsPasswordTextBox.Text)
            .Subscribe(Observer.Create<string?>(pass => ViewModel?.UpdateCredentialsPasswordInput(pass ?? string.Empty)))
            .DisposeWith(disposables);
    }
}
