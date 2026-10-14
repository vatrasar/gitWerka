namespace GitWerka.Features.ProfileManagement.UI.Screens.ProfileManagement;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Mvvm;
using GitWerka.Features.ProfileManagement.Domain.Enums;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

/// <summary>
/// ViewModel for the profile management workspace screen managing authors, credentials, and Git profiles.
/// </summary>
public partial class ProfileManagementViewModel : ViewModelBase<ProfileManagementState>, IRoutableViewModel
{
    private readonly IProfileRepository _profileRepository;

    public string? UrlPathSegment => "profile-management";

    public IScreen HostScreen { get; }

    public ProfileManagementViewModel(
        IScreen hostScreen,
        IProfileRepository profileRepository,
        string? repositoryPath = null) : base(new ProfileManagementState
        {
            RepositoryPath = repositoryPath ?? string.Empty
        })
    {
        HostScreen = hostScreen;
        _profileRepository = profileRepository;

        LoadDataCommand.Execute().Subscribe();
    }

    /// <summary>
    /// Switches the active workspace tab between Profiles, Authors, and Credentials.
    /// Usages: ProfileManagementView tab buttons.
    /// </summary>
    /// <param name="tab">Target workspace tab.</param>
    public void SetActiveTab(ProfileManagementTab tab)
    {
        UpdateState(s => s with
        {
            ActiveTab = tab,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    /// <summary>
    /// Updates the profile name input field in the profile editor.
    /// Usages: ProfileManagementView profile editor input.
    /// </summary>
    /// <param name="name">Profile name text.</param>
    public void UpdateProfileNameInput(string name)
    {
        UpdateState(s => s with { ProfileNameInput = name ?? string.Empty });
    }

    /// <summary>
    /// Selects an author for the currently edited profile.
    /// Usages: ProfileManagementView author ComboBox selection.
    /// </summary>
    /// <param name="author">Selected author model.</param>
    public void SelectAuthorForProfile(GitAuthor? author)
    {
        UpdateState(s => s with { SelectedAuthor = author });
    }

    /// <summary>
    /// Selects credentials for the currently edited profile.
    /// Usages: ProfileManagementView credentials ComboBox selection.
    /// </summary>
    /// <param name="credentials">Selected credentials model.</param>
    public void SelectCredentialsForProfile(GitProfileCredentials? credentials)
    {
        UpdateState(s => s with { SelectedCredentials = credentials });
    }

    /// <summary>
    /// Updates the author full name input field.
    /// Usages: ProfileManagementView author editor input.
    /// </summary>
    /// <param name="name">Author full name.</param>
    public void UpdateAuthorNameInput(string name)
    {
        UpdateState(s => s with { AuthorNameInput = name ?? string.Empty });
    }

    /// <summary>
    /// Updates the author email input field.
    /// Usages: ProfileManagementView author editor input.
    /// </summary>
    /// <param name="email">Author email address.</param>
    public void UpdateAuthorEmailInput(string email)
    {
        UpdateState(s => s with { AuthorEmailInput = email ?? string.Empty });
    }

    /// <summary>
    /// Updates the GitHub username input field.
    /// Usages: ProfileManagementView credentials editor input.
    /// </summary>
    /// <param name="username">GitHub username.</param>
    public void UpdateCredentialsUsernameInput(string username)
    {
        UpdateState(s => s with { CredentialsUsernameInput = username ?? string.Empty });
    }

    /// <summary>
    /// Updates the GitHub personal access token input field.
    /// Usages: ProfileManagementView credentials editor input.
    /// </summary>
    /// <param name="token">Personal access token.</param>
    public void UpdateCredentialsAccessTokenInput(string token)
    {
        UpdateState(s => s with { CredentialsAccessTokenInput = token ?? string.Empty });
    }

    [ReactiveCommand]
    private void NavigateBack()
    {
        HostScreen.Router.NavigateBack.Execute().Subscribe();
    }

    [ReactiveCommand]
    private async Task LoadData()
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            IReadOnlyList<GitProfile> profiles = await _profileRepository.GetAllProfilesAsync();
            IReadOnlyList<GitAuthor> authors = await _profileRepository.GetAllAuthorsAsync();
            IReadOnlyList<GitProfileCredentials> creds = await _profileRepository.GetAllCredentialsAsync();

            GitProfile? activeRepoProfile = null;
            if (!string.IsNullOrWhiteSpace(State.RepositoryPath))
            {
                activeRepoProfile = await _profileRepository.GetActiveProfileForRepositoryAsync(State.RepositoryPath);
            }

            UpdateState(s => s with
            {
                IsLoading = false,
                Profiles = profiles.ToImmutableList(),
                Authors = authors.ToImmutableList(),
                CredentialsList = creds.ToImmutableList(),
                ActiveRepositoryProfile = activeRepoProfile
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void StartCreateProfile()
    {
        GitAuthor? defaultAuthor = State.Authors.FirstOrDefault();
        GitProfileCredentials? defaultCreds = State.CredentialsList.FirstOrDefault();

        UpdateState(s => s with
        {
            IsEditingProfile = true,
            EditingProfileId = null,
            ProfileNameInput = string.Empty,
            SelectedAuthor = defaultAuthor,
            SelectedCredentials = defaultCreds,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void StartEditProfile(GitProfile profile)
    {
        UpdateState(s => s with
        {
            IsEditingProfile = true,
            EditingProfileId = profile.Id,
            ProfileNameInput = profile.Name,
            SelectedAuthor = State.Authors.FirstOrDefault(a => a.Id == profile.AuthorId) ?? profile.Author,
            SelectedCredentials = State.CredentialsList.FirstOrDefault(c => c.Id == profile.CredentialsId) ?? profile.Credentials,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void CancelProfileEdit()
    {
        UpdateState(s => s with
        {
            IsEditingProfile = false,
            EditingProfileId = null,
            ProfileNameInput = string.Empty
        });
    }

    [ReactiveCommand]
    private async Task SaveProfile()
    {
        string trimmedName = State.ProfileNameInput.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            UpdateState(s => s with { ErrorMessage = "Profile name cannot be empty." });
            return;
        }

        if (State.SelectedAuthor == null)
        {
            UpdateState(s => s with { ErrorMessage = "Please select or create an author for this profile." });
            return;
        }

        if (State.SelectedCredentials == null)
        {
            UpdateState(s => s with { ErrorMessage = "Please select or create GitHub credentials for this profile." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            GitProfile profile = new()
            {
                Id = State.EditingProfileId ?? 0,
                Name = trimmedName,
                AuthorId = State.SelectedAuthor.Id,
                CredentialsId = State.SelectedCredentials.Id
            };

            await _profileRepository.SaveProfileAsync(profile);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingProfile = false,
                EditingProfileId = null,
                ProfileNameInput = string.Empty,
                SuccessMessage = "Profile saved successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task DeleteProfile(GitProfile profile)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _profileRepository.DeleteProfileAsync(profile.Id);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingProfile = false,
                EditingProfileId = null,
                SuccessMessage = "Profile deleted successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task SetAsActiveProfile(GitProfile profile)
    {
        if (string.IsNullOrWhiteSpace(State.RepositoryPath))
        {
            UpdateState(s => s with { ErrorMessage = "No repository is currently open." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _profileRepository.SetActiveProfileForRepositoryAsync(State.RepositoryPath, profile.Id);
            await LoadData();

            UpdateState(s => s with
            {
                SuccessMessage = $"Profile '{profile.Name}' set as active for current repository."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void StartCreateAuthor()
    {
        UpdateState(s => s with
        {
            IsEditingAuthor = true,
            EditingAuthorId = null,
            AuthorNameInput = string.Empty,
            AuthorEmailInput = string.Empty,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void StartEditAuthor(GitAuthor author)
    {
        UpdateState(s => s with
        {
            IsEditingAuthor = true,
            EditingAuthorId = author.Id,
            AuthorNameInput = author.Name,
            AuthorEmailInput = author.Email,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void CancelAuthorEdit()
    {
        UpdateState(s => s with
        {
            IsEditingAuthor = false,
            EditingAuthorId = null,
            AuthorNameInput = string.Empty,
            AuthorEmailInput = string.Empty
        });
    }

    [ReactiveCommand]
    private async Task SaveAuthor()
    {
        string name = State.AuthorNameInput.Trim();
        string email = State.AuthorEmailInput.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            UpdateState(s => s with { ErrorMessage = "Author name and email cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            GitAuthor author = new()
            {
                Id = State.EditingAuthorId ?? 0,
                Name = name,
                Email = email
            };

            GitAuthor saved = await _profileRepository.SaveAuthorAsync(author);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingAuthor = false,
                EditingAuthorId = null,
                AuthorNameInput = string.Empty,
                AuthorEmailInput = string.Empty,
                SelectedAuthor = saved,
                SuccessMessage = "Author saved successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task DeleteAuthor(GitAuthor author)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _profileRepository.DeleteAuthorAsync(author.Id);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingAuthor = false,
                EditingAuthorId = null,
                SuccessMessage = "Author deleted successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private void StartCreateCredentials()
    {
        UpdateState(s => s with
        {
            IsEditingCredentials = true,
            EditingCredentialsId = null,
            CredentialsUsernameInput = string.Empty,
            CredentialsAccessTokenInput = string.Empty,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void StartEditCredentials(GitProfileCredentials credentials)
    {
        UpdateState(s => s with
        {
            IsEditingCredentials = true,
            EditingCredentialsId = credentials.Id,
            CredentialsUsernameInput = credentials.Username,
            CredentialsAccessTokenInput = credentials.AccessToken,
            ErrorMessage = null,
            SuccessMessage = null
        });
    }

    [ReactiveCommand]
    private void CancelCredentialsEdit()
    {
        UpdateState(s => s with
        {
            IsEditingCredentials = false,
            EditingCredentialsId = null,
            CredentialsUsernameInput = string.Empty,
            CredentialsAccessTokenInput = string.Empty
        });
    }

    [ReactiveCommand]
    private async Task SaveCredentials()
    {
        string username = State.CredentialsUsernameInput.Trim();
        string token = State.CredentialsAccessTokenInput.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
        {
            UpdateState(s => s with { ErrorMessage = "GitHub username and access token cannot be empty." });
            return;
        }

        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            GitProfileCredentials creds = new()
            {
                Id = State.EditingCredentialsId ?? 0,
                Username = username,
                AccessToken = token
            };

            GitProfileCredentials saved = await _profileRepository.SaveCredentialsAsync(creds);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingCredentials = false,
                EditingCredentialsId = null,
                CredentialsUsernameInput = string.Empty,
                CredentialsAccessTokenInput = string.Empty,
                SelectedCredentials = saved,
                SuccessMessage = "Credentials saved successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [ReactiveCommand]
    private async Task DeleteCredentials(GitProfileCredentials credentials)
    {
        UpdateState(s => s with { IsLoading = true, ErrorMessage = null });

        try
        {
            await _profileRepository.DeleteCredentialsAsync(credentials.Id);
            await LoadData();

            UpdateState(s => s with
            {
                IsEditingCredentials = false,
                EditingCredentialsId = null,
                SuccessMessage = "Credentials deleted successfully."
            });
        }
        catch (Exception ex)
        {
            UpdateState(s => s with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
