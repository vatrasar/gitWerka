namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.SettingsModal;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;

/// <summary>
/// Component: SettingsModalView
/// Purpose: Modal for editing repository Git author, project defaults, and AI commit assistant configuration.
/// Usage: Bound to SettingsModalViewModel in CommitLogView.
/// Key UI elements: SettingsModalBackdrop, ConfigUserNameTextBox, ConfigUserEmailTextBox, DefaultProjectsDirectoryTextBox, AutoFetchIntervalTextBox, AiApiKeyTextBox.
/// Used In: CommitLogView
/// </summary>
public partial class SettingsModalView : ReactiveUserControl<SettingsModalViewModel>
{
    public SettingsModalView()
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
        this.OneWayBind(ViewModel, x => x.State.IsVisible, x => x.SettingsModalBackdrop.IsVisible);

        this.WhenAnyValue(x => x.ViewModel!.State.ConfigUserNameInput)
            .Subscribe(Observer.Create<string>(name =>
            {
                if (ConfigUserNameTextBox.Text != name)
                {
                    ConfigUserNameTextBox.Text = name;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ConfigUserEmailInput)
            .Subscribe(Observer.Create<string>(email =>
            {
                if (ConfigUserEmailTextBox.Text != email)
                {
                    ConfigUserEmailTextBox.Text = email;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.DefaultProjectsDirectoryInput)
            .Subscribe(Observer.Create<string>(dir =>
            {
                if (DefaultProjectsDirectoryTextBox.Text != dir)
                {
                    DefaultProjectsDirectoryTextBox.Text = dir;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.MaxVisibleBranchesInput)
            .Subscribe(Observer.Create<string>(branches =>
            {
                if (MaxVisibleBranchesTextBox.Text != branches)
                {
                    MaxVisibleBranchesTextBox.Text = branches;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AutoFetchIntervalInput)
            .Subscribe(Observer.Create<string>(interval =>
            {
                if (AutoFetchIntervalTextBox.Text != interval)
                {
                    AutoFetchIntervalTextBox.Text = interval;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiApiKeyInput)
            .Subscribe(Observer.Create<string>(key =>
            {
                if (AiApiKeyTextBox.Text != key)
                {
                    AiApiKeyTextBox.Text = key;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiModelInput)
            .Subscribe(Observer.Create<string>(model =>
            {
                if (AiModelTextBox.Text != model)
                {
                    AiModelTextBox.Text = model;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiTokenLimitInput)
            .Subscribe(Observer.Create<string>(limit =>
            {
                if (AiTokenLimitTextBox.Text != limit)
                {
                    AiTokenLimitTextBox.Text = limit;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiCommitStyleInput)
            .Subscribe(Observer.Create<string>(style =>
            {
                if (AiCommitStyleTextBox.Text != style)
                {
                    AiCommitStyleTextBox.Text = style;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiProjectDescriptionInput)
            .Subscribe(Observer.Create<string>(desc =>
            {
                if (AiProjectDescriptionTextBox.Text != desc)
                {
                    AiProjectDescriptionTextBox.Text = desc;
                }
            }))
            .DisposeWith(disposables);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.HideSettingsModalCommand, x => x.CancelSettingsButton);
        this.BindCommand(ViewModel, x => x.ExecuteSaveSettingsCommand, x => x.ConfirmSettingsButton);
        BrowseDefaultProjectsDirectoryButton.Click += async (s, e) => await HandleBrowseDefaultProjectsDirectoryClickAsync();
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.ConfigUserNameTextBox.Text)
            .Subscribe(Observer.Create<string?>(name => ViewModel?.UpdateConfigUserNameInput(name ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ConfigUserEmailTextBox.Text)
            .Subscribe(Observer.Create<string?>(email => ViewModel?.UpdateConfigUserEmailInput(email ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.DefaultProjectsDirectoryTextBox.Text)
            .Subscribe(Observer.Create<string?>(dir => ViewModel?.UpdateDefaultProjectsDirectoryInput(dir ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.MaxVisibleBranchesTextBox.Text)
            .Subscribe(Observer.Create<string?>(branches => ViewModel?.UpdateMaxVisibleBranchesInput(branches ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AutoFetchIntervalTextBox.Text)
            .Subscribe(Observer.Create<string?>(interval => ViewModel?.UpdateAutoFetchIntervalInput(interval ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiApiKeyTextBox.Text)
            .Subscribe(Observer.Create<string?>(key => ViewModel?.UpdateAiApiKeyInput(key ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiModelTextBox.Text)
            .Subscribe(Observer.Create<string?>(model => ViewModel?.UpdateAiModelInput(model ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiTokenLimitTextBox.Text)
            .Subscribe(Observer.Create<string?>(limit => ViewModel?.UpdateAiTokenLimitInput(limit ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiCommitStyleTextBox.Text)
            .Subscribe(Observer.Create<string?>(style => ViewModel?.UpdateAiCommitStyleInput(style ?? string.Empty)))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.AiProjectDescriptionTextBox.Text)
            .Subscribe(Observer.Create<string?>(desc => ViewModel?.UpdateAiProjectDescriptionInput(desc ?? string.Empty)))
            .DisposeWith(disposables);
    }

    private async Task HandleBrowseDefaultProjectsDirectoryClickAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        IStorageFolder? startLocation = null;
        string currentVal = ViewModel?.State.DefaultProjectsDirectoryInput ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(currentVal) && Directory.Exists(currentVal))
        {
            startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(currentVal);
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Default Projects Directory",
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
        });

        if (folders.Count > 0 && ViewModel != null)
        {
            ViewModel.UpdateDefaultProjectsDirectoryInput(folders[0].Path.LocalPath);
        }
    }
}
