namespace GitWerka.Features.ProfileManagement.Resources {
    using System;
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ProfileManagementStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal ProfileManagementStrings() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GitWerka.Src.Features.ProfileManagement.Resources.ProfileManagementStrings", typeof(ProfileManagementStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string ScreenTitle => ResourceManager.GetString("ScreenTitle", resourceCulture) ?? "Profile Management";
        public static string ScreenSubtitle => ResourceManager.GetString("ScreenSubtitle", resourceCulture) ?? "Manage authors and GitHub credentials for repository commit signatures and push authentication";
        public static string BackTooltip => ResourceManager.GetString("BackTooltip", resourceCulture) ?? "Back to repository workspace";
        public static string ProfilesTab => ResourceManager.GetString("ProfilesTab", resourceCulture) ?? "Profiles";
        public static string AuthorsTab => ResourceManager.GetString("AuthorsTab", resourceCulture) ?? "Authors";
        public static string CredentialsTab => ResourceManager.GetString("CredentialsTab", resourceCulture) ?? "Credentials";
        public static string ProfilesSectionTitle => ResourceManager.GetString("ProfilesSectionTitle", resourceCulture) ?? "Configured Profiles";
        public static string NewProfileButton => ResourceManager.GetString("NewProfileButton", resourceCulture) ?? "New Profile";
        public static string ProfileNameLabel => ResourceManager.GetString("ProfileNameLabel", resourceCulture) ?? "Profile Name";
        public static string ProfileNamePlaceholder => ResourceManager.GetString("ProfileNamePlaceholder", resourceCulture) ?? "e.g. Work GitHub, Personal OpenSource";
        public static string AuthorSelectLabel => ResourceManager.GetString("AuthorSelectLabel", resourceCulture) ?? "Commit Author (Signature)";
        public static string CredentialsSelectLabel => ResourceManager.GetString("CredentialsSelectLabel", resourceCulture) ?? "GitHub Credentials (Push Auth)";
        public static string SelectAuthorPlaceholder => ResourceManager.GetString("SelectAuthorPlaceholder", resourceCulture) ?? "Select commit author...";
        public static string SelectCredentialsPlaceholder => ResourceManager.GetString("SelectCredentialsPlaceholder", resourceCulture) ?? "Select credentials...";
        public static string QuickNewAuthorButton => ResourceManager.GetString("QuickNewAuthorButton", resourceCulture) ?? "+ New Author";
        public static string QuickNewCredentialsButton => ResourceManager.GetString("QuickNewCredentialsButton", resourceCulture) ?? "+ New Credentials";
        public static string SaveProfileButton => ResourceManager.GetString("SaveProfileButton", resourceCulture) ?? "Save Profile";
        public static string DeleteProfileButton => ResourceManager.GetString("DeleteProfileButton", resourceCulture) ?? "Delete Profile";
        public static string EditButton => ResourceManager.GetString("EditButton", resourceCulture) ?? "Edit";
        public static string DeleteButton => ResourceManager.GetString("DeleteButton", resourceCulture) ?? "Delete";
        public static string SetActiveForRepoButton => ResourceManager.GetString("SetActiveForRepoButton", resourceCulture) ?? "Use for Current Repository";
        public static string ActiveForCurrentRepoBadge => ResourceManager.GetString("ActiveForCurrentRepoBadge", resourceCulture) ?? "Active for this repo";
        public static string NoProfilesMessage => ResourceManager.GetString("NoProfilesMessage", resourceCulture) ?? "No profiles created yet. Click 'New Profile' to create one by pairing an author and GitHub credentials.";
        public static string AuthorsSectionTitle => ResourceManager.GetString("AuthorsSectionTitle", resourceCulture) ?? "Authors (Commit Signatures)";
        public static string NewAuthorButton => ResourceManager.GetString("NewAuthorButton", resourceCulture) ?? "New Author";
        public static string AuthorNameLabel => ResourceManager.GetString("AuthorNameLabel", resourceCulture) ?? "Full Name";
        public static string AuthorNamePlaceholder => ResourceManager.GetString("AuthorNamePlaceholder", resourceCulture) ?? "e.g. Jane Doe";
        public static string AuthorEmailLabel => ResourceManager.GetString("AuthorEmailLabel", resourceCulture) ?? "Email Address";
        public static string AuthorEmailPlaceholder => ResourceManager.GetString("AuthorEmailPlaceholder", resourceCulture) ?? "e.g. jane.doe@example.com";
        public static string SaveAuthorButton => ResourceManager.GetString("SaveAuthorButton", resourceCulture) ?? "Save Author";
        public static string NoAuthorsMessage => ResourceManager.GetString("NoAuthorsMessage", resourceCulture) ?? "No authors saved yet. Create authors to reuse their signature across multiple profiles.";
        public static string CredentialsSectionTitle => ResourceManager.GetString("CredentialsSectionTitle", resourceCulture) ?? "GitHub Credentials";
        public static string NewCredentialsButton => ResourceManager.GetString("NewCredentialsButton", resourceCulture) ?? "New Credentials";
        public static string CredentialsUsernameLabel => ResourceManager.GetString("CredentialsUsernameLabel", resourceCulture) ?? "GitHub Username";
        public static string CredentialsUsernamePlaceholder => ResourceManager.GetString("CredentialsUsernamePlaceholder", resourceCulture) ?? "e.g. octocat";
        public static string CredentialsTokenLabel => ResourceManager.GetString("CredentialsTokenLabel", resourceCulture) ?? "Personal Access Token";
        public static string CredentialsTokenPlaceholder => ResourceManager.GetString("CredentialsTokenPlaceholder", resourceCulture) ?? "e.g. ghp_xxxxxxxxxxxx";
        public static string SaveCredentialsButton => ResourceManager.GetString("SaveCredentialsButton", resourceCulture) ?? "Save Credentials";
        public static string NoCredentialsMessage => ResourceManager.GetString("NoCredentialsMessage", resourceCulture) ?? "No credentials saved yet. Create credentials to reuse your GitHub tokens across multiple profiles.";
        public static string ProfileEditorHeader => ResourceManager.GetString("ProfileEditorHeader", resourceCulture) ?? "Profile Details";
        public static string AuthorEditorHeader => ResourceManager.GetString("AuthorEditorHeader", resourceCulture) ?? "Author Details";
        public static string CredentialsEditorHeader => ResourceManager.GetString("CredentialsEditorHeader", resourceCulture) ?? "Credentials Details";
        public static string CancelEditButton => ResourceManager.GetString("CancelEditButton", resourceCulture) ?? "Cancel";
    }
}
