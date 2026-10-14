namespace GitWerka.Core.Domain.Models;

/// <summary>
/// Domain model representing AI integration configuration settings.
/// </summary>
public record AiConfiguration
{
    public string? ApiKey { get; init; }

    public string Model { get; init; } = "google/gemini-2.5-flash";

    public string CommitStyle { get; init; } = "Conventional Commits: <type>(<scope>): <subject>";

    public int TokenLimit { get; init; } = 4000;

    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1/chat/completions";
}
