namespace GitWerka.Tests.FeaturesTests.CommitLogTests;

using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using GitWerka.Core.Domain.Models;
using GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.CommitTitlePanel;
using Xunit;

public class CommitTitlePanelTests
{
    private class DummyControl : Control
    {
        private readonly Size _desired;

        public DummyControl(double width, double height)
        {
            _desired = new Size(width, height);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return _desired;
        }
    }

    [Fact]
    public void CommitSummary_HasBadges_TrueWhenBranchNamesExist()
    {
        CommitSummary commit = new()
        {
            BranchNames = ImmutableList.Create("master")
        };

        Assert.True(commit.HasBranches);
        Assert.False(commit.HasTags);
        Assert.True(commit.HasBadges);
    }

    [Fact]
    public void CommitSummary_HasBadges_TrueWhenTagNamesExist()
    {
        CommitSummary commit = new()
        {
            TagNames = ImmutableList.Create("v1.0.0")
        };

        Assert.False(commit.HasBranches);
        Assert.True(commit.HasTags);
        Assert.True(commit.HasBadges);
    }

    [Fact]
    public void CommitSummary_HasBadges_TrueWhenIsWip()
    {
        CommitSummary commit = new()
        {
            IsWip = true
        };

        Assert.True(commit.HasBadges);
    }

    [Fact]
    public void CommitSummary_HasBadges_FalseWhenEmpty()
    {
        CommitSummary commit = new();

        Assert.False(commit.HasBranches);
        Assert.False(commit.HasTags);
        Assert.False(commit.HasBadges);
    }

    [Fact]
    public void CommitTitlePanel_Empty_MeasuresZero()
    {
        CommitTitlePanel panel = new();
        panel.Measure(new Size(500, 500));

        Assert.Equal(0, panel.DesiredSize.Width);
        Assert.Equal(0, panel.DesiredSize.Height);
    }

    [Fact]
    public void CommitTitlePanel_NoBadges_MeasuresMessageOnly()
    {
        CommitTitlePanel panel = new();
        DummyControl badges = new(0, 0) { IsVisible = false };
        DummyControl message = new(200, 20);

        panel.Children.Add(badges);
        panel.Children.Add(message);

        panel.Measure(new Size(500, 500));

        Assert.Equal(200, panel.DesiredSize.Width);
        Assert.Equal(20, panel.DesiredSize.Height);
    }

    [Fact]
    public void CommitTitlePanel_FitsOnSingleLine_WhenWidthIsSufficient()
    {
        CommitTitlePanel panel = new()
        {
            HorizontalSpacing = 10
        };

        DummyControl badges = new(100, 20);
        DummyControl message = new(200, 16);

        panel.Children.Add(badges);
        panel.Children.Add(message);

        panel.Measure(new Size(400, 500));

        Assert.Equal(310, panel.DesiredSize.Width);
        Assert.Equal(20, panel.DesiredSize.Height);

        panel.Arrange(new Rect(0, 0, 400, 20));

        Assert.Equal(0, badges.Bounds.X);
        Assert.Equal(100, badges.Bounds.Width);
        Assert.Equal(110, message.Bounds.X);
    }

    [Fact]
    public void CommitTitlePanel_StacksMultiLine_WhenWidthIsConstrained()
    {
        CommitTitlePanel panel = new()
        {
            HorizontalSpacing = 10,
            VerticalSpacing = 4
        };

        DummyControl badges = new(150, 20);
        DummyControl message = new(200, 16);

        panel.Children.Add(badges);
        panel.Children.Add(message);

        panel.Measure(new Size(300, 500));

        Assert.Equal(200, panel.DesiredSize.Width);
        Assert.Equal(40, panel.DesiredSize.Height);

        panel.Arrange(new Rect(0, 0, 300, 40));

        Assert.Equal(0, badges.Bounds.X);
        Assert.Equal(0, badges.Bounds.Y);
        Assert.Equal(0, message.Bounds.X);
        Assert.Equal(24, message.Bounds.Y);
    }
}
