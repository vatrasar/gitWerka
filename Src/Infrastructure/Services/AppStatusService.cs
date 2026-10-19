namespace GitWerka.Infrastructure.Services;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Core.Domain.Services;
using ReactiveUI;

/// <summary>
/// Implementation of IAppStatusService providing reactive global status notifications with automatic reset timers.
/// </summary>
public class AppStatusService : IAppStatusService, IDisposable
{
    private static readonly TimeSpan DefaultSuccessResetDelay = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan DefaultErrorResetDelay = TimeSpan.FromSeconds(6);
    private const string DefaultReadyMessage = "Ready";

    private readonly BehaviorSubject<AppStatus> _statusSubject;
    private readonly IScheduler _scheduler;
    private readonly object _syncLock = new();
    private IDisposable? _resetSubscription;
    private bool _isDisposed;

    public IObservable<AppStatus> Status => _statusSubject.AsObservable();

    public AppStatus CurrentStatus => _statusSubject.Value;

    public AppStatusService(IScheduler? scheduler = null)
    {
        _scheduler = scheduler ?? RxApp.MainThreadScheduler;
        _statusSubject = new BehaviorSubject<AppStatus>(new AppStatus(DefaultReadyMessage, AppStatusSeverity.Ready));
    }

    /// <summary>
    /// Updates the global status to in-progress with a spinning indicator.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Status message to display while operation is running.</param>
    public void SetInProgress(string message)
    {
        lock (_syncLock)
        {
            CancelActiveReset();
            _statusSubject.OnNext(new AppStatus(message, AppStatusSeverity.InProgress));
        }
    }

    /// <summary>
    /// Updates the global status to success and optionally resets to ready after a timeout.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Success message to display.</param>
    /// <param name="autoResetDelay">Optional custom delay before returning to ready state.</param>
    public void SetSuccess(string message, TimeSpan? autoResetDelay = null)
    {
        lock (_syncLock)
        {
            CancelActiveReset();
            _statusSubject.OnNext(new AppStatus(message, AppStatusSeverity.Success));
            ScheduleAutoReset(autoResetDelay ?? DefaultSuccessResetDelay);
        }
    }

    /// <summary>
    /// Updates the global status to error and optionally resets to ready after a timeout.
    /// Usages: CommitLogViewModel.
    /// </summary>
    /// <param name="message">Error message to display.</param>
    /// <param name="autoResetDelay">Optional custom delay before returning to ready state.</param>
    public void SetError(string message, TimeSpan? autoResetDelay = null)
    {
        lock (_syncLock)
        {
            CancelActiveReset();
            _statusSubject.OnNext(new AppStatus(message, AppStatusSeverity.Error));
            ScheduleAutoReset(autoResetDelay ?? DefaultErrorResetDelay);
        }
    }

    /// <summary>
    /// Resets the global status to ready state immediately.
    /// Usages: HostViewModel, CommitLogViewModel.
    /// </summary>
    /// <param name="message">Optional ready message text, defaults to 'Ready'.</param>
    public void SetReady(string? message = null)
    {
        lock (_syncLock)
        {
            CancelActiveReset();
            _statusSubject.OnNext(new AppStatus(message ?? DefaultReadyMessage, AppStatusSeverity.Ready));
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            CancelActiveReset();
            _statusSubject.Dispose();
        }
    }

    private void CancelActiveReset()
    {
        _resetSubscription?.Dispose();
        _resetSubscription = null;
    }

    private void ScheduleAutoReset(TimeSpan delay)
    {
        _resetSubscription = Observable.Timer(delay, _scheduler)
            .Subscribe(_ =>
            {
                lock (_syncLock)
                {
                    _statusSubject.OnNext(new AppStatus(DefaultReadyMessage, AppStatusSeverity.Ready));
                    _resetSubscription = null;
                }
            });
    }
}
