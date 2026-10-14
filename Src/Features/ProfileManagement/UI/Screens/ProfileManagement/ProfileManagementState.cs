namespace GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;

using System.Collections.Immutable;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.ProfileManagement.Domain.Enums;

/// <summary>
/// Immutable UI state for the ProfileManagement workspace screen.
/// </summary>
public record ProfileManagementState
{
    public string RepositoryPath { get; init; } = string.Empty;

    public ProfileManagementTab ActiveTab { get; init; } = ProfileManagementTab.Profiles;

    public bool IsLoading { get; init; }

    public string? ErrorMessage { get; init; }

    public string? SuccessMessage { get; init; }

    public ImmutableList<GitProfile> Profiles { get; init; } = ImmutableList<GitProfile>.Empty;

    public ImmutableList<GitAuthor> Authors { get; init; } = ImmutableList<GitAuthor>.Empty;

    public ImmutableList<GitProfileCredentials> CredentialsList { get; init; } = ImmutableList<GitProfileCredentials>.Empty;

    public GitProfile? ActiveRepositoryProfile { get; init; }

    public bool IsEditingProfile { get; init; }

    public int? EditingProfileId { get; init; }

    public string ProfileNameInput { get; init; } = string.Empty;

    public GitAuthor? SelectedAuthor { get; init; }

    public GitProfileCredentials? SelectedCredentials { get; init; }

    public bool IsEditingAuthor { get; init; }

    public int? EditingAuthorId { get; init; }

    public string AuthorNameInput { get; init; } = string.Empty;

    public string AuthorEmailInput { get; init; } = string.Empty;

    public bool IsEditingCredentials { get; init; }

    public int? EditingCredentialsId { get; init; }

    public string CredentialsUsernameInput { get; init; } = string.Empty;

    public string CredentialsAccessTokenInput { get; init; } = string.Empty;
}
