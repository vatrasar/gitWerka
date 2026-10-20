namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.InteractiveRebaseModal;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Immutable state record for the InteractiveRebaseModal smart component.
/// </summary>
public record InteractiveRebaseModalState
{
    public bool IsVisible { get; init; }

    public string RepositoryPath { get; init; } = string.Empty;

    public string? RebaseUpstreamSha { get; init; }

    public string? RebaseUpstreamShortSha { get; init; }

    public ImmutableList<RebaseStep> RebaseSteps { get; init; } = ImmutableList<RebaseStep>.Empty;

    public RebaseStep? SelectedRebaseStep { get; init; }

    public bool IsLoading { get; init; }

    public string? ErrorMessage { get; init; }
}
