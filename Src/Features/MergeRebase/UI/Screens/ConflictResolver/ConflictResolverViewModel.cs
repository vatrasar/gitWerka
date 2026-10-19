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
using GitWerka.Features.MergeRebase.Resources;
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
    /// Toggles selection of the entire Ours (local) changes for the specified conflict hunk.
    /// Usages: ConflictResolverView, ConflictResolverViewModelTests.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    public void ToggleHunkOurs(int hunkIndex)
    {
        var updatedChunks = UpdateChunkSelection(State.Chunks, hunkIndex, toggleOurs: true);
        RefreshDisplayState(updatedChunks, State.ActiveConflictIndex);
    }

    /// <summary>
    /// Toggles selection of the entire Theirs (remote) changes for the specified conflict hunk.
    /// Usages: ConflictResolverView, ConflictResolverViewModelTests.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    public void ToggleHunkTheirs(int hunkIndex)
    {
        var updatedChunks = UpdateChunkSelection(State.Chunks, hunkIndex, toggleOurs: false);
        RefreshDisplayState(updatedChunks, State.ActiveConflictIndex);
    }

    /// <summary>
    /// Toggles selection of an individual line from Ours (local) in the specified conflict hunk.
    /// Usages: ConflictResolverView, ConflictResolverViewModelTests.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    /// <param name="lineIndex">The 0-based index of the line within the hunk Ours lines.</param>
    public void ToggleOursLine(int hunkIndex, int lineIndex)
    {
        var updatedChunks = ToggleChunkLine(State.Chunks, hunkIndex, lineIndex, isOurs: true);
        RefreshDisplayState(updatedChunks, State.ActiveConflictIndex);
    }

    /// <summary>
    /// Toggles selection of an individual line from Theirs (remote) in the specified conflict hunk.
    /// Usages: ConflictResolverView, ConflictResolverViewModelTests.
    /// </summary>
    /// <param name="hunkIndex">The 1-based index of the conflict hunk.</param>
    /// <param name="lineIndex">The 0-based index of the line within the hunk Theirs lines.</param>
    public void ToggleTheirsLine(int hunkIndex, int lineIndex)
    {
        var updatedChunks = ToggleChunkLine(State.Chunks, hunkIndex, lineIndex, isOurs: false);
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

    /// <summary>
    /// Resets the merged output text to match the currently selected conflict lines.
    /// Usages: ConflictResolverView.
    /// </summary>
    public void ResetOutputToSelection()
    {
        var (outputLines, mergedText) = BuildOutput(State.Chunks);
        UpdateState(s => s with
        {
            OutputDisplayLines = outputLines,
            MergedOutputText = mergedText
        });
    }

    [ReactiveCommand]
    private async Task LoadData()
    {
        try
        {
            ConflictFileModel model = await _gitService.GetConflictFileAsync(State.RepositoryPath, State.FilePath);
            var normalizedChunks = NormalizeChunks(model.Chunks);
            var (outputLines, mergedText) = BuildOutput(normalizedChunks);
            var oursLines = BuildOursLines(normalizedChunks);
            var theirsLines = BuildTheirsLines(normalizedChunks);

            bool allOurs = model.TotalConflicts > 0 && normalizedChunks.Where(c => c.IsConflict).All(c => c.IsOursSelected);
            bool allTheirs = model.TotalConflicts > 0 && normalizedChunks.Where(c => c.IsConflict).All(c => c.IsTheirsSelected);

            UpdateState(s => s with
            {
                FileName = model.FileName,
                TotalConflicts = model.TotalConflicts,
                OursCommitSha = model.OursCommitSha,
                OursBranch = model.OursBranch,
                TheirsCommitSha = model.TheirsCommitSha,
                TheirsBranch = model.TheirsBranch,
                Chunks = normalizedChunks,
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
            ? SetAllLinesSelection(c, isOurs: true, select: targetValue)
            : c).ToImmutableList();

        RefreshDisplayState(updated, State.ActiveConflictIndex);
    }

    [ReactiveCommand]
    private void ToggleAllTheirs()
    {
        bool targetValue = !State.IsAllTheirsChecked;
        var updated = State.Chunks.Select(c => c.IsConflict
            ? SetAllLinesSelection(c, isOurs: false, select: targetValue)
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

    private static ImmutableList<ConflictFileChunk> NormalizeChunks(ImmutableList<ConflictFileChunk> chunks)
    {
        return chunks.Select(chunk =>
        {
            if (!chunk.IsConflict)
            {
                return chunk;
            }

            if (chunk.SelectedLines.Count > 0)
            {
                return chunk;
            }

            var selected = new List<ConflictSelectedLine>();
            if (chunk.IsOursSelected)
            {
                for (int i = 0; i < chunk.OursLines.Count; i++)
                {
                    selected.Add(new ConflictSelectedLine { IsOurs = true, LineIndex = i, Text = chunk.OursLines[i] });
                }
            }

            if (chunk.IsTheirsSelected)
            {
                for (int i = 0; i < chunk.TheirsLines.Count; i++)
                {
                    selected.Add(new ConflictSelectedLine { IsOurs = false, LineIndex = i, Text = chunk.TheirsLines[i] });
                }
            }

            return chunk with { SelectedLines = selected.ToImmutableList() };
        }).ToImmutableList();
    }

    private static ImmutableList<ConflictFileChunk> ToggleChunkLine(
        ImmutableList<ConflictFileChunk> chunks,
        int hunkIndex,
        int lineIndex,
        bool isOurs)
    {
        return chunks.Select(chunk =>
        {
            if (!chunk.IsConflict || chunk.HunkIndex != hunkIndex)
            {
                return chunk;
            }

            return ToggleLineInChunk(chunk, lineIndex, isOurs);
        }).ToImmutableList();
    }

    private static ConflictFileChunk ToggleLineInChunk(ConflictFileChunk chunk, int lineIndex, bool isOurs)
    {
        var existing = chunk.SelectedLines.FirstOrDefault(s => s.IsOurs == isOurs && s.LineIndex == lineIndex);
        if (existing != null)
        {
            var updated = chunk.SelectedLines.Remove(existing);
            return chunk with
            {
                SelectedLines = updated,
                IsOursSelected = updated.Any(s => s.IsOurs),
                IsTheirsSelected = updated.Any(s => !s.IsOurs)
            };
        }

        string text = isOurs ? chunk.OursLines[lineIndex] : chunk.TheirsLines[lineIndex];
        var added = chunk.SelectedLines.Add(new ConflictSelectedLine
        {
            IsOurs = isOurs,
            LineIndex = lineIndex,
            Text = text
        });

        return chunk with
        {
            SelectedLines = added,
            IsOursSelected = added.Any(s => s.IsOurs),
            IsTheirsSelected = added.Any(s => !s.IsOurs)
        };
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

            return ToggleHunkSelection(c, toggleOurs);
        }).ToImmutableList();
    }

    private static ConflictFileChunk ToggleHunkSelection(ConflictFileChunk chunk, bool isOurs)
    {
        var lineList = isOurs ? chunk.OursLines : chunk.TheirsLines;
        bool allSelected = lineList.Count > 0 && lineList.Select((_, idx) => idx)
            .All(idx => chunk.SelectedLines.Any(s => s.IsOurs == isOurs && s.LineIndex == idx));

        return SetAllLinesSelection(chunk, isOurs, !allSelected);
    }

    private static ConflictFileChunk SetAllLinesSelection(ConflictFileChunk chunk, bool isOurs, bool select)
    {
        var filtered = chunk.SelectedLines.Where(s => s.IsOurs != isOurs).ToList();
        if (select)
        {
            var lines = isOurs ? chunk.OursLines : chunk.TheirsLines;
            for (int i = 0; i < lines.Count; i++)
            {
                filtered.Add(new ConflictSelectedLine
                {
                    IsOurs = isOurs,
                    LineIndex = i,
                    Text = lines[i]
                });
            }
        }

        var result = filtered.ToImmutableList();
        return chunk with
        {
            SelectedLines = result,
            IsOursSelected = result.Any(s => s.IsOurs),
            IsTheirsSelected = result.Any(s => !s.IsOurs)
        };
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

        int selectedCount = chunk.SelectedLines.Count(s => s.IsOurs);
        bool allSelected = chunk.OursLines.Count > 0 && selectedCount == chunk.OursLines.Count;
        bool? hunkChecked = allSelected ? true : (selectedCount > 0 ? null : false);
        string hunkTooltip = allSelected ? MergeRebaseStrings.DontTakeHunkTooltip : MergeRebaseStrings.TakeHunkTooltip;
        var toggleHunkCommand = ReactiveCommand.Create(() => ToggleHunkOurs(chunk.HunkIndex));

        for (int i = 0; i < chunk.OursLines.Count; i++)
        {
            int lineIndex = i;
            string text = chunk.OursLines[i];
            bool isSelected = chunk.SelectedLines.Any(s => s.IsOurs && s.LineIndex == lineIndex);
            lines.Add(new ConflictLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = true,
                IsHunkFirstLine = i == 0,
                HunkIndex = chunk.HunkIndex,
                LineIndex = lineIndex,
                IsSelected = isSelected,
                IsOurs = true,
                IsHunkChecked = i == 0 ? hunkChecked : false,
                HunkTooltip = i == 0 ? hunkTooltip : string.Empty,
                ActionTooltip = isSelected ? MergeRebaseStrings.DontTakeLineTooltip : MergeRebaseStrings.TakeLineTooltip,
                ToggleCommand = ReactiveCommand.Create(() => ToggleOursLine(chunk.HunkIndex, lineIndex)),
                ToggleHunkCommand = i == 0 ? toggleHunkCommand : null
            });
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

        int selectedCount = chunk.SelectedLines.Count(s => !s.IsOurs);
        bool allSelected = chunk.TheirsLines.Count > 0 && selectedCount == chunk.TheirsLines.Count;
        bool? hunkChecked = allSelected ? true : (selectedCount > 0 ? null : false);
        string hunkTooltip = allSelected ? MergeRebaseStrings.DontTakeHunkTooltip : MergeRebaseStrings.TakeHunkTooltip;
        var toggleHunkCommand = ReactiveCommand.Create(() => ToggleHunkTheirs(chunk.HunkIndex));

        for (int i = 0; i < chunk.TheirsLines.Count; i++)
        {
            int lineIndex = i;
            string text = chunk.TheirsLines[i];
            bool isSelected = chunk.SelectedLines.Any(s => !s.IsOurs && s.LineIndex == lineIndex);
            lines.Add(new ConflictLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = text,
                IsConflict = true,
                IsHunkFirstLine = i == 0,
                HunkIndex = chunk.HunkIndex,
                LineIndex = lineIndex,
                IsSelected = isSelected,
                IsOurs = false,
                IsHunkChecked = i == 0 ? hunkChecked : false,
                HunkTooltip = i == 0 ? hunkTooltip : string.Empty,
                ActionTooltip = isSelected ? MergeRebaseStrings.DontTakeLineTooltip : MergeRebaseStrings.TakeLineTooltip,
                ToggleCommand = ReactiveCommand.Create(() => ToggleTheirsLine(chunk.HunkIndex, lineIndex)),
                ToggleHunkCommand = i == 0 ? toggleHunkCommand : null
            });
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
                LineIndex = -1,
                IsSelected = false,
                IsOurs = isOurs,
                IsHunkChecked = false,
                ActionTooltip = string.Empty,
                HunkTooltip = string.Empty
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

        foreach (ConflictSelectedLine selected in chunk.SelectedLines)
        {
            lines.Add(new ConflictOutputLineDisplay
            {
                LineNumber = lineCounter++.ToString(),
                Text = selected.Text,
                IsConflict = true,
                IsFromOurs = selected.IsOurs,
                IsFromTheirs = !selected.IsOurs
            });
            textBuilder.AppendLine(selected.Text);
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
}
