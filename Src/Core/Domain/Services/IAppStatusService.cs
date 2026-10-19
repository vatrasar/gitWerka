namespace GitWerka.Core.Domain.Services;

using System;
using GitWerka.Core.Domain.Models;

/// <summary>
/// Service managing the application-wide status state and notification updates displayed in the Shell footer.
/// </summary>
public interface IAppStatusService
{
    /// <summary>
    /// Gets the observable stream of status changes.
    /// Usages: HostViewModel.
    /// </summary>
    IObservable<AppStatus> Status { get; }

    /// <summary>
    /// Gets the current status snapshot.
    /// Usages: HostViewModel.
    /// </summary>
    AppStatus CurrentStatus { get; }

    /// <summary>
    /// Updates the global status to in-progress with a spinning indicator.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Status message to display while operation is running.</param>
    void SetInProgress(string message);

    /// <summary>
    /// Updates the global status to success and optionally resets to ready after a timeout.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Success message to display.</param>
    /// <param name="autoResetDelay">Optional custom delay before returning to ready state.</param>
    void SetSuccess(string message, TimeSpan? autoResetDelay = null);

    /// <summary>
    /// Updates the global status to error and optionally resets to ready after a timeout.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Error message to display.</param>
    /// <param name="autoResetDelay">Optional custom delay before returning to ready state.</param>
    void SetError(string message, TimeSpan? autoResetDelay = null);

    /// <summary>
    /// Resets the global status to ready state immediately.
    /// Usages: HostViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="message">Optional ready message text, defaults to 'Ready'.</param>
    void SetReady(string? message = null);
}
