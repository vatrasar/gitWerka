namespace GitWerka.Core.Domain.Services;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service contract for generating commit messages using artificial intelligence.
/// </summary>
public interface IAiCommitMessageService
{
    /// <summary>
    /// Maximum character limit for the commit subject (first line / commit title).
    /// </summary>
    public const int MaxCommitSubjectLength = 72;

    /// <summary>
    /// Truncates the commit subject (first line / commit title) to the maximum subject length limit, discarding any multi-line body.
    /// Usages: OpenRouterAiCommitMessageService, CommitLogViewModel.
    /// </summary>
    /// <param name="message">The raw commit message to truncate.</param>
    /// <returns>Single-line commit title trimmed to at most MaxCommitSubjectLength characters.</returns>
    public static string TruncateCommitSubject(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        string subjectLine = ExtractFirstLine(message);
        if (subjectLine.Length <= MaxCommitSubjectLength)
        {
            return subjectLine;
        }

        return subjectLine.Substring(0, MaxCommitSubjectLength);
    }

    private static string ExtractFirstLine(string message)
    {
        string trimmed = message.TrimStart('\r', '\n');
        int newlineIndex = trimmed.IndexOf('\n');
        if (newlineIndex < 0)
        {
            return trimmed.Trim();
        }

        return trimmed.Substring(0, newlineIndex).TrimEnd('\r').Trim();
    }

    /// <summary>
    /// Generates a suggested commit message based on staged git changes and configured context.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Suggested commit message text.</returns>
    Task<string> GenerateCommitMessageAsync(string repositoryPath, CancellationToken cancellationToken = default);
}
