namespace GitWerka.Core.Domain.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Service contract for Git repository operations.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Checks whether the specified file system directory is a valid Git repository.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the path contains a valid repository; otherwise false.</returns>
    bool IsValidRepository(string path);

    /// <summary>
    /// Finds the Git repository root directory for the given path.
    /// Usages: MainWindowViewModel, RepositoryManagerViewModel.
    /// </summary>
    /// <param name="path">The file system path to search from.</param>
    /// <returns>The root path of the Git repository if found; otherwise null.</returns>
    string? FindRepositoryRoot(string path);

    /// <summary>
    /// Initializes a new Git repository at the specified directory path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="directoryPath">Directory path where the repository will be created.</param>
    /// <param name="isBare">Whether to create a bare repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the initialized repository.</returns>
    Task<string> InitializeRepositoryAsync(string directoryPath, bool isBare = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones a remote Git repository to the target directory path.
    /// Usages: RepositoryManagerViewModel.
    /// </summary>
    /// <param name="sourceUrl">Remote repository URL.</param>
    /// <param name="targetDirectoryPath">Target local folder for cloning.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the cloned repository.</returns>
    Task<string> CloneRepositoryAsync(string sourceUrl, string targetDirectoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads summary information from an existing Git repository.
    /// Usages: RepositoryManagerViewModel, HostViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitRepositoryInfo model with summary data.</returns>
    Task<GitRepositoryInfo> GetRepositoryInfoAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads commits and computes visual graph lane indices.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="maxCount">Maximum number of commits to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of commit summary objects with graph topology.</returns>
    Task<IReadOnlyList<CommitSummary>> GetCommitLogAsync(string repositoryPath, int maxCount = 300, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves full commit details and changed files for a specific commit SHA.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Full commit SHA identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>CommitDetail model containing full metadata and changed files.</returns>
    Task<CommitDetail?> GetCommitDetailAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes diff lines for a specific file in a commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Commit SHA identifier.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of parsed diff lines.</returns>
    Task<IReadOnlyList<DiffLine>> GetFileDiffLinesAsync(string repositoryPath, string commitSha, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all staged and unstaged file modifications in the working tree.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of working tree file modifications.</returns>
    Task<IReadOnlyList<WorkingTreeChange>> GetWorkingTreeChangesAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an unstaged file in the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to stage.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task StageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unstages a staged file from the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to unstage.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task UnstageFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards uncommitted modifications of a file in the working tree.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path to revert.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DiscardFileChangesAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all local and remote branches in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of branch descriptions.</returns>
    Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks out a branch by friendly name.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">Branch name to checkout.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new commit or amends the previous commit from currently staged changes.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="message">Commit message.</param>
    /// <param name="authorName">Author name.</param>
    /// <param name="authorEmail">Author email.</param>
    /// <param name="amendPreviousCommit">Whether to amend the previous commit instead of creating a new commit.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The newly created or amended commit SHA.</returns>
    Task<string> CreateCommitAsync(string repositoryPath, string message, string authorName, string authorEmail, bool amendPreviousCommit = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new local branch pointing to a specific commit or HEAD.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">New branch name.</param>
    /// <param name="targetShaOrRef">Target commit SHA or reference.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task CreateBranchAsync(string repositoryPath, string branchName, string? targetShaOrRef = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a local or remote branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="branchName">Branch name to delete.</param>
    /// <param name="isRemote">Whether the branch is remote.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DeleteBranchAsync(string repositoryPath, string branchName, bool isRemote = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames an existing local branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="oldBranchName">Current branch name.</param>
    /// <param name="newBranchName">New target branch name.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task RenameBranchAsync(string repositoryPath, string oldBranchName, string newBranchName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tags in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of tag information.</returns>
    Task<IReadOnlyList<TagInfo>> GetTagsAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a lightweight or annotated tag on a commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="tagName">Tag name to create.</param>
    /// <param name="commitSha">Target commit SHA (defaults to HEAD if null).</param>
    /// <param name="message">Optional tag annotation message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task CreateTagAsync(string repositoryPath, string tagName, string? commitSha = null, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tag by name.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="tagName">Tag name to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DeleteTagAsync(string repositoryPath, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all stash entries in the repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of stash entries.</returns>
    Task<IReadOnlyList<StashInfo>> GetStashesAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves current working tree changes to a new stash entry.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="message">Stash descriptive message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task CreateStashAsync(string repositoryPath, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a stash entry without removing it from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task ApplyStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pops a stash entry, applying changes and removing it from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to pop.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task PopStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops a stash entry from the stash list.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="stashIndex">Index of the stash entry to drop.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task DropStashAsync(string repositoryPath, int stashIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves configured remote repositories.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of remotes.</returns>
    Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new remote repository configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Name of the remote.</param>
    /// <param name="url">URL of the remote repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task AddRemoteAsync(string repositoryPath, string remoteName, string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a configured remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Name of the remote to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task RemoveRemoteAsync(string repositoryPath, string remoteName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches changes from a remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name to fetch (or all remotes if null).</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task FetchAsync(string repositoryPath, string? remoteName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls changes from the tracked remote branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task PullAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes current branch commits to the remote repository.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Optional authentication credentials (HTTPS or SSH passphrase).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task PushAsync(string repositoryPath, string? remoteName = null, string? branchName = null, GitCredentials? credentials = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads user.name and user.email configuration.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitConfigInfo containing user identity settings.</returns>
    Task<GitConfigInfo> GetGitConfigAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user.name and user.email in local repository config.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="userName">Author name.</param>
    /// <param name="userEmail">Author email address.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SetGitConfigAsync(string repositoryPath, string userName, string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cherry-picks a specific commit onto the current HEAD.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">SHA of the commit to cherry-pick.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task CherryPickCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a revert commit that inverts the changes of a specified commit.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">SHA of the commit to revert.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task RevertCommitAsync(string repositoryPath, string commitSha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the current branch tip to a specific commit using Soft, Mixed, or Hard mode.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitSha">Target commit SHA.</param>
    /// <param name="resetType">Type of reset (Soft, Mixed, Hard).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task ResetBranchToCommitAsync(string repositoryPath, string commitSha, GitResetType resetType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the repository has an ongoing long-running operation (such as merge or rebase) and conflict status.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>RepositoryOperationStatus containing active operation state.</returns>
    Task<RepositoryOperationStatus> GetRepositoryOperationStatusAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges the specified branch into the current HEAD branch.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="sourceBranchName">Source branch to merge into current branch.</param>
    /// <param name="fastForwardMode">Fast-forward merge preference.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GitMergeResult containing merge status and conflict indicator.</returns>
    Task<GitMergeResult> MergeBranchAsync(string repositoryPath, string sourceBranchName, GitMergeFastForwardMode fastForwardMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts an ongoing merge operation and restores HEAD to pre-merge state.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task AbortMergeAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an ongoing merge after conflicts have been resolved and staged.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="commitMessage">Optional custom merge commit message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task ContinueMergeAsync(string repositoryPath, string? commitMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves commits between HEAD and the specified upstream commit as rebase steps for interactive editing.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="upstreamCommitSha">Base commit SHA onto which commits will be rebased.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Read-only list of rebase steps in chronological order (oldest to newest).</returns>
    Task<IReadOnlyList<RebaseStep>> GetInteractiveRebaseStepsAsync(string repositoryPath, string upstreamCommitSha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an interactive rebase sequence using the provided list of rebase steps.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="upstreamCommitSha">Base commit SHA onto which commits will be rebased.</param>
    /// <param name="steps">Reordered and configured rebase steps.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if rebase completed successfully, or false if conflicts occurred requiring user resolution.</returns>
    Task<bool> ExecuteInteractiveRebaseAsync(string repositoryPath, string upstreamCommitSha, IReadOnlyList<RebaseStep> steps, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts an in-progress rebase and restores the original branch tip.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task AbortRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues an in-progress interactive rebase after resolving current conflict.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if all remaining steps completed, or false if another conflict was encountered.</returns>
    Task<bool> ContinueRebaseAsync(string repositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the conflicted file data, Git metadata (local/remote SHAs and branches), and parsed conflict hunks.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ConflictFileModel containing metadata and parsed chunks.</returns>
    Task<ConflictFileModel> GetConflictFileAsync(string repositoryPath, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes resolved file content to disk and stages the file in the Git index, clearing its conflict state.
    /// Usages: ConflictResolverViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="filePath">Repository-relative file path.</param>
    /// <param name="resolvedContent">The full resolved text content to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task SaveConflictResolutionAsync(string repositoryPath, string filePath, string resolvedContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all currently conflicted files as resolved by staging them in the Git index.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task MarkAllConflictsResolvedAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
