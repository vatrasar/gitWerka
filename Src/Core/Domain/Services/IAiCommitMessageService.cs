namespace GitWerka.Core.Domain.Services;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service contract for generating commit messages using artificial intelligence.
/// </summary>
public interface IAiCommitMessageService
{
    /// <summary>
    /// Generates a suggested commit message based on staged git changes and configured context.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Suggested commit message text.</returns>
    Task<string> GenerateCommitMessageAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
