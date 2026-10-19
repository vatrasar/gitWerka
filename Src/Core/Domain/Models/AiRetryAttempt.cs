namespace GitWerka.Core.Domain.Models;

using System;

public record AiRetryAttempt(
    int AttemptNumber,
    int MaxRetries,
    TimeSpan Delay,
    bool IsWaiting,
    string? Reason = null
);
