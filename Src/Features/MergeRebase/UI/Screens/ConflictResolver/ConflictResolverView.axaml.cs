/// <summary>
/// Screen: ConflictResolverView
/// Purpose: 3-panel merge conflict resolver screen inspired by GitKraken.
/// Available Functionalities:
/// - Side-by-side inspection of conflicting changes: Panel A (Local/Ours) and Panel B (Remote/Theirs).
/// - Conflict hunk selection using checkboxes to pick local, remote, or both changes into the merged output.
/// - Live reactive Output panel showing the merged document with line numbers and highlighted conflict blocks.
/// - Next/Previous conflict navigation.
/// - Top action bar with "Take All Ours", "Take All Theirs", and "Save" (writes file to disk, stages it in Git index, and returns).
/// - Cancel / Back navigation to return to the previous screen.
/// Key UI Elements:
/// - BackButton, FilePathTextBlock, ConflictCountTextBlock, TakeAllOursButton, TakeAllTheirsButton, SaveButton.
/// - OursAllCheckBox, OursTitleTextBlock, OursLinesItemsControl.
/// - TheirsAllCheckBox, TheirsTitleTextBlock, TheirsLinesItemsControl.
/// - ConflictStatusTextBlock, PreviousConflictButton, NextConflictButton, OutputLinesItemsControl.
/// Navigation:
/// - Navigate From: CommitLogView (clicking a file under Conflicted Files or Resolved Files).
/// - Navigate To: Return to CommitLogView upon saving or canceling.
/// </summary>
namespace GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;

using System;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using GitWerka.Features.MergeRebase.Resources;
using ReactiveUI;

public partial class ConflictResolverView : ReactiveUserControl<ConflictResolverViewModel>
{
    public ConflictResolverView()
    {
        InitializeComponent();

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

            this.OneWayBind(ViewModel, x => x.State.OursDisplayLines, x => x.OursLinesItemsControl.ItemsSource);
            this.OneWayBind(ViewModel, x => x.State.TheirsDisplayLines, x => x.TheirsLinesItemsControl.ItemsSource);
            this.OneWayBind(ViewModel, x => x.State.OutputDisplayLines, x => x.OutputLinesItemsControl.ItemsSource);
        });
    }
}
