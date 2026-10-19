namespace GitWerka.Infrastructure.Services;

using System;

internal sealed record ProposalAttemptOutcome(
    bool Success,
    string? Proposal,
    bool IsTransient,
    TimeSpan? RetryAfter,
    Exception? Exception)
{
    public static ProposalAttemptOutcome Successful(string proposal) =>
        new(true, proposal, false, null, null);

    public static ProposalAttemptOutcome TransientFailure(Exception exception, TimeSpan? retryAfter) =>
        new(false, null, true, retryAfter, exception);

    public static ProposalAttemptOutcome FatalFailure(Exception exception) =>
        new(false, null, false, null, exception);
}
