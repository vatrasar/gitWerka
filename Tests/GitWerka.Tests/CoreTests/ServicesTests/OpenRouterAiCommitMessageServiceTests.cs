namespace GitWerka.Tests.CoreTests.ServicesTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Infrastructure.Services;
using LibGit2Sharp;
using Xunit;

public class OpenRouterAiCommitMessageServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public OpenRouterAiCommitMessageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "GitWerkaAiTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        InitializeTestGitRepository(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            DeleteDirectorySafely(_testDirectory);
        }
    }

    [Fact]
    public async Task GenerateCommitMessageAsync_WhenFirstAttemptReturns429AndSecondSucceeds_RetriesAndReturnsProposal()
    {
        Queue<HttpResponseMessage> responses = new();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        responses.Enqueue(CreateSuccessResponse("feat(auth): add login form"));

        List<TimeSpan> delayCalls = new();
        List<AiRetryAttempt> progressReports = new();

        Task MockDelay(TimeSpan delay, CancellationToken ct)
        {
            delayCalls.Add(delay);
            return Task.CompletedTask;
        }

        using HttpClient httpClient = new(new QueueHttpMessageHandler(responses));
        MockAiConfigRepository configRepo = new();

        OpenRouterAiCommitMessageService service = new(httpClient, configRepo, MockDelay);

        Progress<AiRetryAttempt> progress = new(p => progressReports.Add(p));

        string result = await service.GenerateCommitMessageAsync(_testDirectory, progress, CancellationToken.None);

        Assert.Equal("feat(auth): add login form", result);
        Assert.NotEmpty(delayCalls);
        Assert.Contains(progressReports, r => r.AttemptNumber == 1);
    }

    [Fact]
    public async Task GenerateCommitMessageAsync_When429IncludesRetryAfterHeader_UsesHeaderDelay()
    {
        HttpResponseMessage rateLimitResponse = new(HttpStatusCode.TooManyRequests);
        rateLimitResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(4));

        Queue<HttpResponseMessage> responses = new();
        responses.Enqueue(rateLimitResponse);
        responses.Enqueue(CreateSuccessResponse("fix(core): resolve null pointer"));

        List<TimeSpan> delayCalls = new();

        Task MockDelay(TimeSpan delay, CancellationToken ct)
        {
            delayCalls.Add(delay);
            return Task.CompletedTask;
        }

        using HttpClient httpClient = new(new QueueHttpMessageHandler(responses));
        MockAiConfigRepository configRepo = new();

        OpenRouterAiCommitMessageService service = new(httpClient, configRepo, MockDelay);

        string result = await service.GenerateCommitMessageAsync(_testDirectory, null, CancellationToken.None);

        Assert.Equal("fix(core): resolve null pointer", result);
        Assert.Equal(4, delayCalls.Count);
    }

    [Fact]
    public async Task GenerateCommitMessageAsync_When429RepeatedBeyondMaxRetries_Throws429Exception()
    {
        Queue<HttpResponseMessage> responses = new();
        for (int i = 0; i < 5; i++)
        {
            responses.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        }

        Task MockDelay(TimeSpan delay, CancellationToken ct) => Task.CompletedTask;

        using HttpClient httpClient = new(new QueueHttpMessageHandler(responses));
        MockAiConfigRepository configRepo = new();

        OpenRouterAiCommitMessageService service = new(httpClient, configRepo, MockDelay);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateCommitMessageAsync(_testDirectory, null, CancellationToken.None));

        Assert.Contains("429", ex.Message);
    }

    [Fact]
    public async Task GenerateCommitMessageAsync_When401Unauthorized_DoesNotRetry()
    {
        Queue<HttpResponseMessage> responses = new();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        int delayCallCount = 0;
        Task MockDelay(TimeSpan delay, CancellationToken ct)
        {
            delayCallCount++;
            return Task.CompletedTask;
        }

        using HttpClient httpClient = new(new QueueHttpMessageHandler(responses));
        MockAiConfigRepository configRepo = new();

        OpenRouterAiCommitMessageService service = new(httpClient, configRepo, MockDelay);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateCommitMessageAsync(_testDirectory, null, CancellationToken.None));

        Assert.Contains("401", ex.Message);
        Assert.Equal(0, delayCallCount);
    }

    [Fact]
    public async Task GenerateCommitMessageAsync_WhenCancelledDuringRetry_ThrowsOperationCanceledException()
    {
        Queue<HttpResponseMessage> responses = new();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using CancellationTokenSource cts = new();

        Task MockDelay(TimeSpan delay, CancellationToken ct)
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        using HttpClient httpClient = new(new QueueHttpMessageHandler(responses));
        MockAiConfigRepository configRepo = new();

        OpenRouterAiCommitMessageService service = new(httpClient, configRepo, MockDelay);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GenerateCommitMessageAsync(_testDirectory, null, cts.Token));
    }

    private static HttpResponseMessage CreateSuccessResponse(string proposal)
    {
        string json = $"{{\"choices\":[{{\"message\":{{\"content\":\"{proposal}\"}}}}]}}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static void InitializeTestGitRepository(string path)
    {
        Repository.Init(path);
        using Repository repo = new(path);

        string stagedFile = Path.Combine(path, "tracked.txt");
        File.WriteAllText(stagedFile, "new content");
        Commands.Stage(repo, "tracked.txt");
    }

    private static void DeleteDirectorySafely(string directoryPath)
    {
        try
        {
            foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(directoryPath, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clean up test directory {directoryPath}: {ex.Message}");
        }
    }

    private class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(Queue<HttpResponseMessage> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private class MockAiConfigRepository : IAiConfigurationRepository
    {
        public Task<AiConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiConfiguration
            {
                ApiKey = "sk-test-key",
                Model = "test-model",
                BaseUrl = "https://example.com/api/v1/chat/completions"
            });

        public Task SaveConfigurationAsync(AiConfiguration configuration, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string?> GetProjectDescriptionAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("Test project description");

        public Task SaveProjectDescriptionAsync(string repositoryPath, string? description, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
