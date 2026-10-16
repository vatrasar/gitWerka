namespace GitWerka.Features.MergeRebase.UI.Screens.ConflictResolver;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using GitWerka.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// ViewModel for the 3-panel merge conflict resolver screen.
/// </summary>
public partial class ConflictResolverViewModel : ViewModelBase<ConflictResolverState>, IRoutableViewModel
{
    private readonly IGitService _gitService;

    public string? UrlPathSegment => "conflict-resolver";

    public IScreen HostScreen { get; }

    public ConflictResolverViewModel(
        IScreen hostScreen,
        IGitService gitService,
        string repositoryPath,
        string filePath) : base(new ConflictResolverState
        {
            RepositoryPath = repositoryPath,
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        })
    {
        HostScreen = hostScreen;
        _gitService = gitService;

        LoadDataCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Toggles selection of the Ours (local) changes for the specified conflict hunk.
    /// Usages: ConflictResolverView.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    public void ToggleHunkOurs(int hunkIndex)
    {
        var updatedChunks = UpdateChunkSelection(State.Chunks, hunkIndex, toggleOurs: true);
        RefreshDisplayState(updatedChunks, State.ActiveConflictIndex);
    }

    /// <summary>
    /// Toggles selection of the Theirs (remote) changes for the specified conflict hunk.
    /// Usages: ConflictResolverView.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    public void ToggleHunkTheirs(int hunkIndex)
    {
        var updatedChunks = UpdateChunkSelection(State.Chunks, hunkIndex, toggleOurs: false);
        RefreshDisplayState(updatedChunks, State.ActiveConflictIndex);
    }

    /// <summary>
    /// Updates the raw merged text when manually edited by the user in the output editor.
    /// Usages: ConflictResolverView.
    /// </summary>
    /// <param name="newText">The updated content.</param>
    public void UpdateMergedOutputText(string newText)
    {
        UpdateState(s => s with { MergedOutputText = newText });
    }

    [ReactiveCommand]
    private async Task LoadData()
    {
        try
        {
            ConflictFileModel model = await _gitService.GetConflictFileAsync(State.RepositoryPath, State.FilePath);
            var (outputLines, mergedText) = BuildOutput(model.Chunks);
            var oursLines = BuildOursLines(model.Chunks);
            var theirsLines = BuildTheirsLines(model.Chunks);

            bool allOurs = model.TotalConflicts > 0 && model.Chunks.Where(c => c.IsConflict).All(c => c.IsOursSelected);
            bool allTheirs = model.TotalConflicts > 0 && model.Chunks.Where(c => c.IsConflict).All(c => c.IsTheirsSelected);

            UpdateState(s => s with
            {
                FileName = model.FileName,
                TotalConflicts = model.TotalConflicts,
                OursCommitSha = model.OursCommitSha,
                OursBranch = model.OursBranch,
                TheirsCommitSha = model.TheirsCommitSha,
                TheirsBranch = model.TheirsBranch,
                Chunks = model.Chunks,
                OursDisplayLines = oursLines,
                TheirsDisplayLines = theirsLines,
                OutputDisplayLines = outputLines,
                MergedOutputText = mergedText,
                IsAllOursChecked = allOurs,
                IsAllTheirsChecked = allTheirs,
                ActiveConflictIndex = model.TotalConflicts > 0 ? 1 : 0
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void ToggleAllOurs()
    {
        bool targetValue = !State.IsAllOursChecked;
        var updated = State.Chunks.Select(c => c.IsConflict
            ? c with { IsOursSelected = targetValue }
            : c).ToImmutableList();

        RefreshDisplayState(updated, State.ActiveConflictIndex);
    }

    [ReactiveCommand]
    private void ToggleAllTheirs()
    {
        bool targetValue = !State.IsAllTheirsChecked;
        var updated = State.Chunks.Select(c => c.IsConflict
            ? c with { IsTheirsSelected = targetValue }
            : c).ToImmutableList();

        RefreshDisplayState(updated, State.ActiveConflictIndex);
    }

    [ReactiveCommand]
    private void NextConflict()
    {
        if (State.TotalConflicts == 0)
        {
            return;
        }

        int nextIndex = State.ActiveConflictIndex >= State.TotalConflicts ? 1 : State.ActiveConflictIndex + 1;
        UpdateState(s => s with { ActiveConflictIndex = nextIndex });
    }

    [ReactiveCommand]
    private void PreviousConflict()
    {
        if (State.TotalConflicts == 0)
        {
            return;
        }

        int prevIndex = State.ActiveConflictIndex <= 1 ? State.TotalConflicts : State.ActiveConflictIndex - 1;
        UpdateState(s => s with { ActiveConflictIndex = prevIndex });
    }

    [ReactiveCommand]
    private async Task SaveAndResolve()
    {
        try
        {
            UpdateState(s => s with { IsSaving = true, ErrorMessage = null });
            await _gitService.SaveConflictResolutionAsync(State.RepositoryPath, State.FilePath, State.MergedOutputText);
            UpdateState(s => s with { IsSaving = false });
            HostScreen.Router.NavigateBack.Execute().Subscribe();
        }
        catch (Exception ex)
        {
            UpdateState(s => s with { IsSaving = false, ErrorMessage = ex.Message });
        }
    }

    [ReactiveCommand]
    private void Cancel()
    {
        HostScreen.Router.NavigateBack.Execute().Subscribe();
    }

    private void RefreshDisplayState(ImmutableList<ConflictFileChunk> chunks, int activeIndex)
    {
        var (outputLines, mergedText) = BuildOutput(chunks);
        var oursLines = BuildOursLines(chunks);
        var theirsLines = BuildTheirsLines(chunks);

        bool allOurs = chunks.Where(c => c.IsConflict).All(c => c.IsOursSelected);
        bool allTheirs = chunks.Where(c => c.IsConflict).All(c => c.IsTheirsSelected);

        UpdateState(s => s with
        {
            Chunks = chunks,
            OursDisplayLines = oursLines,
            TheirsDisplayLines = theirsLines,
            OutputDisplayLines = outputLines,
            MergedOutputText = mergedText,
            IsAllOursChecked = allOurs,
            IsAllTheirsChecked = allTheirs,
            ActiveConflictIndex = activeIndex
        });
    }

    private static ImmutableList<ConflictFileChunk> UpdateChunkSelection(
        ImmutableList<ConflictFileChunk> chunks,
        int hunkIndex,
        bool toggleOurs)
    {
        return chunks.Select(c =>
        {
            if (!c.IsConflict || c.HunkIndex != hunkIndex)
            {
                return c;
            }

            return toggleOurs
                ? c with { IsOursSelected = !c.IsOursSelected }
                : c with { IsTheirsSelected = !c.IsTheirsSelected };
        }).ToImmutableList();
    }

    private ImmutableList<ConflictLineDisplay> BuildOursLines(ImmutableList<ConflictFileChunk> chunks)
    {
        List<ConflictLineDisplay> lines = new();
        int lineCounter = 1;

        foreach (ConflictFileChunk chunk in chunks)
        {
            lineCounter = AppendOursChunkLines(chunk, lines, lineCounter);
        }

        return lines.ToImmutableList();
    }

    private int AppendOursChunkLines(ConflictFileChunk chunk, List<ConflictLineDisplay> lines, int lineCounter)
    {
        if (!chunk.IsConflict)
        {
            return AppendCommonLines(chunk.CommonLines, lines, lineCounter, isOurs: true);
        }

        bool isFirst = true;
        foreach (string text in chunk.OursLines)
        {
            lines.Add(new ConflictLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = true,
                IsHunkFirstLine = isFirst,
                HunkIndex = chunk.HunkIndex,
                IsSelected = chunk.IsOursSelected,
                IsOurs = true,
                ToggleCommand = ReactiveCommand.Create(() => ToggleHunkOurs(chunk.HunkIndex))
            });
            isFirst = false;
        }

        return lineCounter;
    }

    private ImmutableList<ConflictLineDisplay> BuildTheirsLines(ImmutableList<ConflictFileChunk> chunks)
    {
        List<ConflictLineDisplay> lines = new();
        int lineCounter = 1;

        foreach (ConflictFileChunk chunk in chunks)
        {
            lineCounter = AppendTheirsChunkLines(chunk, lines, lineCounter);
        }

        return lines.ToImmutableList();
    }

    private int AppendTheirsChunkLines(ConflictFileChunk chunk, List<ConflictLineDisplay> lines, int lineCounter)
    {
        if (!chunk.IsConflict)
        {
            return AppendCommonLines(chunk.CommonLines, lines, lineCounter, isOurs: false);
        }

        bool isFirst = true;
        foreach (string text in chunk.TheirsLines)
        {
            lines.Add(new ConflictLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = true,
                IsHunkFirstLine = isFirst,
                HunkIndex = chunk.HunkIndex,
                IsSelected = chunk.IsTheirsSelected,
                IsOurs = false,
                ToggleCommand = ReactiveCommand.Create(() => ToggleHunkTheirs(chunk.HunkIndex))
            });
            isFirst = false;
        }

        return lineCounter;
    }

    private static int AppendCommonLines(
        ImmutableList<string> commonLines,
        List<ConflictLineDisplay> lines,
        int lineCounter,
        bool isOurs)
    {
        foreach (string text in commonLines)
        {
            lines.Add(new ConflictLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = false,
                IsHunkFirstLine = false,
                HunkIndex = 0,
                IsSelected = false,
                IsOurs = isOurs
            });
        }

        return lineCounter;
    }

    private static (ImmutableList<ConflictOutputLineDisplay>, string) BuildOutput(ImmutableList<ConflictFileChunk> chunks)
    {
        List<ConflictOutputLineDisplay> lines = new();
        StringBuilder textBuilder = new();
        int lineCounter = 1;

        foreach (ConflictFileChunk chunk in chunks)
        {
            lineCounter = AppendOutputChunk(chunk, lines, textBuilder, lineCounter);
        }

        return (lines.ToImmutableList(), textBuilder.ToString());
    }

    private static int AppendOutputChunk(
        ConflictFileChunk chunk,
        List<ConflictOutputLineDisplay> lines,
        StringBuilder textBuilder,
        int lineCounter)
    {
        if (!chunk.IsConflict)
        {
            return AppendOutputCommonLines(chunk.CommonLines, lines, textBuilder, lineCounter);
        }

        if (chunk.IsOursSelected)
        {
            lineCounter = AppendOutputConflictLines(chunk.OursLines, lines, textBuilder, lineCounter, fromOurs: true, fromTheirs: false);
        }

        if (chunk.IsTheirsSelected)
        {
            lineCounter = AppendOutputConflictLines(chunk.TheirsLines, lines, textBuilder, lineCounter, fromOurs: false, fromTheirs: true);
        }

        return lineCounter;
    }

    private static int AppendOutputCommonLines(
        ImmutableList<string> commonLines,
        List<ConflictOutputLineDisplay> lines,
        StringBuilder textBuilder,
        int lineCounter)
    {
        foreach (string text in commonLines)
        {
            lines.Add(new ConflictOutputLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = false
            });
            textBuilder.AppendLine(text);
        }

        return lineCounter;
    }

    private static int AppendOutputConflictLines(
        ImmutableList<string> hunkLines,
        List<ConflictOutputLineDisplay> lines,
        StringBuilder textBuilder,
        int lineCounter,
        bool fromOurs,
        bool fromTheirs)
    {
        foreach (string text in hunkLines)
        {
            lines.Add(new ConflictOutputLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = true,
                IsFromOurs = fromOurs,
                IsFromTheirs = fromTheirs
            });
            textBuilder.AppendLine(text);
        }

        return lineCounter;
    }
}
