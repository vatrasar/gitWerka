namespace GitWerka.Shared.Converters;

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GitWerka.Core.Domain.Enums;

public class FileStatusToBrushConverter : IValueConverter
{
    public static readonly FileStatusToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FileChangeStatus status)
        {
            return ResolveBrush("SubtleTextBrush", Brushes.Gray);
        }

        return status switch
        {
            FileChangeStatus.Added or FileChangeStatus.Untracked => ResolveBrush("SuccessBrush", Brushes.ForestGreen),
            FileChangeStatus.Deleted => ResolveBrush("ErrorBrush", Brushes.Crimson),
            FileChangeStatus.Modified => ResolveBrush("WarningBrush", Brushes.Goldenrod),
            FileChangeStatus.Conflicted => ResolveBrush("WarningBrush", Brushes.Goldenrod),
            FileChangeStatus.Renamed or FileChangeStatus.Copied => ResolveBrush("InfoBrush", Brushes.DodgerBlue),
            _ => ResolveBrush("SubtleTextBrush", Brushes.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static IBrush ResolveBrush(string resourceKey, IBrush fallback)
    {
        if (Application.Current != null && Application.Current.TryFindResource(resourceKey, out object? resource) && resource is IBrush brush)
        {
            return brush;
        }

        return fallback;
    }
}
