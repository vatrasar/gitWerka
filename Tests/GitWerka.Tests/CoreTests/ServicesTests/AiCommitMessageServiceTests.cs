namespace GitWerka.Tests.CoreTests.ServicesTests;

using GitWerka.Core.Domain.Services;
using Xunit;

public class AiCommitMessageServiceTests
{
    [Fact]
    public void TruncateCommitSubject_WhenNullOrEmpty_ReturnsOriginal()
    {
        Assert.Null(IAiCommitMessageService.TruncateCommitSubject(null!));
        Assert.Equal(string.Empty, IAiCommitMessageService.TruncateCommitSubject(string.Empty));
    }

    [Fact]
    public void TruncateCommitSubject_WhenWithinLimit_ReturnsOriginal()
    {
        string message = "feat(auth): add user authentication flow";
        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        Assert.Equal(message, result);
    }

    [Fact]
    public void TruncateCommitSubject_WhenExactlyLimitLength_ReturnsOriginal()
    {
        string message = new('B', IAiCommitMessageService.MaxCommitSubjectLength);
        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        Assert.Equal(message, result);
        Assert.Equal(IAiCommitMessageService.MaxCommitSubjectLength, result.Length);
    }

    [Fact]
    public void TruncateCommitSubject_WhenSingleLineExceedsLimit_TruncatesToLimit()
    {
        string message = new('C', 90);
        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        Assert.Equal(IAiCommitMessageService.MaxCommitSubjectLength, result.Length);
        Assert.Equal(new string('C', IAiCommitMessageService.MaxCommitSubjectLength), result);
    }

    [Fact]
    public void TruncateCommitSubject_WhenMultiLineWithLfAndLongSubject_TruncatesSubjectAndDiscardsBody()
    {
        string longSubject = new('D', 100);
        string body = "\n\nDetailed body line 1\nDetailed body line 2";
        string message = longSubject + body;

        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        string expected = new string('D', IAiCommitMessageService.MaxCommitSubjectLength);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TruncateCommitSubject_WhenMultiLineWithCrLfAndLongSubject_TruncatesSubjectAndDiscardsBody()
    {
        string longSubject = new('E', 85);
        string body = "\r\n\r\nDetailed body with CRLF line endings";
        string message = longSubject + body;

        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        string expected = new string('E', IAiCommitMessageService.MaxCommitSubjectLength);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TruncateCommitSubject_WhenMultiLineWithShortSubject_ReturnsOnlySubjectLine()
    {
        string message = "fix: resolve memory leak\n\nExplanation of why the leak occurred and how it was fixed.";
        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        Assert.Equal("fix: resolve memory leak", result);
    }

    [Fact]
    public void TruncateCommitSubject_WhenUserExample_DiscardsBodyAndReturnsOnlyTitle()
    {
        string message = "docs(readme): add first chapter content\n\nAdded a new section titled \"rozdział pierwszy\" with introductory text to the README file. This includes a placeholder sentence marking the beginning of the first chapter in the document";
        string result = IAiCommitMessageService.TruncateCommitSubject(message);

        Assert.Equal("docs(readme): add first chapter content", result);
    }
}
