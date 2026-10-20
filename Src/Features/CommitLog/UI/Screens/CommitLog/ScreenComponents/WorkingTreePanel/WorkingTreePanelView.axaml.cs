namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.WorkingTreePanel;

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.Resources;
using ReactiveUI;

/// <summary>
/// Component: WorkingTreePanelView
/// Purpose: Working tree changes tab displaying staged/unstaged changes, conflict resolution mode, and commit composer.
/// Usage: Bound to WorkingTreePanelViewModel in CommitLogView.
/// Key UI elements: StagedChangesListBox, UnstagedChangesListBox, ConflictedFilesListBox, ResolvedFilesListBox, CommitMessageTextBox, CommitButton.
/// Used In: CommitLogView
/// </summary>
public partial class WorkingTreePanelView : ReactiveUserControl<WorkingTreePanelViewModel>
{
    public WorkingTreePanelView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            SetupBindings(disposables);
            SetupCommands(disposables);
            SetupTextInputs(disposables);
            SetupSelectionHandlers(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.StagedChanges, x => x.StagedChangesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.UnstagedChanges, x => x.UnstagedChangesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.ConflictedFiles, x => x.ConflictedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.ResolvedFiles, x => x.ResolvedFilesListBox.ItemsSource);
        this.OneWayBind(ViewModel, x => x.State.IsInConflictMode, x => x.ConflictWorkingTreePanel.IsVisible);

        this.WhenAnyValue(x => x.ViewModel!.State.IsInConflictMode)
            .Subscribe(Observer.Create<bool>(inConflict => NormalWorkingTreePanel.IsVisible = !inConflict))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ConflictedFiles.Count)
            .Subscribe(Observer.Create<int>(count =>
            {
                ConflictedFilesHeaderTextBlock.Text = string.Format(CommitLogStrings.ConflictedFilesHeader, count);
                MarkAllResolvedButton.IsVisible = count > 0;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.ResolvedFiles.Count)
            .Subscribe(Observer.Create<int>(count =>
            {
                ResolvedFilesHeaderTextBlock.Text = string.Format(CommitLogStrings.ResolvedFilesHeader, count);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.MergeSourceBranch, x => x.ViewModel!.State.MergeTargetBranch)
            .Subscribe(Observer.Create<(string Source, string Target)>(t =>
            {
                ConflictMergingDetailsTextBlock.Text = string.Format(CommitLogStrings.MergingBranchFormat, t.Source, t.Target);
            }))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.ActiveProfile, x => x.NoActiveProfileBanner.IsVisible, profile => profile == null);
        this.WhenAnyValue(x => x.ViewModel!.State.ActiveProfile)
            .Subscribe(Observer.Create<GitProfile?>(profile =>
            {
                ToolTip.SetTip(CommitButton, profile == null ? CommitLogStrings.CommitProfileRequiredToolTip : null);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.IsAmendCommit)
            .Subscribe(Observer.Create<bool>(isAmend =>
            {
                AmendCommitCheckBox.IsChecked = isAmend;
                CommitButton.Content = isAmend ? CommitLogStrings.AmendCommitButton : CommitLogStrings.CommitButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CommitMessageInput)
            .Subscribe(Observer.Create<string>(msg =>
            {
                if (CommitMessageTextBox.Text != msg)
                {
                    CommitMessageTextBox.Text = msg;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.CommitSubjectRemainingChars)
            .Subscribe(Observer.Create<int>(remaining =>
            {
                if (remaining >= 0)
                {
                    CommitMessageRemainingCountTextBlock.Text = string.Format(CommitLogStrings.CommitSubjectRemainingChars, remaining);
                }
                else
                {
                    CommitMessageRemainingCountTextBlock.Text = string.Format(CommitLogStrings.CommitSubjectOverLimitChars, Math.Abs(remaining));
                }

                CommitMessageRemainingCountTextBlock.Classes.Set("CommitCounterWarning", remaining >= 0 && remaining <= 10);
                CommitMessageRemainingCountTextBlock.Classes.Set("CommitCounterError", remaining < 0);
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.IsGeneratingAiCommit)
            .Subscribe(Observer.Create<bool>(isGenerating =>
            {
                AiGeneratingProgressBar.IsVisible = isGenerating;
                GenerateAiCommitLoadingIcon.IsVisible = isGenerating;
                GenerateAiCommitNormalIcon.IsVisible = !isGenerating;
                GenerateAiCommitButtonText.Text = isGenerating
                    ? CommitLogStrings.GeneratingAiCommitMessage
                    : CommitLogStrings.GenerateAiCommitButton;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.AiErrorMessage)
            .Subscribe(Observer.Create<string?>(err =>
            {
                bool hasError = !string.IsNullOrWhiteSpace(err);
                AiErrorBanner.IsVisible = hasError;
                AiErrorTextBlock.Text = err ?? string.Empty;
            }))
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, x => x.State.IsAiErrorSettingsActionVisible, x => x.AiErrorSettingsButton.IsVisible);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.StageAllCommand, x => x.StageAllButton);
        this.BindCommand(ViewModel, x => x.UnstageAllCommand, x => x.UnstageAllButton);
        this.BindCommand(ViewModel, x => x.MarkAllResolvedCommand, x => x.MarkAllResolvedButton);
        this.BindCommand(ViewModel, x => x.GenerateAiCommitCommand, x => x.GenerateAiCommitButton);
        this.BindCommand(ViewModel, x => x.DismissAiErrorCommand, x => x.AiErrorDismissButton);
        this.BindCommand(ViewModel, x => x.OpenSettingsCommand, x => x.AiErrorSettingsButton);
        this.BindCommand(ViewModel, x => x.ConfigureProfileCommand, x => x.ConfigureProfileButton);
        this.BindCommand(ViewModel, x => x.ExecuteCommitCommand, x => x.CommitButton);

        AmendCommitCheckBox.IsCheckedChanged += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.SetAmendCommit(AmendCommitCheckBox.IsChecked ?? false);
            }
        };
    }

    private void SetupTextInputs(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.CommitMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(msg => ViewModel?.UpdateCommitMessageInput(msg ?? string.Empty)))
            .DisposeWith(disposables);
    }

    private void SetupSelectionHandlers(CompositeDisposable disposables)
    {
        UnstagedChangesListBox.DoubleTapped += (sender, e) =>
        {
            if (UnstagedChangesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.StageFileCommand.Execute(item.Path).Subscribe();
            }
        };

        StagedChangesListBox.DoubleTapped += (sender, e) =>
        {
            if (StagedChangesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.UnstageFileCommand.Execute(item.Path).Subscribe();
            }
        };

        ConflictedFilesListBox.SelectionChanged += (sender, e) =>
        {
            if (ConflictedFilesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.OpenConflictResolverCommand.Execute(item).Subscribe();
                ConflictedFilesListBox.SelectedItem = null;
            }
        };

        ResolvedFilesListBox.SelectionChanged += (sender, e) =>
        {
            if (ResolvedFilesListBox.SelectedItem is WorkingTreeChange item && ViewModel != null)
            {
                ViewModel.OpenConflictResolverCommand.Execute(item).Subscribe();
                ResolvedFilesListBox.SelectedItem = null;
            }
        };
    }
}
