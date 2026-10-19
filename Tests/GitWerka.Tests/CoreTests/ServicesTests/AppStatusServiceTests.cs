namespace GitWerka.Tests.CoreTests.ServicesTests;

using System;
using System.Reactive.Concurrency;
using GitWerka.Core.Domain.Enums;
using GitWerka.Core.Domain.Models;
using GitWerka.Infrastructure.Services;
using Xunit;

public class AppStatusServiceTests
{
    [Fact]
    public void InitialStatus_ShouldBeReady()
    {
        using AppStatusService service = new();

        Assert.Equal("Ready", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Ready, service.CurrentStatus.Severity);
    }

    [Fact]
    public void SetInProgress_ShouldUpdateStatusToInProgress()
    {
        using AppStatusService service = new();

        service.SetInProgress("Pushing changes...");

        Assert.Equal("Pushing changes...", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.InProgress, service.CurrentStatus.Severity);
    }

    [Fact]
    public void SetSuccess_ShouldUpdateStatusAndResetToReadyAfterTimeout()
    {
        HistoricalScheduler scheduler = new();
        using AppStatusService service = new(scheduler);

        service.SetSuccess("Push completed successfully.");

        Assert.Equal("Push completed successfully.", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Success, service.CurrentStatus.Severity);

        scheduler.AdvanceBy(TimeSpan.FromSeconds(3));
        Assert.Equal("Push completed successfully.", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Success, service.CurrentStatus.Severity);

        scheduler.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal("Ready", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Ready, service.CurrentStatus.Severity);
    }

    [Fact]
    public void SetError_ShouldUpdateStatusAndResetToReadyAfterTimeout()
    {
        HistoricalScheduler scheduler = new();
        using AppStatusService service = new(scheduler);

        service.SetError("Push failed: network error");

        Assert.Equal("Push failed: network error", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Error, service.CurrentStatus.Severity);

        scheduler.AdvanceBy(TimeSpan.FromSeconds(5));
        Assert.Equal("Push failed: network error", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Error, service.CurrentStatus.Severity);

        scheduler.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal("Ready", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Ready, service.CurrentStatus.Severity);
    }

    [Fact]
    public void SetInProgress_ShouldCancelPendingResetTimer()
    {
        HistoricalScheduler scheduler = new();
        using AppStatusService service = new(scheduler);

        service.SetSuccess("Commit created successfully.");
        scheduler.AdvanceBy(TimeSpan.FromSeconds(2));

        service.SetInProgress("Pushing changes...");
        scheduler.AdvanceBy(TimeSpan.FromSeconds(10));

        Assert.Equal("Pushing changes...", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.InProgress, service.CurrentStatus.Severity);
    }

    [Fact]
    public void SetReady_ShouldResetStatusImmediately()
    {
        HistoricalScheduler scheduler = new();
        using AppStatusService service = new(scheduler);

        service.SetError("Some error");
        service.SetReady();

        Assert.Equal("Ready", service.CurrentStatus.Message);
        Assert.Equal(AppStatusSeverity.Ready, service.CurrentStatus.Severity);
    }
}
