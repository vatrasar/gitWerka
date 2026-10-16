namespace GitWerka.Shared.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GitWerka.Core.Domain.Enums;
using Material.Icons;

public class FileStatusToIconConverter : IValueConverter
{
    public static readonly FileStatusToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FileChangeStatus status)
        {
            return MaterialIconKind.FileOutline;
        }

        return status switch
        {
            FileChangeStatus.Added or FileChangeStatus.Untracked => MaterialIconKind.Plus,
            FileChangeStatus.Deleted => MaterialIconKind.Minus,
            FileChangeStatus.Modified => MaterialIconKind.PencilOutline,
            FileChangeStatus.Renamed => MaterialIconKind.FileMoveOutline,
            FileChangeStatus.Copied => MaterialIconKind.ContentCopy,
            FileChangeStatus.Ignored => MaterialIconKind.EyeOffOutline,
            FileChangeStatus.Conflicted => MaterialIconKind.AlertCircleOutline,
            _ => MaterialIconKind.FileOutline
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
