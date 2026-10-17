/// <summary>
/// Component: CommitTitlePanel
/// Purpose: Custom responsive layout panel that places badges and commit title on a single line when space permits,
/// or stacks badges on top and wraps the title below when space is constrained.
/// Usage:
/// - Child 0: Badges container (WrapPanel containing branch/tag/WIP badges).
/// - Child 1: Commit message TextBlock.
/// Key UI Elements:
/// - Badges container, message TextBlock.
/// Used In:
/// - CommitLogView.
/// </summary>
namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.CommitTitlePanel;

using System;
using Avalonia;
using Avalonia.Controls;

public class CommitTitlePanel : Panel
{
    public static readonly StyledProperty<double> HorizontalSpacingProperty =
        AvaloniaProperty.Register<CommitTitlePanel, double>(nameof(HorizontalSpacing), 6.0);

    public static readonly StyledProperty<double> VerticalSpacingProperty =
        AvaloniaProperty.Register<CommitTitlePanel, double>(nameof(VerticalSpacing), 2.0);

    private bool _fitsOnSingleLine;

    public double HorizontalSpacing
    {
        get => GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    static CommitTitlePanel()
    {
        AffectsMeasure<CommitTitlePanel>(HorizontalSpacingProperty, VerticalSpacingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return new Size(0, 0);
        }

        if (count == 1)
        {
            Children[0].Measure(availableSize);
            _fitsOnSingleLine = true;
            return Children[0].DesiredSize;
        }

        Control badgesControl = Children[0];
        Control messageControl = Children[1];

        bool hasBadges = MeasureBadges(badgesControl, availableSize, out Size badgesSize);
        if (!hasBadges)
        {
            return MeasureMessageOnly(messageControl, availableSize);
        }

        return MeasureBadgesAndMessage(badgesControl, messageControl, badgesSize, availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return finalSize;
        }

        if (count == 1)
        {
            Children[0].Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        Control badgesControl = Children[0];
        Control messageControl = Children[1];

        bool hasBadges = badgesControl.IsVisible &&
                         badgesControl.DesiredSize.Width > 0.001 &&
                         badgesControl.DesiredSize.Height > 0.001;

        if (!hasBadges)
        {
            badgesControl.Arrange(new Rect(0, 0, 0, 0));
            messageControl.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        double requiredWidth = badgesControl.DesiredSize.Width + HorizontalSpacing + messageControl.DesiredSize.Width;
        bool canFitSingleLine = _fitsOnSingleLine && requiredWidth <= finalSize.Width + 0.5;

        if (canFitSingleLine)
        {
            ArrangeSingleLine(badgesControl, messageControl, finalSize);
        }
        else
        {
            ArrangeMultiLine(badgesControl, messageControl, finalSize);
        }

        return finalSize;
    }

    private static bool MeasureBadges(Control badgesControl, Size availableSize, out Size badgesSize)
    {
        if (!badgesControl.IsVisible)
        {
            badgesSize = new Size(0, 0);
            return false;
        }

        badgesControl.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        badgesSize = badgesControl.DesiredSize;
        return badgesSize.Width > 0.001 && badgesSize.Height > 0.001;
    }

    private Size MeasureMessageOnly(Control messageControl, Size availableSize)
    {
        messageControl.Measure(availableSize);
        _fitsOnSingleLine = true;
        return messageControl.DesiredSize;
    }

    private Size MeasureBadgesAndMessage(Control badgesControl, Control messageControl, Size badgesSize, Size availableSize)
    {
        messageControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Size messageSingleLineSize = messageControl.DesiredSize;

        double requiredWidth = badgesSize.Width + HorizontalSpacing + messageSingleLineSize.Width;
        bool fitsOnSingleLine = double.IsInfinity(availableSize.Width) || requiredWidth <= availableSize.Width;

        _fitsOnSingleLine = fitsOnSingleLine;

        if (fitsOnSingleLine)
        {
            double combinedHeight = Math.Max(badgesSize.Height, messageSingleLineSize.Height);
            return new Size(requiredWidth, combinedHeight);
        }

        messageControl.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        Size messageMultiLineSize = messageControl.DesiredSize;

        double stackedWidth = Math.Max(badgesSize.Width, messageMultiLineSize.Width);
        double stackedHeight = badgesSize.Height + VerticalSpacing + messageMultiLineSize.Height;
        return new Size(stackedWidth, stackedHeight);
    }

    private void ArrangeSingleLine(Control badgesControl, Control messageControl, Size finalSize)
    {
        double badgesW = badgesControl.DesiredSize.Width;
        double badgesH = badgesControl.DesiredSize.Height;
        double badgesY = (finalSize.Height - badgesH) / 2.0;
        badgesControl.Arrange(new Rect(0, Math.Max(0, badgesY), badgesW, badgesH));

        double messageX = badgesW + HorizontalSpacing;
        double messageW = Math.Max(0, finalSize.Width - messageX);
        double messageH = messageControl.DesiredSize.Height;
        double messageY = (finalSize.Height - messageH) / 2.0;
        messageControl.Arrange(new Rect(messageX, Math.Max(0, messageY), messageW, messageH));
    }

    private void ArrangeMultiLine(Control badgesControl, Control messageControl, Size finalSize)
    {
        double badgesW = finalSize.Width;
        double badgesH = badgesControl.DesiredSize.Height;
        badgesControl.Arrange(new Rect(0, 0, badgesW, badgesH));

        double messageY = badgesH + VerticalSpacing;
        double messageW = finalSize.Width;
        double messageH = Math.Max(0, finalSize.Height - messageY);
        messageControl.Arrange(new Rect(0, messageY, messageW, messageH));
    }
}
