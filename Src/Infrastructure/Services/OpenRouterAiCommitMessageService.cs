namespace GitWerka.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.RepositoryContracts;
using GitWerka.Core.Domain.Services;
using LibGit2Sharp;

/// <summary>
/// Service implementation for generating commit messages via OpenRouter and OpenAI-compatible APIs.
/// </summary>
public class OpenRouterAiCommitMessageService : IAiCommitMessageService
{
    private readonly HttpClient _httpClient;
    private readonly IAiConfigurationRepository _configurationRepository;

    private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".pdf",
        ".zip", ".tar", ".gz", ".7z", ".dll", ".exe", ".so", ".dylib", ".wasm",
        ".min.js", ".min.css", ".map"
    };

    private static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "Cargo.lock",
        "packages.lock.json", "composer.lock", "Gemfile.lock", "poetry.lock"
    };

    public OpenRouterAiCommitMessageService(
        HttpClient httpClient,
        IAiConfigurationRepository configurationRepository)
    {
        _httpClient = httpClient;
        _configurationRepository = configurationRepository;
    }

    /// <summary>
    /// Generates a suggested commit message based on staged git changes and configured context.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="repositoryPath">Directory path of the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Suggested commit message text.</returns>
    public async Task<string> GenerateCommitMessageAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AiConfiguration configuration = await _configurationRepository.GetConfigurationAsync(cancellationToken);
        ValidateApiKey(configuration.ApiKey);

        string? projectDescription = await _configurationRepository.GetProjectDescriptionAsync(repositoryPath, cancellationToken);

        string prompt = await Task.Run(() => BuildCommitPrompt(repositoryPath, configuration, projectDescription), cancellationToken);

        return await RequestCommitProposalAsync(configuration, prompt, cancellationToken);
    }

    private static void ValidateApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured. Please open Settings to configure your API key.");
        }
    }

    private static string BuildCommitPrompt(string repositoryPath, AiConfiguration configuration, string? projectDescription)
    {
        using Repository repository = new(repositoryPath);

        Tree? headTree = repository.Head.Tip?.Tree;
        Patch patch = repository.Diff.Compare<Patch>(headTree, DiffTargets.Index);

        if (!patch.Any())
        {
            throw new InvalidOperationException("No staged changes found. Stage files before generating a commit message.");
        }

        string branchName = repository.Head.FriendlyName;
        IReadOnlyList<string> recentCommits = GetRecentCommitSummaries(repository);

        string fileStats = BuildFileStatsSummary(patch);
        string stagedDiff = BuildPrioritizedDiff(patch, configuration.TokenLimit);

        return FormatUserPrompt(branchName, projectDescription, configuration.CommitStyle, recentCommits, fileStats, stagedDiff);
    }

    private static IReadOnlyList<string> GetRecentCommitSummaries(Repository repository)
    {
        return repository.Commits
            .Take(5)
            .Select(c => c.MessageShort)
            .ToList();
    }

    private static string BuildFileStatsSummary(Patch patch)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Staged files ({patch.Count()}):");

        foreach (PatchEntryChanges entry in patch)
        {
            builder.AppendLine($"- {entry.Path} (+{entry.LinesAdded}, -{entry.LinesDeleted}) [{entry.Status}]");
        }

        return builder.ToString();
    }

    private static string BuildPrioritizedDiff(Patch patch, int tokenLimit)
    {
        int charBudget = Math.Max(2000, tokenLimit * 3);
        StringBuilder diffBuilder = new();

        List<PatchEntryChanges> sortedEntries = patch
            .OrderBy(GetEntryPriority)
            .ToList();

        foreach (PatchEntryChanges entry in sortedEntries)
        {
            if (ShouldSkipDiffContent(entry))
            {
                continue;
            }

            if (diffBuilder.Length >= charBudget)
            {
                diffBuilder.AppendLine("\n[Remaining diff content truncated to fit token budget]");
                break;
            }

            AppendFileDiffWithBudget(diffBuilder, entry, charBudget - diffBuilder.Length);
        }

        return diffBuilder.ToString();
    }

    private static int GetEntryPriority(PatchEntryChanges entry)
    {
        string path = entry.Path;
        if (path.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }

    private static bool ShouldSkipDiffContent(PatchEntryChanges entry)
    {
        if (entry.IsBinaryComparison)
        {
            return true;
        }

        string fileName = Path.GetFileName(entry.Path);
        if (ExcludedFileNames.Contains(fileName))
        {
            return true;
        }

        string extension = Path.GetExtension(entry.Path);
        if (ExcludedExtensions.Contains(extension))
        {
            return true;
        }

        if (entry.Path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static void AppendFileDiffWithBudget(StringBuilder builder, PatchEntryChanges entry, int remainingBudget)
    {
        string filePatch = entry.Patch;
        if (string.IsNullOrWhiteSpace(filePatch))
        {
            return;
        }

        const int maxCharsPerFile = 2500;
        int allowedChars = Math.Min(maxCharsPerFile, remainingBudget);

        builder.AppendLine($"\n--- {entry.Path} ---");

        if (filePatch.Length <= allowedChars)
        {
            builder.Append(filePatch);
        }
        else
        {
            builder.Append(filePatch.AsSpan(0, allowedChars));
            builder.AppendLine("\n[File diff truncated]");
        }
    }

    private static string FormatUserPrompt(
        string branchName,
        string? projectDescription,
        string commitStyle,
        IReadOnlyList<string> recentCommits,
        string fileStats,
        string stagedDiff)
    {
        StringBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(projectDescription))
        {
            builder.AppendLine("Project context:");
            builder.AppendLine(projectDescription);
            builder.AppendLine();
        }

        builder.AppendLine($"Current branch: {branchName}");
        builder.AppendLine($"Required commit style: {commitStyle}");
        builder.AppendLine("Length constraint: The first line (commit title/subject) MUST NOT exceed 72 characters.");
        builder.AppendLine();

        if (recentCommits.Count > 0)
        {
            builder.AppendLine("Recent repository commits (for style and convention reference):");
            foreach (string commit in recentCommits)
            {
                builder.AppendLine($"- {commit}");
            }
            builder.AppendLine();
        }

        builder.AppendLine(fileStats);
        builder.AppendLine();
        builder.AppendLine("Staged Diff:");
        builder.AppendLine(stagedDiff);

        return builder.ToString();
    }

    private async Task<string> RequestCommitProposalAsync(
        AiConfiguration configuration,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateHttpRequest(configuration, userPrompt);
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Connection error: Unable to connect to OpenRouter endpoint. Please check your internet connection and endpoint URL.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Connection timed out waiting for OpenRouter response. Please check your network or try again.", ex);
        }

        using (response)
        {
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildApiErrorException(response.StatusCode, responseContent);
            }

            return ParseResponseContent(responseContent);
        }
    }

    private static InvalidOperationException BuildApiErrorException(System.Net.HttpStatusCode statusCode, string responseContent)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => new InvalidOperationException("Authentication failed (401): Invalid or missing OpenRouter API key. Please check your API key in Settings."),
            System.Net.HttpStatusCode.Forbidden => new InvalidOperationException("Access forbidden (403): Your OpenRouter account or key does not have permission for the selected model."),
            System.Net.HttpStatusCode.NotFound => new InvalidOperationException("Endpoint or model not found (404): Please check your model identifier and endpoint URL in Settings."),
            System.Net.HttpStatusCode.TooManyRequests => new InvalidOperationException("Rate limit or quota exceeded (429): OpenRouter credits or rate limits exceeded."),
            _ => new InvalidOperationException($"OpenRouter API error ({statusCode}): {responseContent}")
        };
    }

    private static HttpRequestMessage CreateHttpRequest(AiConfiguration configuration, string userPrompt)
    {
        HttpRequestMessage request = new(HttpMethod.Post, configuration.BaseUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
        request.Headers.Add("HTTP-Referer", "https://github.com/JetpackDuba/Gitnuro");
        request.Headers.Add("X-Title", "GitWerka");

        var payload = new
        {
            model = configuration.Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an expert Git commit message generator. Output ONLY the raw final commit message. Do NOT output any reasoning, chain of thought, analysis, or conversational commentary. Follow the requested style strictly with no markdown formatting, code fences, quotes, or explanations. CRITICAL LIMIT: The first line (commit title/subject) must be at most 72 characters long (recommended 50 characters or fewer)."
                },
                new
                {
                    role = "user",
                    content = userPrompt
                }
            },
            max_tokens = 10000,
            temperature = 0.2
        };

        string json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return request;
    }

    private static string ParseResponseContent(string jsonResponse)
    {
        JsonNode? rootNode = JsonNode.Parse(jsonResponse);
        string? content = rootNode?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Received empty response from the AI model.");
        }

        return CleanCommitProposal(content);
    }

    private static string CleanCommitProposal(string rawProposal)
    {
        string cleaned = StripThinkingBlock(rawProposal.Trim());

        if (cleaned.StartsWith("```") && cleaned.EndsWith("```"))
        {
            int firstNewline = cleaned.IndexOf('\n');
            int lastBacktick = cleaned.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewline >= 0 && lastBacktick > firstNewline)
            {
                cleaned = cleaned.Substring(firstNewline + 1, lastBacktick - firstNewline - 1).Trim();
            }
        }

        if (cleaned.StartsWith('"') && cleaned.EndsWith('"') && cleaned.Length >= 2)
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
        }

        return cleaned;
    }

    private static string StripThinkingBlock(string text)
    {
        int thinkStart = text.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        if (thinkStart < 0)
        {
            return text;
        }

        int thinkEnd = text.IndexOf("</think>", thinkStart, StringComparison.OrdinalIgnoreCase);
        if (thinkEnd >= 0)
        {
            return (text.Substring(0, thinkStart) + text.Substring(thinkEnd + 8)).Trim();
        }

        return text;
    }
}
