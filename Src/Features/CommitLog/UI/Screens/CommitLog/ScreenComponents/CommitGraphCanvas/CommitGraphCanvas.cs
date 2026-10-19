/// <summary>
/// Component: CommitGraphCanvas
/// Purpose: High-performance visual renderer for Git commit graph topology lines, lanes, and node dots.
/// Usage:
/// - Bound to CommitSummary.Lane, GraphWidth, PassingLanes, MergingLanes, and ForkingOffLanes.
/// Key UI Elements:
/// - Custom hardware-accelerated vector drawing context.
/// Used In:
/// - CommitLogView.
/// </summary>
namespace GitWerka.Features.CommitLog.UI.Screens.CommitLog.ScreenComponents.CommitGraphCanvas;

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

public class CommitGraphCanvas : Control
{
    private const double LaneSpacing = 16.0;
    private const double NodeRadius = 4.5;
    private const double StrokeWidth = 2.0;

    private static readonly IBrush[] LaneBrushes =
    {
        new SolidColorBrush(Color.Parse("#388BFD")),
        new SolidColorBrush(Color.Parse("#3FB950")),
        new SolidColorBrush(Color.Parse("#D29922")),
        new SolidColorBrush(Color.Parse("#F778BA")),
        new SolidColorBrush(Color.Parse("#A371F7"))
    };

    public static readonly StyledProperty<int> LaneProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, int>(nameof(Lane), 0);

    public static readonly StyledProperty<double> GraphWidthProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, double>(nameof(GraphWidth), 80.0);

    public static readonly StyledProperty<IReadOnlyList<int>?> PassingLanesProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, IReadOnlyList<int>?>(nameof(PassingLanes));

    public static readonly StyledProperty<IReadOnlyList<int>?> MergingLanesProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, IReadOnlyList<int>?>(nameof(MergingLanes));

    public static readonly StyledProperty<IReadOnlyList<int>?> ForkingOffLanesProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, IReadOnlyList<int>?>(nameof(ForkingOffLanes));

    public static readonly StyledProperty<bool> IsWipProperty =
        AvaloniaProperty.Register<CommitGraphCanvas, bool>(nameof(IsWip), false);

    public int Lane
    {
        get => GetValue(LaneProperty);
        set => SetValue(LaneProperty, value);
    }

    public double GraphWidth
    {
        get => GetValue(GraphWidthProperty);
        set => SetValue(GraphWidthProperty, value);
    }

    public bool IsWip
    {
        get => GetValue(IsWipProperty);
        set => SetValue(IsWipProperty, value);
    }

    public IReadOnlyList<int>? PassingLanes
    {
        get => GetValue(PassingLanesProperty);
        set => SetValue(PassingLanesProperty, value);
    }

    public IReadOnlyList<int>? MergingLanes
    {
        get => GetValue(MergingLanesProperty);
        set => SetValue(MergingLanesProperty, value);
    }

    public IReadOnlyList<int>? ForkingOffLanes
    {
        get => GetValue(ForkingOffLanesProperty);
        set => SetValue(ForkingOffLanesProperty, value);
    }

    static CommitGraphCanvas()
    {
        AffectsMeasure<CommitGraphCanvas>(GraphWidthProperty);
        AffectsRender<CommitGraphCanvas>(LaneProperty, GraphWidthProperty, PassingLanesProperty, MergingLanesProperty, ForkingOffLanesProperty, IsWipProperty);
    }

    public CommitGraphCanvas()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = !double.IsNaN(Width) ? Width : (GraphWidth > 0 ? GraphWidth : 80.0);
        double height = double.IsNaN(Height) ? 28.0 : Height;
        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double centerY = Bounds.Height / 2.0;
        double currentX = 10.0 + (Lane * LaneSpacing);
        IBrush currentBrush = GetLaneBrush(Lane);
        IPen currentPen = new Pen(currentBrush, StrokeWidth);

        DrawPassingLanes(context);

        if (IsWip)
        {
            context.DrawLine(currentPen, new Point(currentX, centerY), new Point(currentX, Bounds.Height));
            DrawWipNode(context, currentX, centerY, currentBrush);
            return;
        }

        DrawParentLine(context, currentX, centerY, currentPen);
        DrawMergingLanes(context, currentX, centerY);
        DrawForkingLanes(context, currentX, centerY);
        DrawCommitNode(context, currentX, centerY, currentBrush);
    }

    private void DrawWipNode(DrawingContext context, double currentX, double centerY, IBrush brush)
    {
        Point center = new(currentX, centerY);
        double radius = NodeRadius + 1.5;

        IDashStyle dashStyle = new DashStyle(new double[] { 2.5, 2.5 }, 0);
        IPen dashedPen = new Pen(brush, 1.8, dashStyle);

        Color fillColor = Color.FromArgb(40, 56, 139, 253);
        if (brush is ISolidColorBrush solid)
        {
            fillColor = Color.FromArgb(45, solid.Color.R, solid.Color.G, solid.Color.B);
        }

        IBrush fillBrush = new SolidColorBrush(fillColor);
        context.DrawEllipse(fillBrush, dashedPen, center, radius, radius);
    }

    private void DrawPassingLanes(DrawingContext context)
    {
        if (PassingLanes == null)
        {
            return;
        }

        foreach (int lane in PassingLanes)
        {
            double x = 10.0 + (lane * LaneSpacing);
            IPen pen = new Pen(GetLaneBrush(lane), StrokeWidth);
            context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
        }
    }

    private void DrawParentLine(DrawingContext context, double currentX, double centerY, IPen pen)
    {
        context.DrawLine(pen, new Point(currentX, 0), new Point(currentX, Bounds.Height));
    }

    private void DrawMergingLanes(DrawingContext context, double currentX, double centerY)
    {
        if (MergingLanes == null)
        {
            return;
        }

        foreach (int lane in MergingLanes)
        {
            double x = 10.0 + (lane * LaneSpacing);
            IPen pen = new Pen(GetLaneBrush(lane), StrokeWidth);
            context.DrawLine(pen, new Point(x, Bounds.Height), new Point(currentX, centerY));
        }
    }

    private void DrawForkingLanes(DrawingContext context, double currentX, double centerY)
    {
        if (ForkingOffLanes == null)
        {
            return;
        }

        foreach (int lane in ForkingOffLanes)
        {
            double x = 10.0 + (lane * LaneSpacing);
            IPen pen = new Pen(GetLaneBrush(lane), StrokeWidth);
            context.DrawLine(pen, new Point(currentX, centerY), new Point(x, 0));
        }
    }

    private void DrawCommitNode(DrawingContext context, double currentX, double centerY, IBrush brush)
    {
        Point center = new(currentX, centerY);
        context.DrawEllipse(brush, null, center, NodeRadius, NodeRadius);

        IPen borderPen = new Pen(Brushes.White, 1.2);
        context.DrawEllipse(null, borderPen, center, NodeRadius, NodeRadius);
    }

    private static IBrush GetLaneBrush(int lane)
    {
        int index = Math.Abs(lane) % LaneBrushes.Length;
        return LaneBrushes[index];
    }
}
