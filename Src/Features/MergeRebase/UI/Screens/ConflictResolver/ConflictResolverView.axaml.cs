/// <summary>
/// Screen: ConflictResolverView
/// Purpose: 3-panel merge conflict resolver screen inspired by GitKraken.
/// Available Functionalities:
/// - Side-by-side inspection of conflicting changes: Panel A (Local/Ours) and Panel B (Remote/Theirs).
/// - Line-by-line selection to take or un-take individual lines into the merged output, with GitKraken checkmark, plus, and minus hover buttons.
/// - Whole-hunk and whole-file toggles: Take All Ours, Take All Theirs, and panel header checkboxes.
/// - Live interactive Output panel with editable text editor, line numbers gutter with source indicators (A/B), and Reset button.
/// - Next/Previous conflict navigation.
/// - Save and resolve (writes file to disk, stages it in Git index, and returns) or Cancel.
/// Key UI Elements:
/// - BackButton, FilePathTextBlock, ConflictCountTextBlock, TakeAllOursButton, TakeAllTheirsButton, SaveButton.
/// - OursAllCheckBox, OursTitleTextBlock, OursLinesItemsControl.
/// - TheirsAllCheckBox, TheirsTitleTextBlock, TheirsLinesItemsControl.
/// - ConflictStatusTextBlock, PreviousConflictButton, NextConflictButton, ResetOutputButton.
/// - OutputGutterScrollViewer, OutputLineNumbersTextBlock, OutputCodeScrollViewer, OutputEditorTextBox.
/// Navigation:
/// - Navigate From: CommitLogView (clicking a conflicted file).
/// - Navigate To: Return to CommitLogView upon saving or canceling.
/// </summary>
namespace GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;

using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.MergeRebase.Resources;
using ReactiveUI;

public partial class ConflictResolverView : ReactiveUserControl<ConflictResolverViewModel>
{
    private bool _isUpdatingTextProgrammatically;

    public ConflictResolverView()
    {
        InitializeComponent();

        OutputCodeScrollViewer.ScrollChanged += HandleOutputCodeScrollChanged;
        OutputGutterScrollViewer.PointerWheelChanged += HandleOutputGutterPointerWheelChanged;
        OutputEditorTextBox.PropertyChanged += HandleOutputEditorTextChanged;

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, x => x.CancelCommand, x => x.BackButton);
            this.BindCommand(ViewModel, x => x.SaveAndResolveCommand, x => x.SaveButton);
            this.BindCommand(ViewModel, x => x.ToggleAllOursCommand, x => x.TakeAllOursButton);
            this.BindCommand(ViewModel, x => x.ToggleAllTheirsCommand, x => x.TakeAllTheirsButton);

            this.BindCommand(ViewModel, x => x.ToggleAllOursCommand, x => x.OursAllCheckBox);
            this.BindCommand(ViewModel, x => x.ToggleAllTheirsCommand, x => x.TheirsAllCheckBox);
            this.OneWayBind(ViewModel, x => x.State.IsAllOursChecked, x => x.OursAllCheckBox.IsChecked);
            this.OneWayBind(ViewModel, x => x.State.IsAllTheirsChecked, x => x.TheirsAllCheckBox.IsChecked);

            this.BindCommand(ViewModel, x => x.PreviousConflictCommand, x => x.PreviousConflictButton);
            this.BindCommand(ViewModel, x => x.NextConflictCommand, x => x.NextConflictButton);

            ResetOutputButton.Click += HandleResetOutputClicked;
            Disposable.Create(() => ResetOutputButton.Click -= HandleResetOutputClicked).DisposeWith(disposables);

            this.OneWayBind(ViewModel, x => x.State.FilePath, x => x.FilePathTextBlock.Text);

            this.WhenAnyValue(x => x.ViewModel!.State.TotalConflicts)
                .Subscribe(Observer.Create<int>(count =>
                {
                    ConflictCountTextBlock.Text = count == 1
                        ? MergeRebaseStrings.ConflictCountSingleFormat
                        : string.Format(MergeRebaseStrings.ConflictCountMultiFormat, count);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.OursCommitSha, x => x.ViewModel!.State.OursBranch)
                .Subscribe(Observer.Create<(string Sha, string Branch)>(t =>
                {
                    OursTitleTextBlock.Text = string.Format(MergeRebaseStrings.CommitOnBranchFormat, t.Sha, t.Branch);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.TheirsCommitSha, x => x.ViewModel!.State.TheirsBranch)
                .Subscribe(Observer.Create<(string Sha, string Branch)>(t =>
                {
                    TheirsTitleTextBlock.Text = string.Format(MergeRebaseStrings.CommitOnBranchFormat, t.Sha, t.Branch);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.ActiveConflictIndex, x => x.ViewModel!.State.TotalConflicts)
                .Subscribe(Observer.Create<(int Active, int Total)>(t =>
                {
                    ConflictStatusTextBlock.Text = string.Format(MergeRebaseStrings.ConflictOfFormat, t.Active, t.Total);
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.MergedOutputText)
                .Subscribe(Observer.Create<string>(text =>
                {
                    if (OutputEditorTextBox.Text != text)
                    {
                        _isUpdatingTextProgrammatically = true;
                        OutputEditorTextBox.Text = text;
                        _isUpdatingTextProgrammatically = false;
                        UpdateOutputGutter(text, ViewModel?.State.OutputDisplayLines ?? ImmutableList<ConflictOutputLineDisplay>.Empty);
                    }
                }))
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, x => x.State.OursDisplayLines, x => x.OursLinesItemsControl.ItemsSource);
            this.OneWayBind(ViewModel, x => x.State.TheirsDisplayLines, x => x.TheirsLinesItemsControl.ItemsSource);
        });
    }

    private void HandleOutputCodeScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        OutputGutterScrollViewer.Offset = new Vector(0, OutputCodeScrollViewer.Offset.Y);
    }

    private void HandleOutputGutterPointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        double newY = Math.Max(0, OutputCodeScrollViewer.Offset.Y - (e.Delta.Y * 40));
        OutputCodeScrollViewer.Offset = new Vector(OutputCodeScrollViewer.Offset.X, newY);
        e.Handled = true;
    }

    private void HandleOutputEditorTextChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _isUpdatingTextProgrammatically)
        {
            return;
        }

        string newText = OutputEditorTextBox.Text ?? string.Empty;
        UpdateOutputGutter(newText, ViewModel?.State.OutputDisplayLines ?? ImmutableList<ConflictOutputLineDisplay>.Empty);

        if (ViewModel != null && ViewModel.State.MergedOutputText != newText)
        {
            ViewModel.UpdateMergedOutputText(newText);
        }
    }

    private void HandleResetOutputClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel?.ResetOutputToSelection();
    }

    private void UpdateOutputGutter(string? text, ImmutableList<ConflictOutputLineDisplay> lines)
    {
        if (string.IsNullOrEmpty(text))
        {
            OutputLineNumbersTextBlock.Text = "  1";
            return;
        }

        string[] splitLines = text.Split('\n');
        StringBuilder sb = new();
        for (int i = 0; i < splitLines.Length; i++)
        {
            int lineNum = i + 1;
            string prefix = "  ";
            if (i < lines.Count && lines[i].IsConflict)
            {
                prefix = lines[i].IsFromOurs ? "A " : (lines[i].IsFromTheirs ? "B " : "  ");
            }

            sb.AppendLine($"{prefix}{lineNum}");
        }

        OutputLineNumbersTextBlock.Text = sb.ToString().TrimEnd('\r', '\n');
    }
}
