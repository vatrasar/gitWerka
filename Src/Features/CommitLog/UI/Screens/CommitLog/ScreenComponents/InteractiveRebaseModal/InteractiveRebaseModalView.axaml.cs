namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.InteractiveRebaseModal;

using System;
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
/// Component: InteractiveRebaseModalView
/// Purpose: Modal for interactive rebase step editing, reordering, and reword message adjustments.
/// Usage: Bound to InteractiveRebaseModalViewModel in CommitLogView.
/// Key UI elements: InteractiveRebaseModalBackdrop, RebaseStepsListBox, MoveUpStepButton, MoveDownStepButton, Action buttons, RebaseStepMessageTextBox.
/// Used In: CommitLogView
/// </summary>
public partial class InteractiveRebaseModalView : ReactiveUserControl<InteractiveRebaseModalViewModel>
{
    public InteractiveRebaseModalView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            SetupBindings(disposables);
            SetupCommands(disposables);
            SetupStepHandlers(disposables);
        });
    }

    private void SetupBindings(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, x => x.State.IsVisible, x => x.InteractiveRebaseModalBackdrop.IsVisible);
        this.OneWayBind(ViewModel, x => x.State.RebaseSteps, x => x.RebaseStepsListBox.ItemsSource);

        this.WhenAnyValue(x => x.ViewModel!.State.RebaseUpstreamShortSha)
            .Subscribe(Observer.Create<string?>(shortSha =>
            {
                RebaseUpstreamSubtitleTextBlock.Text = !string.IsNullOrEmpty(shortSha)
                    ? string.Format(CommitLogStrings.InteractiveRebaseSubtitle, shortSha)
                    : string.Empty;
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel!.State.SelectedRebaseStep)
            .Subscribe(Observer.Create<RebaseStep?>(step =>
            {
                if (step != null && RebaseStepMessageTextBox.Text != step.NewMessage)
                {
                    RebaseStepMessageTextBox.Text = step.NewMessage;
                }

                if (!ReferenceEquals(RebaseStepsListBox.SelectedItem, step))
                {
                    RebaseStepsListBox.SelectedItem = step;
                }
            }))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.RebaseStepMessageTextBox.Text)
            .Subscribe(Observer.Create<string?>(text =>
            {
                if (ViewModel?.State.SelectedRebaseStep is RebaseStep step && text != null && text != step.NewMessage)
                {
                    ViewModel.UpdateRebaseStepMessage(step, text);
                }
            }))
            .DisposeWith(disposables);
    }

    private void SetupCommands(CompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, x => x.HideInteractiveRebaseModalCommand, x => x.CancelInteractiveRebaseButton);
        this.BindCommand(ViewModel, x => x.ExecuteInteractiveRebaseCommand, x => x.ConfirmInteractiveRebaseButton);
        this.BindCommand(ViewModel, x => x.MoveRebaseStepUpCommand, x => x.MoveUpStepButton);
        this.BindCommand(ViewModel, x => x.MoveRebaseStepDownCommand, x => x.MoveDownStepButton);

        ActionPickButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Pick);
        ActionRewordButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Reword);
        ActionEditButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Edit);
        ActionSquashButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Squash);
        ActionFixupButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Fixup);
        ActionDropButton.Click += (s, e) => ApplyRebaseAction(RebaseStepAction.Drop);
    }

    private void SetupStepHandlers(CompositeDisposable disposables)
    {
        RebaseStepsListBox.SelectionChanged += (sender, e) =>
        {
            if (RebaseStepsListBox.SelectedItem is RebaseStep step && ViewModel != null)
            {
                ViewModel.SelectRebaseStep(step);
            }
        };
    }

    private void ApplyRebaseAction(RebaseStepAction action)
    {
        if (ViewModel?.State.SelectedRebaseStep != null)
        {
            ViewModel.ChangeRebaseStepAction(ViewModel.State.SelectedRebaseStep, action);
        }
    }
}
